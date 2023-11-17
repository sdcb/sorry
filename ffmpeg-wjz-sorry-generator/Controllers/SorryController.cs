using ffmpeg_wjz_sorry_generator.Models;
using Microsoft.AspNetCore.Mvc;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Toolboxs.Extensions;
using Sdcb.FFmpeg.Utils;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;

namespace ffmpeg_wjz_sorry_generator.Controllers
{
    public class SorryController : Controller
    {
        private readonly ILogger<SorryController> _logger;

        public SorryController(ILogger<SorryController> logger)
        {
            _logger = logger;
        }

        public FileResult Generate(string type, string subtitle)
        {
            FFmpegLogger.LogWriter = (level, msg) => _logger.LogInformation(msg);
            Mp4SourceDef def = type switch
            {
                "wjz" => Mp4SourceDef.WangJingZe,
                "sorry" => Mp4SourceDef.Sorry,
                "nlgl" => Mp4SourceDef.Nlgl,
                _ => throw new NotImplementedException($"Not supported type: {type}"),
            };
            Mp4Source mp4Source = def.CreateLines(subtitle.Split("|"));
            byte[] videoBytes = DecodeAddSubtitle(mp4Source, RenderOneFrame);
            return File(videoBytes, "image/gif");
        }

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

        public static byte[] DecodeAddSubtitle(Mp4Source mp4Source, FrameRendererDelegate frameRenderer)
        {
            byte[] downloadedMp4 = new HttpClient().GetByteArrayAsync(mp4Source.Mp4Url).GetAwaiter().GetResult();
            using IOContext inIO = IOContext.ReadStream(new MemoryStream(downloadedMp4));
            using FormatContext inFc = FormatContext.OpenInputIO(inIO);
            inFc.LoadStreamInfo();
            MediaStream inVideoStream = inFc.GetVideoStream();
            CodecParameters inCodecpar = inVideoStream.Codecpar ?? throw new InvalidOperationException("Codecpar should not be null");
            double durationInSeconds = inVideoStream.GetDurationInSeconds();
            using CodecContext inVCodec = new CodecContext(Codec.FindDecoderById(inVideoStream.Codecpar.CodecId));
            inVCodec.FillParameters(inCodecpar);
            inVCodec.Open();

            using FormatContext fc = FormatContext.AllocOutput(formatName: "gif");
            fc.VideoCodec = Codec.FindEncoderById(AVCodecID.Gif);
            MediaStream vstream = fc.NewStream(fc.VideoCodec);
            using CodecContext vcodec = new CodecContext(fc.VideoCodec)
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
                .ConvertVideoFrames(() => (vcodec.Width, vcodec.Height), AVPixelFormat.Bgra)
                .RenderAll(vcodec, frameRenderer, mp4Source, frameCount: frameCount)
                //.ConvertFrames(vcodec)
                .ApplyVideoFilters(vcodec.TimeBase, AVPixelFormat.Pal8, $"scale=flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse")
                .EncodeAllFrames(fc, null, vcodec)
                .WriteAll(fc);
            fc.WriteTrailer();
            return io.GetBuffer().ToArray();
        }
    }

    public delegate void FrameRendererDelegate(VideoTime time, ID2D1RenderTarget ctx, DxRes res, Mp4Source mp4Source);

