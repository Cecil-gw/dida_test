using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ticktick_WPF.Properties;

[CompilerGenerated]
[GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.4.0.0")]
internal sealed class Settings : ApplicationSettingsBase
{
	private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

	public static Settings Default => defaultInstance;

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("False")]
	public bool NeedRestart
	{
		get
		{
			return (bool)this["NeedRestart"];
		}
		set
		{
			this["NeedRestart"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string ApiTest
	{
		get
		{
			return (string)this["ApiTest"];
		}
		set
		{
			this["ApiTest"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public long LastPullHolidayTime
	{
		get
		{
			return (long)this["LastPullHolidayTime"];
		}
		set
		{
			this["LastPullHolidayTime"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public long LastWindowActiveTime
	{
		get
		{
			return (long)this["LastWindowActiveTime"];
		}
		set
		{
			this["LastWindowActiveTime"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string YearPromoClosedUsers
	{
		get
		{
			return (string)this["YearPromoClosedUsers"];
		}
		set
		{
			this["YearPromoClosedUsers"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("")]
	public string YearPromo
	{
		get
		{
			return (string)this["YearPromo"];
		}
		set
		{
			this["YearPromo"] = value;
		}
	}

	[UserScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public long ActiveTime
	{
		get
		{
			return (long)this["ActiveTime"];
		}
		set
		{
			this["ActiveTime"] = value;
		}
	}

	[ApplicationScopedSetting]
	[DebuggerNonUserCode]
	[DefaultSettingValue("0")]
	public int DbVersion => (int)this["DbVersion"];

	private void SettingChangingEventHandler(object sender, SettingChangingEventArgs e)
	{
	}

	private void SettingsSavingEventHandler(object sender, CancelEventArgs e)
	{
	}
}
