using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class EmptyIconsSourceModel
{
	public DrawingImage Icon0;

	public DrawingImage Icon1;

	public DrawingImage Icon2;

	public DrawingImage Icon3;

	public DrawingImage Icon4;

	public DrawingImage Icon5;

	public DrawingImage Icon6;

	public EmptyIconsSourceModel(string key, string bottom = "EmptyViewBottomDrawingImage")
		: this(key, new List<int> { 1, 2, 3, 4 }, bottom)
	{
	}

	public EmptyIconsSourceModel(string key, List<int> iconIndexes, string bottom = "EmptyViewBottomDrawingImage")
	{
		Icon0 = Utils.GetImageSource(bottom)?.Clone();
		Icon1 = ((!iconIndexes.Contains(1)) ? null : Utils.GetImageSource(key + 1)?.Clone());
		Icon2 = ((!iconIndexes.Contains(2)) ? null : Utils.GetImageSource(key + 2)?.Clone());
		Icon3 = ((!iconIndexes.Contains(3)) ? null : Utils.GetImageSource(key + 3)?.Clone());
		Icon4 = ((!iconIndexes.Contains(4)) ? null : Utils.GetImageSource(key + 4)?.Clone());
		Icon5 = ((!iconIndexes.Contains(5)) ? null : Utils.GetImageSource(key + 5)?.Clone());
		Icon6 = ((!iconIndexes.Contains(6)) ? null : Utils.GetImageSource(key + 6)?.Clone());
	}
}
