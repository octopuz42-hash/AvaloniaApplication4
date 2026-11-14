using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaApplication4.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AvaloniaApplication4.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _canvasWidth = 800;
    [ObservableProperty] private int _canvasHeight = 600;
    [ObservableProperty] private SKBitmap? _bitmap;
    private double _zoomScale = 1.0;

    public double ZoomScale
    {
        get => _zoomScale;
        set
        {
            if (SetProperty(ref _zoomScale, value))
            {
                OnPropertyChanged(nameof(ScaledCanvasWidth));
                OnPropertyChanged(nameof(ScaledCanvasHeight));
            }
        }
    }

    public double ScaledCanvasWidth => CanvasWidth * ZoomScale;
    public double ScaledCanvasHeight => CanvasHeight * ZoomScale;

    partial void OnCanvasWidthChanged(int value) => OnPropertyChanged(nameof(ScaledCanvasWidth));
    partial void OnCanvasHeightChanged(int value) => OnPropertyChanged(nameof(ScaledCanvasHeight));

    [ObservableProperty] private DrawingTool _currentTool = DrawingTool.Pencil;
    [ObservableProperty] private Color _selectedColor = Colors.Black;
    [ObservableProperty] private int _brushSize = 5;

    private readonly Stack<SKBitmap> _undoStack = new();
    private readonly Stack<SKBitmap> _redoStack = new();

    public MainViewModel()
    {
        CreateNewCanvas();
    }

    [RelayCommand]
    public void CreateNewCanvas()
    {
        var newBitmap = new SKBitmap(CanvasWidth, CanvasHeight);
        using (var canvas = new SKCanvas(newBitmap))
        {
            canvas.Clear(SKColors.White);
        }

        Bitmap = newBitmap;
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public void SaveStateForUndo()
    {
        if (Bitmap == null) return;
        _undoStack.Push(Bitmap.Copy());
        _redoStack.Clear();
    }

    [RelayCommand]
    public void Undo()
    {
        if (_undoStack.Count == 0 || Bitmap == null) return;

        _redoStack.Push(Bitmap);
        Bitmap = _undoStack.Pop();
    }

    [RelayCommand]
    public void Redo()
    {
        if (_redoStack.Count == 0 || Bitmap == null) return;

        _undoStack.Push(Bitmap);
        Bitmap = _redoStack.Pop();
    }

    public void SaveToFile(Stream stream)
    {
        if (Bitmap == null) return;
        using var image = SKImage.FromBitmap(Bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }
}

public partial class MainViewModel : ObservableObject
{

    [RelayCommand]
    public void ApplyFilter(ImageFilterType filterType)
    {
        if (Bitmap == null || filterType == ImageFilterType.None) return;

        SaveStateForUndo();

        var filteredBitmap = new SKBitmap(Bitmap.Width, Bitmap.Height);

        using (var canvas = new SKCanvas(filteredBitmap))
        using (var paint = new SKPaint())
        {
            switch (filterType)
            {
                case ImageFilterType.Invert:
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[] {
                            -1,  0,  0,  0, 255,
                             0, -1,  0,  0, 255,
                             0,  0, -1,  0, 255,
                             0,  0,  0,  1,   0
                        });
                    break;

                case ImageFilterType.Grayscale:
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[] {
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0.21f, 0.72f, 0.07f, 0, 0,
                            0,     0,     0,     1, 0
                        });
                    break;

                case ImageFilterType.Sepia:
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[] {
                            0.393f, 0.769f, 0.189f, 0, 0,
                            0.349f, 0.686f, 0.168f, 0, 0,
                            0.272f, 0.534f, 0.131f, 0, 0,
                            0,      0,      0,      1, 0
                        });
                    break;

                case ImageFilterType.Blur:
                    paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 5);
                    break;
            }

            canvas.DrawBitmap(Bitmap, 0, 0, new SKSamplingOptions(), paint);
        }

        Bitmap = filteredBitmap;
    }
}