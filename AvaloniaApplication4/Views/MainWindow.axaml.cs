using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaApplication4.ViewModels;

namespace AvaloniaApplication4.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void CanvasScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || DataContext is not MainViewModel vm)
            return;

        e.Handled = true;

        var oldScale = vm.ZoomScale;
        var newScale = Math.Clamp(oldScale * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1), 0.2, 5.0);
        if (newScale == oldScale)
            return;

        var pointer = e.GetPosition(CanvasScrollViewer);
        var offset = CanvasScrollViewer.Offset;
        var margin = CanvasHost.Margin;
        var canvasX = (offset.X + pointer.X - margin.Left) / oldScale;
        var canvasY = (offset.Y + pointer.Y - margin.Top) / oldScale;

        vm.ZoomScale = newScale;

        Dispatcher.UIThread.Post(() =>
        {
            CanvasScrollViewer.Offset = new Vector(
                canvasX * newScale + margin.Left - pointer.X,
                canvasY * newScale + margin.Top - pointer.Y);
        }, DispatcherPriority.Background);
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.Bitmap == null) return;

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить рисунок",
            DefaultExtension = "png",
            SuggestedFileName = "canvas_image",
            FileTypeChoices = new[] { new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } } }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            vm.SaveToFile(stream);
        }
    }
}

public class EnumToIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Enum enumValue ? System.Convert.ToInt32(enumValue, culture) : -1;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int index && targetType.IsEnum ? Enum.ToObject(targetType, index) : Avalonia.Data.BindingOperations.DoNothing;
    }
}
