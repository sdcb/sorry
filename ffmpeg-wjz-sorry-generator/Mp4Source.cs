using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace Sorry;

public record Mp4Source(string Mp4Url, Subtitle[] Subtitles)
{
    public IEnumerable<Subtitle> GetSubtitlesOnTS(double ts) => Subtitles.Where(x => x.Def.WillShowInTS(ts));

    static void RenderOneFrame(VideoTime time, ID2D1RenderTarget ctx, DxRes res, Mp4Source mp4Source)
    {
        IEnumerable<Subtitle> subtitles = mp4Source.GetSubtitlesOnTS(time.Elapsed.TotalSeconds);
        foreach (Subtitle subtitle in subtitles)
        {
            using IDWriteTextFormat font = res.DWriteFactory.CreateTextFormat(subtitle.Val.Font ?? "Consolas", subtitle.Val.FontSize ?? 20);
            using IDWriteTextLayout layout = res.DWriteFactory.CreateTextLayout(subtitle.Val.Text, font, ctx.Size.Width, ctx.Size.Height);
            TextMetrics metrics = layout.Metrics;
            ctx.Transform = Matrix3x2.CreateTranslation(-metrics.Width / 2, -metrics.Height / 2) * Matrix3x2.CreateTranslation(ctx.Size.Width / 2, ctx.Size.Height * 0.80f) * Matrix3x2.CreateTranslation(font.FontSize / 14, font.FontSize / 14);
            ctx.DrawTextLayout(Vector2.Zero, layout, res.GetColor(Colors.Black));
            ctx.Transform = Matrix3x2.CreateTranslation(-metrics.Width / 2, -metrics.Height / 2) * Matrix3x2.CreateTranslation(ctx.Size.Width / 2, ctx.Size.Height * 0.80f);
            ctx.DrawTextLayout(Vector2.Zero, layout, res.GetColor(Colors.White));
        }
    }

    public byte[] DecodeAddSubtitle()
    {
        byte[] downloadedMp4 = new HttpClient().GetByteArrayAsync(Mp4Url).GetAwaiter().GetResult();
        using IOContext inIO = IOContext.ReadStream(new MemoryStream(downloadedMp4));
        using FormatContext inFc = FormatContext.OpenInputIO(inIO);
        inFc.LoadStreamInfo();
        MediaStream inVideoStream = inFc.GetVideoStream();
        CodecParameters inCodecpar = inVideoStream.Codecpar ?? throw new InvalidOperationException("Codecpar should not be null");
        double durationInSeconds = inVideoStream.GetDurationInSeconds();
        using CodecContext inVCodec = new(Codec.FindDecoderById(inVideoStream.Codecpar.CodecId));
        inVCodec.FillParameters(inCodecpar);
        inVCodec.Open();

        using FormatContext fc = FormatContext.AllocOutput(formatName: "gif");
        fc.VideoCodec = Codec.FindEncoderById(AVCodecID.Gif);
        MediaStream vstream = fc.NewStream(fc.VideoCodec);
        using CodecContext vcodec = new(fc.VideoCodec)
        {
            Width = inCodecpar.Width,
            Height = inCodecpar.Height,
            TimeBase = inVideoStream.RFrameRate.Inverse(),
            PixelFormat = AVPixelFormat.Pal8,
        };
        vcodec.Open(fc.VideoCodec);
        vstream.Codecpar!.CopyFrom(vcodec);
        vstream.TimeBase = vcodec.TimeBase;

        using DynamicIOContext io = IOContext.OpenDynamic();
        fc.Pb = io;
        fc.WriteHeader();
        int frameCount = (int)Math.Ceiling(durationInSeconds / vcodec.TimeBase.ToDouble());
        inFc.ReadPackets(inVideoStream.Index)
            .DecodePackets(inVCodec)
            .ConvertVideoFrames(() => new SizeI(vcodec.Width, vcodec.Height), AVPixelFormat.Bgra)
            .RenderAll(vcodec, RenderOneFrame, this, frameCount: frameCount)
            //.ConvertFrames(vcodec)
            .ApplyVideoFilters(vcodec.TimeBase, AVPixelFormat.Pal8, $"scale=flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse")
            .EncodeAllFrames(fc, null, vcodec)
            .WriteAll(fc);
        fc.WriteTrailer();
        return io.GetBuffer().ToArray();
    }
}