using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace TickTickUtils;

public static class GlobalSettings
{
	public static CultureInfo Ci = CultureInfo.CurrentCulture;

	public static string UserId { get; set; }

	public static string AppVersion { get; set; }

	public static bool IsAdmin { get; set; }

	public static Dispatcher Dispatcher { get; set; }

	public static Application AppInstance { get; set; }

	public static bool UseCustomWindowChrome { get; set; }
}
