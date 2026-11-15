using System.ComponentModel;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class CommonSettings : BaseModel
{
	public int InBoxId { get; set; } = -1;

	public string LoginUserId { get; set; } = "";

	public string LoginUserAuth { get; set; } = "";

	public string LoginUserName { get; set; } = "";

	public string LoginAvatarUrl { get; set; } = "";

	public string InServerBoxId { get; set; } = "";

	public string LocalTimeZone { get; set; } = "";

	public bool CheckTaskTimeString { get; set; }

	public string AccountType { get; set; } = "mail_account";

	public int ProxyType { get; set; }

	public string ProxyAddress { get; set; } = "";

	public string ProxyPort { get; set; } = "";

	public string ProxyUsername { get; set; } = "";

	public string ProxyPassword { get; set; } = "";

	public string ProxyDomain { get; set; } = "";

	public string UserChooseLanguage { get; set; } = "";

	public int GrayVersionDate { get; set; } = 20000101;

	public long DomainInvalidTime { get; set; }

	public string CustomThemeLocation { get; set; } = "";

	public string CustomThemeColor { get; set; } = "";

	[NotNull]
	[DefaultValue("1")]
	public double ThemeImageOpacity { get; set; } = 1.0;

	public int ThemeImageBlurRadius { get; set; }

	[NotNull]
	[DefaultValue("0.9")]
	public double ShowAreaOpacity { get; set; } = 0.9;

	public string UserDeviceString { get; set; }

	[NotNull]
	[DefaultValue("14")]
	public int BaseFontSize { get; set; }

	public string SkipVersion { get; set; } = "";

	public string AppIconKey { get; set; }

	public int ShowWhenStart { get; set; }

	public bool UseCustomWindowChrome { get; set; }

	public bool FirstInstall { get; set; }

	public string LastLoginAccount { get; set; } = "";

	public string OtherSettings { get; set; }

	public CommonOtherSettings GetOtherSettings()
	{
		if (!string.IsNullOrEmpty(OtherSettings))
		{
			return JsonConvert.DeserializeObject<CommonOtherSettings>(OtherSettings);
		}
		return new CommonOtherSettings();
	}

	public void SetOtherSettings(CommonOtherSettings settings)
	{
		OtherSettings = JsonConvert.SerializeObject(settings);
	}
}