    public static class FramesExtensions
    {
        public static IEnumerable<Frame> RenderAll(this IEnumerable<Frame> frames, CodecContext codecCtx, FrameRendererDelegate frameRenderer, Mp4Source mp4Source, int frameCount, bool unref = true)
        {
            using DxRes basic = new(codecCtx.Width, codecCtx.Height);
            using VideoFrameConverter frameConverter = new();
            using Frame rgbFrame = new Frame()
            {
                Width = codecCtx.Width,
                Height = codecCtx.Height,
                Format = (int)AVPixelFormat.Bgra
            };
            using Frame refFrame = new();

            int i = 0;
            foreach (Frame frame in frames)
            {
                using ID2D1DeviceContext ctx = basic.RenderTarget.QueryInterface<ID2D1DeviceContext1>();
                using ID2D1Bitmap bmp = ctx.CreateBitmap(new SizeI(frame.Width, frame.Height), frame.Data._0, frame.Linesize[0], new BitmapProperties1(new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied)));
                if (unref) frame.Unref();
                ctx.BeginDraw();
                {
                    ctx.Transform = Matrix3x2.Identity;
                    ctx.DrawImage(bmp, new Vector2(0, 0));
                    VideoTime time = new(i, TimeSpan.FromSeconds(1.0 * i * codecCtx.TimeBase.Num / codecCtx.TimeBase.Den), frameCount);
                    frameRenderer(time, ctx, basic, mp4Source);
                }
                ctx.EndDraw();

                using (IWICBitmapLock bmpLock = basic.WicBmp.Lock(BitmapLockFlags.Read))
                {
                    rgbFrame.Data._0 = bmpLock.Data.DataPointer;
                    rgbFrame.Linesize[0] = bmpLock.Data.Pitch;
                    refFrame.Ref(rgbFrame);
                    yield return refFrame;
                }
                ++i;
            };
        }
    }

    public record struct VideoTime(int Frame, TimeSpan Elapsed, int TotalFrame)
    {
        public float Percent => 1.0f * Frame / TotalFrame;
    }

    public class DxRes : IDisposable
    {
        public readonly IWICImagingFactory WicFactory = new IWICImagingFactory();
        public readonly ID2D1Factory2 D2dFactory = D2D1.D2D1CreateFactory<ID2D1Factory2>();
        public readonly IWICBitmap WicBmp;
        public readonly ID2D1RenderTarget RenderTarget;
        private readonly ID2D1SolidColorBrush DefaultColor;
        public readonly IDWriteFactory DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();

        public DxRes(int width, int height)
        {
            WicBmp = WicFactory.CreateBitmap(width, height, Vortice.WIC.PixelFormat.Format32bppPBGRA, BitmapCreateCacheOption.CacheOnLoad);
            RenderTarget = D2dFactory.CreateWicBitmapRenderTarget(WicBmp, new RenderTargetProperties(new Vortice.DCommon.PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied)));
            DefaultColor = RenderTarget.CreateSolidColorBrush(Colors.CornflowerBlue);
        }

        public ID2D1SolidColorBrush GetColor(Color4 color)
        {
            DefaultColor.Color = color;
            return DefaultColor;
        }

        public void Dispose()
        {
            DefaultColor.Dispose();
            RenderTarget.Dispose();
            WicBmp.Dispose();
            D2dFactory.Dispose();
            WicFactory.Dispose();
            DWriteFactory.Dispose();
        }
    }

    public record SubtitleDef(double StartTS, double EndTS, string RefText)
    {
        public double Duration => EndTS - StartTS;
        public bool WillShowInTS(double ts) => StartTS <= ts && ts < EndTS;
    }

    public record SubtitleValue(string? Font, float? FontSize, string Text)
    {
        public static SubtitleValue CreateDefault(string text) => new SubtitleValue(null, null, text);
    }

    public record Subtitle(SubtitleDef Def, SubtitleValue Val);

    public record Mp4SourceDef(string Mp4Url, SubtitleDef[] SubtitleDefs)
    {
        public IEnumerable<SubtitleDef> GetSubtitlesDefOnTS(float ts) => SubtitleDefs.Where(x => x.WillShowInTS(ts));

        public Mp4Source CreateDefault() => new Mp4Source(Mp4Url, SubtitleDefs.Select(x => new Subtitle(x, SubtitleValue.CreateDefault(x.RefText))).ToArray());

        public Mp4Source CreateLines(params string[] lines) => new Mp4Source(Mp4Url, SubtitleDefs.Zip(lines).Select(x => new Subtitle(x.First, SubtitleValue.CreateDefault(x.Second))).ToArray());

        public static readonly Mp4SourceDef WangJingZe = new Mp4SourceDef("https://io.starworks.cc:88/cv-public/2022/gif-wjz.mp4", new[]
        {
            new SubtitleDef(0, 1.04, "我王境泽就是饿死"),
            new SubtitleDef(1.46, 2.9, "死外面 从这里跳下去"),
            new SubtitleDef(3.09, 4.33, "也不会吃你们一点东西的"),
            new SubtitleDef(4.59, 5.93, "真香~"),
        });

        public static readonly Mp4SourceDef Sorry = new Mp4SourceDef("https://io.starworks.cc:88/cv-public/2022/gif-sorry.mp4", new[]
        {
            new SubtitleDef(1.18, 1.56, "好啊"),
            new SubtitleDef(3.18, 4.43, "别说我是一等良民"),
            new SubtitleDef(5.31, 7.43, "就算你真的想要诬告我"),
            new SubtitleDef(7.56, 9.93, "我有的是钱找律师帮我打官司"),
            new SubtitleDef(10.06, 11.56, "我想我根本不用坐牢了"),
            new SubtitleDef(11.93, 13.06, "你别以为有钱了不起啊"),
            new SubtitleDef(13.81, 16.31, "Sorry"),
            new SubtitleDef(18.06, 19.56, "有钱真的了不起"),
            new SubtitleDef(19.60, 21.60, "不过我想你不会明白这种感觉"),
        });

        public static readonly Mp4SourceDef Nlgl = new Mp4SourceDef("https://io.starworks.cc:88/cv-public/2023/nlgl-mini.mp4", new[]
        {
            new SubtitleDef(0.333, 1.399, "花西子越来越贵了"),
            new SubtitleDef(1.400, 2.333, "哪里贵了"),
            new SubtitleDef(2.766, 4.733, "这么多年都是这个价格好吧"),
            new SubtitleDef(4.733, 6.166, "不要睁着眼睛乱说"),
            new SubtitleDef(6.166, 8.966, "国货品牌很难的哦"),
            new SubtitleDef(8.966, 10.199, "而且花西子真的不是那种"),
            new SubtitleDef(10.200, 11.700, "随便买原料就做的品牌"),
            new SubtitleDef(11.700, 13.966, "哎我跟花西子跟了多少年"),
            new SubtitleDef(13.966, 15.899, "他怎么起来的我是最知道的一个人"),
            new SubtitleDef(15.900, 16.700, "是啊"),
            new SubtitleDef(17.100, 18.966, "他们就差点把他们家掏给我了"),
            new SubtitleDef(19.100, 19.900, "哈哈好不好"),
            new SubtitleDef(19.900, 20.600, "真的乱说"),
            new SubtitleDef(20.600, 22.266, "这么多年都是79块钱"),
            new SubtitleDef(22.266, 23.333, "哪里贵了"),
            new SubtitleDef(23.366, 24.899, "有的时候找找自己原因好吧"),
            new SubtitleDef(24.900, 25.333, "这么多年了"),
            new SubtitleDef(25.333, 25.999, "工资涨没涨"),
            new SubtitleDef(26.000, 28.200, "有没有认真工作好不好"),
            new SubtitleDef(28.800, 29.966, "这么多年都是这个价格"),
            new SubtitleDef(29.966, 30.899, "我真的快疯掉了"),
        });
    }

    public record Mp4Source(string Mp4Url, Subtitle[] Subtitles)
    {
        public IEnumerable<Subtitle> GetSubtitlesOnTS(double ts) => Subtitles.Where(x => x.Def.WillShowInTS(ts));
    }
}