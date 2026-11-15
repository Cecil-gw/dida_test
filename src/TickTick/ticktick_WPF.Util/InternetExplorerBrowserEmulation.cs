using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace ticktick_WPF.Util;

public class InternetExplorerBrowserEmulation
{
	private const string InternetExplorerRootKey = "Software\\Microsoft\\Internet Explorer";

	private const string BrowserEmulationKey = "Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";

	private static BrowserEmulationVersion _browserVersion;

	private static int GetInternetExplorerMajorVersion()
	{
		int result = 0;
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Internet Explorer");
			if (registryKey != null)
			{
				object obj = registryKey.GetValue("svcVersion", null) ?? registryKey.GetValue("Version", null);
				if (obj != null)
				{
					string text = obj.ToString();
					int num = text.IndexOf('.');
					if (num != -1)
					{
						int.TryParse(text.Substring(0, num), out result);
					}
				}
			}
		}
		catch (SecurityException)
		{
			return 10;
		}
		catch (UnauthorizedAccessException)
		{
			return 10;
		}
		return result;
	}

	private static bool SetBrowserEmulationVersion(BrowserEmulationVersion browserEmulationVersion)
	{
		bool result = false;
		try
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", writable: true);
			if (registryKey != null)
			{
				string fileName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
				if (browserEmulationVersion != BrowserEmulationVersion.Default)
				{
					registryKey.SetValue(fileName, (int)browserEmulationVersion, RegistryValueKind.DWord);
				}
				else if (fileName != null)
				{
					registryKey.DeleteValue(fileName, throwOnMissingValue: false);
				}
				result = true;
			}
		}
		catch (SecurityException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		return result;
	}

	public static bool SetBrowserEmulationVersion()
	{
		int internetExplorerMajorVersion = GetInternetExplorerMajorVersion();
		return SetBrowserEmulationVersion(_browserVersion = ((internetExplorerMajorVersion >= 11) ? BrowserEmulationVersion.Version11 : (internetExplorerMajorVersion switch
		{
			10 => BrowserEmulationVersion.Version10, 
			9 => BrowserEmulationVersion.Version9, 
			8 => BrowserEmulationVersion.Version8, 
			_ => BrowserEmulationVersion.Version7, 
		})));
	}

	private static BrowserEmulationVersion GetBrowserEmulationVersion()
	{
		BrowserEmulationVersion browserEmulationVersion = BrowserEmulationVersion.Default;
		try
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION", writable: true);
			if (registryKey != null)
			{
				string fileName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
				object value = registryKey.GetValue(fileName, null);
				if (value != null)
				{
					browserEmulationVersion = (BrowserEmulationVersion)Convert.ToInt32(value);
				}
			}
		}
		catch (SecurityException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		_browserVersion = browserEmulationVersion;
		return browserEmulationVersion;
	}

	public static bool IsBrowserEmulationSet()
	{
		return GetBrowserEmulationVersion() >= BrowserEmulationVersion.Version10;
	}

	public static bool IsVersion11()
	{
		return _browserVersion >= BrowserEmulationVersion.Version11;
	}
}
