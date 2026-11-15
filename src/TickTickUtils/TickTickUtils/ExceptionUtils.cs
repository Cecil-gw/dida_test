using System;
using System.Globalization;

namespace TickTickUtils;

public static class ExceptionUtils
{
	private const string ExceptionMessageTemplate = "Operation System Version-> {0} ***** \nCurrentTime -> {1} ***** \nLocale -> {2} ***** \nDevice Location -> {3} ***** \nVersion -> {4} ***** \nException Type -> {5} ***** \nException Message -> {6} ***** \nException Data -> {7} ***** \n -> {8} ***** \n -> {9} ***** \n -> {10} ***** \n -> {11} ***** \n";

	public static string BuildExceptionMessage(Exception ex)
	{
		return $"Operation System Version-> {Environment.OSVersion.Version} ***** \nCurrentTime -> {DateTime.Now.ToString(CultureInfo.InvariantCulture)} ***** \nLocale -> {GlobalSettings.Ci} ***** \nDevice Location -> {CultureInfo.CurrentCulture} ***** \nVersion -> {GlobalSettings.AppVersion} ***** \nException Type -> {ex.GetType()} ***** \nException Message -> {ex.Message} ***** \nException Data -> {ex.Data} ***** \n -> {ex.InnerException} ***** \n -> {ex.Source} ***** \n -> {ex.TargetSite} ***** \n -> {ex.StackTrace} ***** \n";
	}
}
