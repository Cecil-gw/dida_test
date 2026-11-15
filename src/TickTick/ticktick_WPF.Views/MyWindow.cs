using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class MyWindow : Window
{
	public MyWindow()
	{
		base.Title = Utils.GetAppName();
		Binding binding = new Binding("FontFamily")
		{
			Source = LocalSettings.Settings
		};
		SetBinding(Control.FontFamilyProperty, binding);
		base.UseLayoutRounding = true;
	}
}
