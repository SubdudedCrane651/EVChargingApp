using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Microcharts;

namespace EVCharging
{
    public class CustomChartView : SKCanvasView
    {
        public Chart Chart { get; set; }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            Chart?.Draw(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        }
    }
}