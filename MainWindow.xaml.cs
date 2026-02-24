using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Path = System.IO.Path;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;

namespace minisnip;

public sealed class MinisnipWindow : Window {
	private readonly Canvas _canvas = new();
	private readonly System.Windows.Shapes.Rectangle _selection = new();

	private Point? _start; // screen coords
	private Rect _currentScreenRect;

	public MinisnipWindow() {
		Title = "minisnip";
		WindowStyle = WindowStyle.None;
		ResizeMode = ResizeMode.NoResize;
		AllowsTransparency = true;
		Background = new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)); // faint dim
		Topmost = true;
		ShowInTaskbar = false;
		Cursor = Cursors.Cross;

		Left = SystemParameters.VirtualScreenLeft;
		Top = SystemParameters.VirtualScreenTop;
		Width = SystemParameters.VirtualScreenWidth;
		Height = SystemParameters.VirtualScreenHeight;
		Content = _canvas;
		_selection.Stroke = Brushes.White;
		_selection.StrokeThickness = 2;
		_selection.Fill = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
		_selection.Visibility = Visibility.Collapsed;
		_canvas.Children.Add(_selection);
		KeyDown += OnKeyDown;
		MouseLeftButtonDown += OnMouseDown;
		MouseMove += OnMouseMove;
		MouseLeftButtonUp += OnMouseUp;
	}

	private void OnKeyDown(object sender, KeyEventArgs e) {
		if (e.Key == Key.Escape) {
			Close();
		}
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e) {
		_start = PointToScreen(e.GetPosition(this));
		_selection.Visibility = Visibility.Visible;
		CaptureMouse();
	}

	private void OnMouseMove(object sender, MouseEventArgs e) {
		if (_start is null || !IsMouseCaptured) {
			return;
		}
		Point end = PointToScreen(e.GetPosition(this));
		double x1 = Math.Min(_start.Value.X, end.X);
		double y1 = Math.Min(_start.Value.Y, end.Y);
		double x2 = Math.Max(_start.Value.X, end.X);
		double y2 = Math.Max(_start.Value.Y, end.Y);
		_currentScreenRect = new Rect(x1, y1, x2 - x1, y2 - y1);
		Point tl = PointFromScreen(new Point(_currentScreenRect.Left, _currentScreenRect.Top));
		Point br = PointFromScreen(new Point(_currentScreenRect.Right, _currentScreenRect.Bottom));
		Canvas.SetLeft(_selection, tl.X);
		Canvas.SetTop(_selection, tl.Y);
		_selection.Width = Math.Max(0, br.X - tl.X);
		_selection.Height = Math.Max(0, br.Y - tl.Y);
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e) {
		if (_start is null) {
			return;
		}
		ReleaseMouseCapture();

		if (_currentScreenRect.Width < 2 || _currentScreenRect.Height < 2) {
			_selection.Visibility = Visibility.Collapsed;
			_start = null;
			return;
		}
		Hide();
		try {
			using Bitmap bmp = CaptureScreenRect(_currentScreenRect);
			string path = Path.Combine(
				Path.GetTempPath(),
				$"minisnip_{DateTime.Now:yyyyMMdd-HHmmss-fff}.png"
			);
			bmp.Save(path, ImageFormat.Png);
			BitmapSource img = ConvertToWpfImageSource(bmp);
			Clipboard.SetImage(img);
		} finally {
			Close();
		}
	}
	private static Bitmap CaptureScreenRect(Rect r) {
		int x = (int)Math.Round(r.X);
		int y = (int)Math.Round(r.Y);
		int w = (int)Math.Round(r.Width);
		int h = (int)Math.Round(r.Height);

		if (w <= 0) {
			w = 1;
		}
		if (h <= 0) {
			h = 1;
		}
		Bitmap bmp = new(w, h, PixelFormat.Format32bppArgb);
		using Graphics g = Graphics.FromImage(bmp);
		g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
		return bmp;
	}
	private static BitmapSource ConvertToWpfImageSource(Bitmap bmp) {
		IntPtr hBitmap = bmp.GetHbitmap();
		try {
			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
				hBitmap, IntPtr.Zero, Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions()
			);
		} finally {
			DeleteObject(hBitmap);
		}
	}
	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr hObject);
}