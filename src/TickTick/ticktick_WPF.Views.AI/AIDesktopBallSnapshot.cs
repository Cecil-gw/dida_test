using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace ticktick_WPF.Views.AI;

internal sealed class AIDesktopBallSnapshot : NativeWindow, IDisposable
{
	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	private struct NativeSize
	{
		public int Width;

		public int Height;
	}

	private struct BitmapInfo
	{
		public uint Size;

		public int Width;

		public int Height;

		public ushort Planes;

		public ushort BitCount;

		public uint Compression;

		public uint ImageSize;

		public int XPixelsPerMeter;

		public int YPixelsPerMeter;

		public uint ColorsUsed;

		public uint ColorsImportant;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct NativeBlend
	{
		public byte Operation;

		public byte Flags;

		public byte ConstantAlpha;

		public byte AlphaFormat;
	}

	internal BitmapSource Bitmap { get; }

	internal Rect ScreenBounds { get; }

	internal double Opacity { get; private set; }

	internal bool IsOpen => base.Handle != IntPtr.Zero;

	internal bool IsVisible { get; private set; }

	[DllImport("user32.dll")]
	private static extern IntPtr GetDC(IntPtr window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(IntPtr window, IntPtr dc);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr window, int command);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateCompatibleDC(IntPtr dc);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(IntPtr dc);

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr pixels, IntPtr section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr value);

	[DllImport("user32.dll", EntryPoint = "UpdateLayeredWindow", SetLastError = true)]
	private static extern bool PresentBitmap(IntPtr window, IntPtr destinationDc, ref NativePoint destination, ref NativeSize size, IntPtr sourceDc, ref NativePoint source, uint colorKey, ref NativeBlend blend, uint flags);

	[DllImport("user32.dll", EntryPoint = "UpdateLayeredWindow", SetLastError = true)]
	private static extern bool UpdateOpacity(IntPtr window, IntPtr destinationDc, IntPtr destination, IntPtr size, IntPtr sourceDc, IntPtr source, uint colorKey, ref NativeBlend blend, uint flags);

	internal AIDesktopBallSnapshot(BitmapSource bitmap, Point position, double opacity)
	{
		Bitmap = bitmap;
		ScreenBounds = new Rect(Math.Round(position.X), Math.Round(position.Y), bitmap.PixelWidth, bitmap.PixelHeight);
		Opacity = opacity;
		try
		{
			CreateHandle(new CreateParams
			{
				Caption = "AI ball animation",
				Style = int.MinValue,
				ExStyle = 134742184,
				X = (int)ScreenBounds.X,
				Y = (int)ScreenBounds.Y,
				Width = bitmap.PixelWidth,
				Height = bitmap.PixelHeight
			});
			SubmitBitmap();
			ShowWindow(base.Handle, 4);
			DwmFlush();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	internal Point PointToScreen(Point point)
	{
		return new Point(ScreenBounds.X + point.X * Bitmap.DpiX / 96.0, ScreenBounds.Y + point.Y * Bitmap.DpiY / 96.0);
	}

	internal void Show()
	{
		SetOpacity(Opacity);
		IsVisible = true;
	}

	internal void SetOpacity(double opacity)
	{
		Opacity = opacity;
		if (IsOpen)
		{
			NativeBlend blend = Blend(opacity);
			UpdateOpacity(base.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0u, ref blend, 2u);
		}
	}

	private static NativeBlend Blend(double opacity)
	{
		return new NativeBlend
		{
			ConstantAlpha = (byte)Math.Round(Math.Max(0.0, Math.Min(1.0, opacity)) * 255.0),
			AlphaFormat = 1
		};
	}

	private void SubmitBitmap()
	{
		IntPtr dC = GetDC(IntPtr.Zero);
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		IntPtr intPtr3 = IntPtr.Zero;
		try
		{
			intPtr = CreateCompatibleDC(dC);
			BitmapInfo info = new BitmapInfo
			{
				Size = (uint)Marshal.SizeOf(typeof(BitmapInfo)),
				Width = Bitmap.PixelWidth,
				Height = -Bitmap.PixelHeight,
				Planes = 1,
				BitCount = 32
			};
			intPtr2 = CreateDIBSection(dC, ref info, 0u, out var pixels, IntPtr.Zero, 0u);
			if (intPtr == IntPtr.Zero || intPtr2 == IntPtr.Zero)
			{
				throw new Win32Exception();
			}
			intPtr3 = SelectObject(intPtr, intPtr2);
			if (intPtr3 == IntPtr.Zero || intPtr3 == new IntPtr(-1))
			{
				throw new Win32Exception();
			}
			int num = Bitmap.PixelWidth * 4;
			Bitmap.CopyPixels(Int32Rect.Empty, pixels, num * Bitmap.PixelHeight, num);
			NativePoint destination = new NativePoint
			{
				X = (int)ScreenBounds.X,
				Y = (int)ScreenBounds.Y
			};
			NativeSize size = new NativeSize
			{
				Width = Bitmap.PixelWidth,
				Height = Bitmap.PixelHeight
			};
			NativePoint source = default(NativePoint);
			NativeBlend blend = Blend(Math.Min(Opacity, 1.0 / 255.0));
			if (!PresentBitmap(base.Handle, dC, ref destination, ref size, intPtr, ref source, 0u, ref blend, 2u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
		finally
		{
			if (intPtr3 != IntPtr.Zero && intPtr3 != new IntPtr(-1))
			{
				SelectObject(intPtr, intPtr3);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				DeleteObject(intPtr2);
			}
			if (intPtr != IntPtr.Zero)
			{
				DeleteDC(intPtr);
			}
			if (dC != IntPtr.Zero)
			{
				ReleaseDC(IntPtr.Zero, dC);
			}
		}
	}

	public void Dispose()
	{
		Opacity = 0.0;
		IsVisible = false;
		if (IsOpen)
		{
			DestroyHandle();
		}
	}
}
