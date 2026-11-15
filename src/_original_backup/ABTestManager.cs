using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public static class ABTestManager
{
	private const string CleanTask = "202402_clean_task";

	private const string MatrixTab = "202401_matrix_tab";

	public const string NewUser = "newuser3_202403";

	private const string WindowChrome = "202403_window_chrome";

	private const string RemindCalculate = "2024_pc_remind";

	private const string PremiumV1 = "premium1_202403";

	private const string PremiumV2 = "premium2_202407";

	public const string AATestNewUser = "aatest_newuser_202406";

	public const string AIVoice = "ai_voice_202603";

	public const string WebView2 = "pc_webview2_202608";

	private const string OGroup = "_o";

	private const string AGroup = "_a";

	private const string BGroup = "_b";

	private const string CGroup = "_c";

	private const string NGroup = "_n";

	private static readonly List<string> AbTestList = new List<string> { "202402_clean_task", "premium1_202403", "premium2_202407", "newuser3_202403", "aatest_newuser_202406", "ai_voice_202603", "pc_webview2_202608" };

	public static async Task AssignGroupOnLaunch()
	{
		await GetAssignedTestGroup();
		await ApplyABTestGroup();
	}

	private static async Task GetAssignedTestGroup()
	{
		if (!Utils.IsNetworkAvailable())
		{
			UtilLog.Info("GetAssignedTestGroup no network");
			return;
		}
		TestCode2PlanCodeInfo testCode2PlanCodeInfo = await Communicator.GetABTestGroupResult(new BatchTestInfo
		{
			appCode = "608903cbe4b00f4b6d989f98",
			clientCode = "60890f038f0839d464599a56",
			testCodes = AbTestList,
			deviceId = App.UId,
			userId = LocalSettings.Settings.Common.LoginUserId
		});
		if (testCode2PlanCodeInfo?.testCode2PlanCode == null || testCode2PlanCodeInfo.testCode2PlanCode.Count <= 0)
		{
			return;
		}
		Dictionary<string, string> aBTestData = LocalSettings.Settings.ExtraSettings.ABTestData;
		foreach (string key in testCode2PlanCodeInfo.testCode2PlanCode.Keys)
		{
			UtilLog.Info("ABTestManager.GetAssignedTestGroup() code: " + key + ", result: " + testCode2PlanCodeInfo.testCode2PlanCode[key]);
			aBTestData[key] = testCode2PlanCodeInfo.testCode2PlanCode[key]?.Trim();
		}
		LocalSettings.Settings.ExtraSettings.ABTestData = aBTestData;
		LocalSettings.Settings.Save();
	}

	private static async Task ApplyABTestGroup()
	{
		if (!Utils.IsNetworkAvailable())
		{
			UtilLog.Info("AssignGroupOnLaunch no network");
			return;
		}
		Dictionary<string, string> code2Result = LocalSettings.Settings.ExtraSettings.ABTestData;
		foreach (string code in AbTestList.Where((string key) => !code2Result.ContainsKey(key)))
		{
			TabPlanData tabPlanData = await Communicator.ApplyABTestGroupResult(new TestInfo
			{
				appCode = "608903cbe4b00f4b6d989f98",
				clientCode = "60890f038f0839d464599a56",
				deviceId = App.UId,
				testCode = code,
				userId = LocalSettings.Settings.Common.LoginUserId
			});
			UtilLog.Info("ABTestManager.AssignGroupOnLaunch() code: " + code + ", result: " + tabPlanData);
			if (tabPlanData?.data != null)
			{
				code2Result[code] = tabPlanData.data.planCode?.Trim();
			}
		}
		LocalSettings.Settings.ExtraSettings.ABTestData = code2Result;
		LocalSettings.Settings.Save();
	}

	public static async Task<string> GetABTestGroupResult(string code)
	{
		if (!Utils.IsNetworkAvailable())
		{
			UtilLog.Info("AssignGroupOnLaunch no network");
			return "_n";
		}
		TabPlanData tabPlanData = await Communicator.ApplyABTestGroupResult(new TestInfo
		{
			appCode = "608903cbe4b00f4b6d989f98",
			clientCode = "60890f038f0839d464599a56",
			deviceId = App.UId,
			testCode = code,
			userId = LocalSettings.Settings.Common.LoginUserId
		});
		UtilLog.Info("ABTestManager.AssignGroupOnLaunch() code: " + code + ", result: " + tabPlanData);
		string a = tabPlanData?.data?.planCode?.ToLower().Trim();
		if (string.Equals(a, code + "_a", StringComparison.CurrentCultureIgnoreCase))
		{
			return "_a";
		}
		if (string.Equals(a, code + "_b", StringComparison.CurrentCultureIgnoreCase))
		{
			return "_b";
		}
		if (string.Equals(a, code + "_o", StringComparison.CurrentCultureIgnoreCase))
		{
			return "_o";
		}
		return "_n";
	}

	public static string TryGetABTestGroup(string code, bool log = true)
	{
		string value;
		string text = (LocalSettings.Settings.ExtraSettings.ABTestData.TryGetValue(code, out value) ? value : (code + "_o"));
		if (log)
		{
			UtilLog.Info("ABTestManager.TryGetABTestGroup() code: " + code + ", result: " + text);
		}
		return text;
	}

	public static bool IsTabWithMatrix()
	{
		return string.Equals(TryGetABTestGroup("202401_matrix_tab"), "202401_matrix_tab_a", StringComparison.CurrentCultureIgnoreCase);
	}

	public static bool IsCleanTask()
	{
		return string.Equals(TryGetABTestGroup("202402_clean_task"), "202402_clean_task_a", StringComparison.CurrentCultureIgnoreCase);
	}

	public static bool IsSystemWC()
	{
		return true;
	}

	public static string GetPremiumV1PlanCode()
	{
		return TryGetABTestGroup("premium1_202403");
	}

	public static void HandleBestTest(List<Best> abBest)
	{
		Dictionary<string, string> aBTestData = LocalSettings.Settings.ExtraSettings.ABTestData;
		foreach (Best item in abBest)
		{
			aBTestData[item.testCode] = item.planCode;
		}
		LocalSettings.Settings.ExtraSettings.ABTestData = aBTestData;
		LocalSettings.Settings.Save();
	}

	public static bool IsAGroup(string abTest)
	{
		return abTest == "_a";
	}

	public static bool IsBGroup(string abTest)
	{
		return abTest == "_b";
	}

	public static bool IsNewRemindCalculate()
	{
		return true;
	}

	public static bool IsAIVoiceEnabled()
	{
		return TryGetABTestGroup("ai_voice_202603") == "ai_voice_202603_a";
	}

	public static bool IsWebView2Enabled()
	{
		LocalSettings.Settings.ExtraSettings.ABTestData.TryGetValue("pc_webview2_202608", out var value);
		return IsWebView2GroupEnabled(value);
	}

	internal static bool IsWebView2GroupEnabled(string group)
	{
		string a = group?.Trim();
		if (!string.Equals(a, "pc_webview2_202608_a", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "pc_webview2_202608_b", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(a, "pc_webview2_202608_c", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static List<string> GetAllGroups()
	{
		return new List<string>
		{
			TryGetABTestGroup("premium1_202403", log: false),
			TryGetABTestGroup("premium2_202407", log: false),
			TryGetABTestGroup("newuser3_202403", log: false),
			TryGetABTestGroup("aatest_newuser_202406", log: false)
		};
	}

	public static bool UsePremiumV2CalendarProStyle()
	{
		string text = TryGetABTestGroup("premium2_202407");
		if (!(text == "premium2_202407_c"))
		{
			return text == "premium2_202407_a";
		}
		return true;
	}

	public static bool UsePremiumV2PayStyle()
	{
		string text = TryGetABTestGroup("premium2_202407");
		if (!(text == "premium2_202407_b"))
		{
			return text == "premium2_202407_c";
		}
		return true;
	}
}
