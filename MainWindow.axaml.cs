using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace MyApp;

public partial class MainWindow : Window
{
    private Grid? _mainContainer;
    private string? _startupImagePath;

    private double _zoomScale = 1.0;
    private const double ZoomStep = 0.1;
    private Image? _currentImageControl;

    private Point _lastPointerPosition;
    private bool _isDragging = false;
    private TranslateTransform _panTransform = new();
    private ScaleTransform _scaleTransform = new();
    private TransformGroup _transformGroup = new();
    private RotateTransform _rotateTransform = new();

    public MainWindow()
    {
        InitializeComponent();

        _mainContainer = this.FindControl<Grid>("MainContainer");

        _transformGroup.Children.Add(_panTransform);
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_rotateTransform);

        Opened += MainWindow_Opened;
        KeyDown += MainWindow_KeyDown;
        PointerWheelChanged += MainWindow_PointerWheelChanged;
    }

    public MainWindow(string imagePath)
    {
        InitializeComponent();

        _mainContainer = this.FindControl<Grid>("MainContainer");

        _transformGroup.Children.Add(_panTransform);
        _transformGroup.Children.Add(_scaleTransform);
        _transformGroup.Children.Add(_rotateTransform);

        _startupImagePath = imagePath;

        Opened += MainWindow_Opened;
        KeyDown += MainWindow_KeyDown;
        PointerWheelChanged += MainWindow_PointerWheelChanged;
    }

    private async void MainWindow_Opened(object? sender, EventArgs e)
    {
        Opened -= MainWindow_Opened;

        if (!string.IsNullOrEmpty(_startupImagePath))
        {
            DisplayImageFromFile(_startupImagePath);
        }
        else
        {
            await OpenFilePickerAsync();
        }
    }

    private async Task OpenFilePickerAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var fileTypeFilters = new[]
        {
            new FilePickerFileType("Image Files")
            {
                Patterns = [ "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" ],
                MimeTypes = [ "image/*" ]
            },
            new FilePickerFileType("All Files")
            {
                Patterns = [ "*.*" ]
            }
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Image File",
            AllowMultiple = false,
            FileTypeFilter = fileTypeFilters
        });

        if (result.Any())
        {
            string? imagePath = result.First().Path.LocalPath;
            if (imagePath != null)
            {
                DisplayImageFromFile(imagePath);
            }
        }
        else
        {
            Close();
        }
    }

    private void MainWindow_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_currentImageControl == null || _mainContainer == null) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            double oldScale = _zoomScale;

            if (e.Delta.Y > 0)
            {
                _zoomScale = Math.Min(8.0, _zoomScale + ZoomStep);
            }
            else if (e.Delta.Y < 0)
            {
                _zoomScale = Math.Max(0.8, _zoomScale - ZoomStep);
            }

            Point position = e.GetPosition(_currentImageControl);

            double imageCenterW = _currentImageControl.Bounds.Width / 2.0;
            double imageCenterH = _currentImageControl.Bounds.Height / 2.0;

            double x_distance_from_center = position.X - imageCenterW;
            double y_distance_from_center = position.Y - imageCenterH;

            double totalShiftX = x_distance_from_center * (_zoomScale - oldScale);
            double totalShiftY = y_distance_from_center * (_zoomScale - oldScale);

            double panAdjustmentX = totalShiftX / _zoomScale;
            double panAdjustmentY = totalShiftY / _zoomScale;

            double desiredPanX = _panTransform.X - panAdjustmentX;
            double desiredPanY = _panTransform.Y - panAdjustmentY;

            ApplyZoomScale();

            var (minX, maxX, minY, maxY) = CalculatePanLimits();

            _panTransform.X = Math.Clamp(desiredPanX, minX, maxX);
            _panTransform.Y = Math.Clamp(desiredPanY, minY, maxY);

            if (_zoomScale <= 0.8)
            {
                _zoomScale = 0.8;
                ApplyZoomScale();
                var (centerMinX, centerMaxX, centerMinY, centerMaxY) = CalculatePanLimits();
                _panTransform.X = centerMinX;
                _panTransform.Y = centerMinY;
            }

            e.Handled = true;
        }
    }

    private void ApplyZoomScale()
    {
        if (_currentImageControl != null)
        {
            _scaleTransform.ScaleX = _zoomScale;
            _scaleTransform.ScaleY = _zoomScale;
        }
    }

    private (double MinX, double MaxX, double MinY, double MaxY) CalculatePanLimits()
    {
        if (_currentImageControl == null || _mainContainer == null)
            return (0, 0, 0, 0);

        double containerWidth = _mainContainer.Bounds.Width;
        double containerHeight = _mainContainer.Bounds.Height;

        double angle = _rotateTransform.Angle % 360;
        if (angle < 0) angle += 360;
        bool isRotated90or270 = Math.Abs(angle - 90) < 0.1 || Math.Abs(angle - 270) < 0.1;

        double currentImgW = _currentImageControl.Bounds.Width;
        double currentImgH = _currentImageControl.Bounds.Height;

        double rotatedImgW = isRotated90or270 ? currentImgH : currentImgW;
        double rotatedImgH = isRotated90or270 ? currentImgW : currentImgH;

        double imageRenderWidth = rotatedImgW * _zoomScale;
        double imageRenderHeight = rotatedImgH * _zoomScale;

        double excessWidth = Math.Max(0, imageRenderWidth - containerWidth);
        double excessHeight = Math.Max(0, imageRenderHeight - containerHeight);

        double panLimitX = (isRotated90or270 ? excessHeight : excessWidth) / (2 * _zoomScale);
        double panLimitY = (isRotated90or270 ? excessWidth : excessHeight) / (2 * _zoomScale);

        double finalMinX = -panLimitX;
        double finalMaxX = panLimitX;
        double finalMinY = -panLimitY;
        double finalMaxY = panLimitY;

        return (finalMinX, finalMaxX, finalMinY, finalMaxY);
    }

    private void MainContainer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_currentImageControl == null) return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;

            _lastPointerPosition = e.GetPosition(_mainContainer);

            e.Pointer.Capture(_mainContainer);
        }
    }

    private void MainContainer_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _currentImageControl == null || _mainContainer == null) return;

        Point currentPosition = e.GetPosition(_mainContainer);

        double screenDeltaX = currentPosition.X - _lastPointerPosition.X;
        double screenDeltaY = currentPosition.Y - _lastPointerPosition.Y;

        if (_zoomScale > 0)
        {
            screenDeltaX /= _zoomScale;
            screenDeltaY /= _zoomScale;
        }

        double localDeltaX = screenDeltaX;
        double localDeltaY = screenDeltaY;

        double angle = _rotateTransform.Angle % 360;
        if (angle < 0) angle += 360;

        if (Math.Abs(angle - 90) < 0.1)
        {
            localDeltaX = screenDeltaY;
            localDeltaY = -screenDeltaX;
        }
        else if (Math.Abs(angle - 180) < 0.1)
        {
            localDeltaX = -screenDeltaX;
            localDeltaY = -screenDeltaY;
        }
        else if (Math.Abs(angle - 270) < 0.1)
        {
            localDeltaX = -screenDeltaY;
            localDeltaY = screenDeltaX;
        }

        double desiredPanX = _panTransform.X + localDeltaX;
        double desiredPanY = _panTransform.Y + localDeltaY;

        var (minX, maxX, minY, maxY) = CalculatePanLimits();

        _panTransform.X = Math.Clamp(desiredPanX, minX, maxX);
        _panTransform.Y = Math.Clamp(desiredPanY, minY, maxY);

        _lastPointerPosition = currentPosition;
    }

    private void MainContainer_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;

            e.Pointer.Capture(null);
        }
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.R)
        {
            _rotateTransform.Angle += 90;
        }
    }

    private void DisplayImageFromFile(string imagePath)
    {
        const int FALLBACK_CONTAINER_WIDTH = 4000;
        double containerWidth = MainContainer.Bounds.Width > 0 ? MainContainer.Bounds.Width : FALLBACK_CONTAINER_WIDTH;
        int targetWidth = Math.Min((int)(containerWidth), 4000);

        try
        {
            MainContainer.Children.Clear();
            _zoomScale = 1.0;

            _panTransform.X = 0;
            _panTransform.Y = 0;

            Bitmap bitmap;

            using (var stream = File.OpenRead(imagePath))
            {
                bitmap = Bitmap.DecodeToWidth(stream, targetWidth);
            }

            var imageControl = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            _currentImageControl = imageControl;

            _currentImageControl.RenderTransform = _transformGroup;
            _currentImageControl.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            MainContainer.Children.Add(_currentImageControl);
        }
        catch (Exception ex)
        {
            var errorText = new TextBlock
            {
                Text = $"Error loading image: {ex.Message}",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            MainContainer.Children.Add(errorText);
        }
    }
}