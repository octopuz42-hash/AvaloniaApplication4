using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaApplication4.ViewModels;
using AvaloniaApplication4.Models;
using SkiaSharp;

namespace AvaloniaApplication4.Controls;

public class PaintCanvas : Control
{
    public static readonly StyledProperty<SKBitmap?> BitmapProperty =
        AvaloniaProperty.Register<PaintCanvas, SKBitmap?>(nameof(Bitmap));

    public SKBitmap? Bitmap
    {
        get => GetValue(BitmapProperty);
        set => SetValue(BitmapProperty, value);
    }

    private Point? _lastPoint;
    private bool _isDrawing;

    static PaintCanvas()
    {
        BitmapProperty.Changed.AddClassHandler<PaintCanvas>((x, e) => x.InvalidateVisual());
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (DataContext is not MainViewModel vm || Bitmap == null) return;

        var point = e.GetPosition(this);
        vm.SaveStateForUndo();
        _isDrawing = true;

        using var canvas = new SKCanvas(Bitmap);
        using var paint = CreatePaint(vm);

        if (vm.CurrentTool == DrawingTool.BucketFill)
        {
            canvas.DrawColor(paint.Color, SKBlendMode.SrcOver);
        }
        else
        {
            canvas.DrawPoint((float)point.X, (float)point.Y, paint);
            _lastPoint = point;
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (DataContext is not MainViewModel vm || !_isDrawing || _lastPoint == null || Bitmap == null) return;
        if (vm.CurrentTool == DrawingTool.BucketFill) return;

        var currentPoint = e.GetPosition(this);

        using var canvas = new SKCanvas(Bitmap);
        using var paint = CreatePaint(vm);

        canvas.DrawLine((float)_lastPoint.Value.X, (float)_lastPoint.Value.Y,
                        (float)currentPoint.X, (float)currentPoint.Y, paint);

        _lastPoint = currentPoint;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDrawing = false;
        _lastPoint = null;
    }

    public override void Render(DrawingContext context)
    {
        if (Bitmap == null) return;

        using var ms = new System.IO.MemoryStream();
        Bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
        ms.Position = 0;

        var avaloniaBitmap = new Avalonia.Media.Imaging.Bitmap(ms);

        context.DrawImage(avaloniaBitmap, new Rect(0, 0, Bitmap.Width, Bitmap.Height));
    }


    private SKPaint CreatePaint(MainViewModel vm)
    {
        var color = vm.CurrentTool == DrawingTool.Eraser ? Colors.White : vm.SelectedColor;

        return new SKPaint
        {
            Color = new SKColor(color.R, color.G, color.B, color.A),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = vm.BrushSize,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };
    }
}
