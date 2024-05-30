using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Swscales;
using Sdcb.FFmpeg.Utils;
using Sorry;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;

namespace Sorry;

public static class FramesExtensions
{
    public static IEnumerable<Frame> RenderAll(this IEnumerable<Frame> frames, CodecContext codecCtx, FrameRendererDelegate frameRenderer, Mp4Source mp4Source, int frameCount, bool unref = true)
    {
        using DxRes basic = new(codecCtx.Width, codecCtx.Height);
        using VideoFrameConverter frameConverter = new();
        using Frame rgbFrame = new()
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

    public static IEnumerable<Frame> ConvertVideoFrames(this IEnumerable<Frame> sourceFrames, Func<SizeI> sizeAccessor, AVPixelFormat pixelFormat, SWS swsFlags = SWS.Bilinear, bool unref = true)
    {
        Frame dest = null!;
        {
            SizeI size = sizeAccessor();
            dest = Frame.CreateVideo(size.Width, size.Height, pixelFormat);
        }
        using Frame destRef = new();

        try
        {
            int pts = 0;
            using VideoFrameConverter frameConverter = new();
            foreach (Frame sourceFrame in sourceFrames)
            {
                if (sourceFrame.Width > 0)
                {
                    SizeI newSize = sizeAccessor();
                    if (dest.Width != newSize.Width || dest.Height != newSize.Height)
                    {
                        dest.Dispose();
                        dest = Frame.CreateVideo(newSize.Width, newSize.Height, pixelFormat);
                    }

                    dest.MakeWritable();
                    frameConverter.ConvertFrame(sourceFrame, dest, swsFlags);
                    if (unref) sourceFrame.Unref();
                    dest.Pts = pts++;
                    destRef.Ref(dest);
                    yield return destRef;
                }
                else
                {
                    // bypass not a video frame
                    yield return sourceFrame;
                }
            }
        }
        finally
        {
            dest.Dispose();
        }
    }
}

public delegate void FrameRendererDelegate(VideoTime time, ID2D1RenderTarget ctx, DxRes res, Mp4Source mp4Source);