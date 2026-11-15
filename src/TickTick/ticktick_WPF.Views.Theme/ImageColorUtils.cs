using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ticktick_WPF.Views.Theme;

public class ImageColorUtils
{
	public static PixelColor[,] GetPixels(BitmapSource source)
	{
		if (source.Format != PixelFormats.Bgra32)
		{
			source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0.0);
		}
		PixelColor[,] array = new PixelColor[source.PixelHeight, source.PixelWidth];
		int stride = source.PixelWidth * source.Format.BitsPerPixel / 8;
		GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
		source.CopyPixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), gCHandle.AddrOfPinnedObject(), array.GetLength(0) * array.GetLength(1) * 4, stride);
		gCHandle.Free();
		return array;
	}
}
