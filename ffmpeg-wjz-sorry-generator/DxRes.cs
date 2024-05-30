using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;

namespace Sorry;

public class DxRes : IDisposable
{
    public readonly IWICImagingFactory WicFactory = new();
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
        GC.SuppressFinalize(this);
    }
}
