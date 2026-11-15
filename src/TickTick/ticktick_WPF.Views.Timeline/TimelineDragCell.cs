using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Timeline;

public class TimelineDragCell : UserControl, IComponentConnector
{
	internal Grid Cell;

	internal Rectangle BackgroundBorder;

	internal Grid InfoTile;

	internal EmjTextBlock TitleText;

	internal TaskCircleProgress Progress;

	internal Border AvatarBorder;

	internal Thumb StartDateThumb;

	internal Thumb EndDateThumb;

	private bool _contentLoaded;

	public TimelineDragCell(TimelineCellViewModel cellModel, double width)
	{
		InitializeComponent();
		Open(cellModel, width);
	}

	public void Open(TimelineCellViewModel cellModel, double width)
	{
		base.DataContext = cellModel;
		InfoTile.Width = width;
		SetTextProgressAndAvatarWidth(cellModel);
		bool flag = cellModel.BatchSelected || (cellModel.DisplayModel.Type != DisplayType.Task && cellModel.DisplayModel.Type != DisplayType.Agenda) || !cellModel.Editable;
		StartDateThumb.Visibility = (flag ? Visibility.Hidden : Visibility.Visible);
		StartDateThumb.IsHitTestVisible = !flag;
		EndDateThumb.Visibility = (flag ? Visibility.Hidden : Visibility.Visible);
		EndDateThumb.IsHitTestVisible = !flag;
	}

	private void SetTextProgressAndAvatarWidth(TimelineCellViewModel model)
	{
		if (model == null)
		{
			return;
		}
		int num = 14;
		double num2 = InfoTile.Width;
		bool flag = model.AvatarUrl != "-1" && !string.IsNullOrEmpty(model.AvatarUrl);
		bool flag2 = model.Progress > 0 && model.Status == 0;
		double num3 = (string.IsNullOrEmpty(model.Title) ? 0.0 : Utils.MeasureString(model.Title, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width);
		if (num3 < 70.0)
		{
			if (flag2 && num2 <= 40.0)
			{
				flag2 = num2 - (double)num - num3 >= 16.0;
			}
			if (flag && num2 <= 70.0)
			{
				flag = num2 - (double)num - num3 - (double)(flag2 ? 16 : 0) >= 22.0;
			}
		}
		else
		{
			flag2 = flag2 && num2 > 40.0;
			flag = flag && num2 > 70.0;
		}
		num += (flag2 ? 16 : 0);
		num += (flag ? 22 : 0);
		if (!double.IsPositiveInfinity(num2))
		{
			num2 -= (double)num;
			if (num2 < 6.0)
			{
				num2 = 0.0;
			}
		}
		TitleText.MaxWidth = ((!model.Inline) ? Math.Min(num2, 120.0) : num2);
		Progress.Visibility = ((!flag2) ? Visibility.Collapsed : Visibility.Visible);
		AvatarBorder.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/timeline/timelinedragcell.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			Cell = (Grid)target;
			break;
		case 2:
			BackgroundBorder = (Rectangle)target;
			break;
		case 3:
			InfoTile = (Grid)target;
			break;
		case 4:
			TitleText = (EmjTextBlock)target;
			break;
		case 5:
			Progress = (TaskCircleProgress)target;
			break;
		case 6:
			AvatarBorder = (Border)target;
			break;
		case 7:
			StartDateThumb = (Thumb)target;
			break;
		case 8:
			EndDateThumb = (Thumb)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
