using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using TickTickUtils;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Theme;

public class FontFamilyUtils
{
	public const string FFMicrosoftYH = "Microsoft YaHei UI";

	public const string FFDefault_CN = "Default_CN";

	public const string FFSource_CN = "SourceHansansSC_CN";

	public const string FFLXGW_CN = "LXGW_CN";

	public const string FFYozai_CN = "Yozai_CN";

	public const string FF975Maru_CN = "975Maru_CN";

	public const string FFHarmony_CN = "HarmonyOS";

	public const string FFDefault_EN = "Default_EN";

	public const string FFRoboto_EN = "Roboto";

	public const string FFArial_EN = "Arial";

	public const string FFInter_EN = "Inter";

	public const string FFPoppins_EN = "Poppins";

	public const string FFNunito_EN = "Nunito";

	public static readonly Dictionary<string, string> FontFamilyNameDict = new Dictionary<string, string>
	{
		{ "Default_CN", "默认字体" },
		{ "HarmonyOS", "鸿蒙字体" },
		{ "SourceHansansSC_CN", "思源黑体" },
		{ "LXGW_CN", "霞鹜文楷" },
		{ "Yozai_CN", "悠哉字体" },
		{ "975Maru_CN", "975圆体" }
	};

	public static readonly Dictionary<string, string> FontFamilyAuthDict = new Dictionary<string, string>
	{
		{ "LXGW_CN", "落霞孤鹜" },
		{ "Yozai_CN", "落霞孤鹜" },
		{ "975Maru_CN", "落霞孤鹜" },
		{ "Roboto", "Copyright Christian Robertson" },
		{ "HarmonyOS", "HarmonyOS Sans" }
	};

	public static readonly Dictionary<string, string> FontUserEventDict = new Dictionary<string, string>
	{
		{ "LXGW_CN", "fonts_lxgw" },
		{ "SourceHansansSC_CN", "fonts_source_han_sans" },
		{ "Yozai_CN", "fonts_yozai" },
		{ "975Maru_CN", "fonts_975_maru" },
		{ "HarmonyOS", "fonts_harmony" },
		{ "Inter", "fonts_inter" },
		{ "Poppins", "fonts_poppins" },
		{ "Nunito", "fonts_nunito" },
		{ "Arial", "fonts_arial" },
		{ "Roboto", "fonts_roboto" }
	};

	public static readonly Dictionary<string, string> FontFamilyCopyRightLinkDict = new Dictionary<string, string>
	{
		{ "LXGW_CN", "https://github.com/lxgw/LxgwWenKai" },
		{ "Yozai_CN", "https://github.com/lxgw/yozai-font" },
		{ "975Maru_CN", "https://github.com/lxgw/975maru" },
		{ "HarmonyOS", "https://developer.harmonyos.com/cn/docs/design/font-0000001157868583" },
		{ "Roboto", "https://fonts.google.com/specimen/Roboto/about?sort=popularity" }
	};

	public static readonly Dictionary<string, string> FontFamilyFileNameDict = new Dictionary<string, string>
	{
		{ "SourceHansansSC_CN", "SourceHanSansSC-VF.ttf" },
		{ "HarmonyOS", "HarmonyOS_Sans_SC_Regular.ttf|HarmonyOS_Sans_SC_Bold.ttf" },
		{ "LXGW_CN", "LXGW_GB.ttf" },
		{ "Yozai_CN", "Yozai.ttf" },
		{ "975Maru_CN", "975Maru.ttf" }
	};

	public static readonly Dictionary<string, string> FontFamilyDict = new Dictionary<string, string>
	{
		{ "Default_EN", "Microsoft YaHei UI" },
		{ "Default_CN", "Microsoft YaHei UI" },
		{
			"Inter",
			AppPaths.ENTTFDir + "/#Inter,Microsoft YaHei UI"
		},
		{ "Arial", "Arial,Microsoft YaHei UI" },
		{
			"Poppins",
			AppPaths.ENTTFDir + "/#Poppins,Microsoft YaHei UI"
		},
		{
			"Nunito",
			AppPaths.ENTTFDir + "/#Roboto,Microsoft YaHei UI"
		},
		{
			"Roboto",
			AppPaths.ENTTFDir + "/#Nunito,Microsoft YaHei UI"
		},
		{
			"HarmonyOS",
			AppPaths.TTFDir + "/#HarmonyOS Sans SC"
		},
		{
			"SourceHansansSC_CN",
			AppPaths.TTFDir + "/#Source Han Sans SC VF"
		},
		{
			"LXGW_CN",
			AppPaths.TTFDir + "/#霞鹜文楷 GB 屏幕阅读版," + AppPaths.TTFDir + "/#霞鹜文楷 屏幕阅读版"
		},
		{
			"Yozai_CN",
			AppPaths.TTFDir + "/#悠哉字体"
		},
		{
			"975Maru_CN",
			AppPaths.TTFDir + "/#975 圆体"
		}
	};

	public static HashSet<string> NewDownloadFont = new HashSet<string>();

	public static string SourceHanSansFont => GetFontFamily("SourceHansansSC_CN");

	public static string SourceHarmony => GetFontFamily("HarmonyOS");

