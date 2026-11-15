using System.Windows.Controls;
using System.Windows.Input;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views.Setting;

public class WidgetConfig : FeatureConfig
{
	internal class WidgetViewModel : FeatureViewModel
	{
		public new WidgetType Value { get; set; }

		public WidgetViewModel(WidgetType type, bool? isOpen)
		{
			Value = type;
			base.Title = GetTitle(type);
			base.Desc = Utils.GetString(type.ToString() + "WidgetDesc");
			base.IsOpen = isOpen == true;
			base.ShowOpen = isOpen.HasValue;
		}

		private string GetTitle(WidgetType type)
		{
			return Utils.GetString(type.ToString() + "Widget");
		}
	}

	public WidgetConfig()
	{
		InitializeComponent();
	}

	protected override void OnCheckBoxClick(object sender, MouseButtonEventArgs e)
	{
		Mouse.Capture(null);
		e.Handled = true;
		if (!(sender is CheckBox { DataContext: WidgetViewModel dataContext } checkBox))
		{
			return;
		}
		checkBox.IsChecked = !checkBox.IsChecked;
		dataContext.IsOpen = checkBox.IsChecked == true;
		switch (dataContext.Value)
		{
		case WidgetType.Calendar:
			if (ProChecker.CheckPro(ProType.CalendarWidget))
			{
				if (dataContext.IsOpen)
				{
					CalendarWidgetHelper.AddWidget();
				}
				else
				{
					CalendarWidgetHelper.CloseWidget();
				}
			}
			break;
		case WidgetType.Matrix:
			if (ProChecker.CheckPro(ProType.MatrixWidget))
			{
				if (dataContext.IsOpen)
				{
					MatrixWidgetHelper.AddWidget();
				}
				else
				{
					MatrixWidgetHelper.CloseWidget();
				}
			}
			break;
		}
		dataContext.NotifySettingTextChanged();
	}
}
