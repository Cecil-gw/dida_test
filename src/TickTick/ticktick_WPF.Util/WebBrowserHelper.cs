using System.Reflection;
using System.Windows.Controls;

namespace ticktick_WPF.Util;

public class WebBrowserHelper
{
	public static void SetErrorSilent(WebBrowser browser)
	{
		browser.Navigating += delegate
		{
			FieldInfo field = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (!(field == null))
			{
				object value = field.GetValue(browser);
				value?.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, value, new object[1] { true });
			}
		};
	}
}