	public static string SourceRoboto => GetFontFamily("Roboto");

	public static string SourceYahei => GetFontFamily("Default_CN");

	public static string SourceInter => GetFontFamily("Inter");

	public static string SourcePoppins => GetFontFamily("Poppins");

	public static string SourceNunito => GetFontFamily("Nunito");

	public static string SourceArial => GetFontFamily("Arial");

	public static string SourceInterNumber => AppPaths.ENTTFDir + "/#Inter number";

	public static string GetFontFamily(string fontKey)
	{
		string fontValue = GetFontValue(fontKey, FontFamilyDict);
		if (!string.IsNullOrEmpty(fontValue))
		{
			return fontValue;
		}
		return GetFontValue("Default_CN", FontFamilyDict);
	}

	public static bool ExistFontFile(string fontKey)
	{
		if (Utils.IsEn())
		{
			return true;
		}
		string fontFileName = GetFontFileName(fontKey);
		if (!string.IsNullOrEmpty(fontFileName))
		{
			foreach (string item in fontFileName.Split('|').ToList())
			{
				if (!File.Exists(AppPaths.TTFDir + item) || new FileInfo(AppPaths.TTFDir + item).Length < 7000000)
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	public static FontFamily GetFontFamilyByKey(string fontKey)
	{
		if (!ExistFontFile(fontKey))
		{
			fontKey = null;
		}
		if (!(fontKey == ""))
		{
			switch (fontKey)
			{
			case "Default_CN":
			case "Default_EN":
			case null:
				break;
			default:
				return new FontFamily(GetFontFamily(fontKey));
			case "SourceHansansSC_CN":
				return (FontFamily)App.Instance.FindResource("FontSourceHanSans");
			case "HarmonyOS":
				return (FontFamily)App.Instance.FindResource("FontHarmony");
			case "Roboto":
				return (FontFamily)App.Instance.FindResource("FontRoboto");
			case "Poppins":
				return (FontFamily)App.Instance.FindResource("FontPoppins");
			case "Inter":
				return (FontFamily)App.Instance.FindResource("FontInter");
			case "Nunito":
				return (FontFamily)App.Instance.FindResource("FontNunito");
			case "Arial":
				return (FontFamily)App.Instance.FindResource("FontArial");
			}
		}
		return (FontFamily)App.Instance.FindResource("FontYahei");
	}

	public static string GetFontName(string fontKey)
	{
		return GetFontValue(fontKey, FontFamilyNameDict);
	}

	public static string GetFontFileName(string fontKey)
	{
		return GetFontValue(fontKey, FontFamilyFileNameDict);
	}

	public static string GetFontAuth(string fontKey)
	{
		return GetFontValue(fontKey, FontFamilyAuthDict);
	}

	public static string GetFontCopyRightLink(string fontKey)
	{
		return GetFontValue(fontKey, FontFamilyCopyRightLinkDict);
	}

	public static string GetFontValue(string fontKey, Dictionary<string, string> dict)
	{
		if (!string.IsNullOrEmpty(fontKey) && dict.ContainsKey(fontKey))
		{
			return dict[fontKey];
		}
		return string.Empty;
	}

	public static string GetFontEventName(string fontKey)
	{
		return GetFontValue(fontKey, FontUserEventDict);
	}

	public static FontFamily GetFocusFontFamily()
	{
		if (!CanSetFontFamily())
		{
			return GetFontFamilyByKey("Arial");
		}
		string appFontFamily = LocalSettings.Settings.ExtraSettings.AppFontFamily;
		if (appFontFamily == "Inter" || appFontFamily == "Poppins")
		{
			return (FontFamily)App.Instance.FindResource("FontInterNumber");
		}
		return GetFontFamilyByKey(LocalSettings.Settings.ExtraSettings.AppFontFamily);
	}

	private static bool CanSetFontFamily()
	{
		if (!Utils.IsDida() || !Utils.IsZhCn())
		{
			if (!Utils.IsDida())
			{
				return Utils.IsEn();
			}
			return false;
		}
		return true;
	}

	public static async Task<string> DownLoadFontFile(string fontKey)
	{
		string fontFileName = GetFontFileName(fontKey);
		if (!string.IsNullOrEmpty(fontFileName))
		{
			List<string> list = fontFileName.Split('|').ToList();
			bool needDownload = false;
			foreach (string fileName in list)
			{
				if (!File.Exists(AppPaths.TTFDir + fileName) || new FileInfo(AppPaths.TTFDir + fileName).Length < 7000000)
				{
					needDownload = true;
					string remotePath = "https://" + BaseUrl.GetPullDomain() + "/windows/font/" + fileName;
					await IOUtils.CheckResourceExist(AppPaths.TTFDir, fileName, remotePath);
					bool flag = !File.Exists(AppPaths.TTFDir + fileName);
					if (!flag && new FileInfo(AppPaths.TTFDir + fileName).Length < 7000000)
					{
						flag = true;
					}
					if (flag)
					{
						return Utils.GetString("DownloadFailed");
					}
				}
			}
			if (needDownload)
			{
				NewDownloadFont.Add(fontKey);
				return Utils.GetString("FontDownloadSuccessful");
			}
		}
		return null;
	}
}
