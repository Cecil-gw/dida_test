using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emoji.Wpf;
using TickTickUtils;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Util;

public static class EmojiSearchHelper
{
	private static readonly Dictionary<string, EmojiKeyLists> EmojiSearchDict;

	private static readonly Dictionary<string, string> BlackList;

	private static readonly Dictionary<string, List<string>> PresetList;

	private static Dictionary<string, string> NumEmjDict;

	static EmojiSearchHelper()
	{
		EmojiSearchDict = new Dictionary<string, EmojiKeyLists>();
		BlackList = new Dictionary<string, string> { { "工作", "\ud83d\udc15\u200d\ud83e\uddba" } };
		PresetList = new Dictionary<string, List<string>>
		{
			{
				"工作",
				new List<string> { "\ud83d\udcbc", "\ud83d\udda5\ufe0f", "\ud83d\udcbb", "\ud83d\udcc5", "\ud83d\udcdd", "\ud83d\udcc2", "\ud83d\udcce", "\ud83c\udfaf" }
			},
			{
				"work",
				new List<string> { "\ud83d\udcbc", "\ud83d\udda5\ufe0f", "\ud83d\udcbb", "\ud83d\udcc5", "\ud83d\udcdd", "\ud83d\udcc2", "\ud83d\udcce", "\ud83c\udfaf" }
			},
			{
				"学习",
				new List<string> { "\ud83d\udcda", "\ud83d\udcdd", "\ud83d\udcd6", "\ud83d\udcd1", "✏\ufe0f", "\ud83d\udc68\u200d\ud83c\udfeb", "\ud83d\udc69\u200d\ud83c\udfeb", "\ud83c\udf93" }
			},
			{
				"study",
				new List<string> { "\ud83d\udcda", "\ud83d\udcdd", "\ud83d\udcd6", "\ud83d\udcd1", "✏\ufe0f", "\ud83d\udc68\u200d\ud83c\udfeb", "\ud83d\udc69\u200d\ud83c\udfeb", "\ud83c\udf93" }
			},
			{
				"生活",
				new List<string> { "\ud83c\udfe1", "\ud83c\udfe0", "\ud83c\udf7d\ufe0f", "\ud83d\udecb\ufe0f", "\ud83d\uded2", "\ud83c\udf1e", "\ud83d\udc5a", "\ud83c\udf40" }
			},
			{
				"life",
				new List<string> { "\ud83c\udfe1", "\ud83c\udfe0", "\ud83c\udf7d\ufe0f", "\ud83d\udecb\ufe0f", "\ud83d\uded2", "\ud83c\udf1e", "\ud83d\udc5a", "\ud83c\udf40" }
			},
			{
				"健康",
				new List<string> { "\ud83c\udfc3\u200d♀\ufe0f", "\ud83e\uddd8\u200d♂\ufe0f", "\ud83d\udcaa", "\ud83c\udf4e", "\ud83c\udf4a", "\ud83e\ude7a", "\ud83d\udca7", "\ud83d\udc89" }
			},
			{
				"health",
				new List<string> { "\ud83c\udfc3\u200d♀\ufe0f", "\ud83e\uddd8\u200d♂\ufe0f", "\ud83d\udcaa", "\ud83c\udf4e", "\ud83c\udf4a", "\ud83e\ude7a", "\ud83d\udca7", "\ud83d\udc89" }
			},
			{
				"旅行",
				new List<string> { "✈\ufe0f", "\ud83c\udf0d", "\ud83c\udf92", "\ud83c\udfde\ufe0f", "\ud83d\udcf8", "⛷\ufe0f", "\ud83c\udfd6\ufe0f", "\ud83c\udf05" }
			},
			{
				"travel",
				new List<string> { "✈\ufe0f", "\ud83c\udf0d", "\ud83c\udf92", "\ud83c\udfde\ufe0f", "\ud83d\udcf8", "⛷\ufe0f", "\ud83c\udfd6\ufe0f", "\ud83c\udf05" }
			},
			{
				"娱乐",
				new List<string> { "\ud83c\udfae", "\ud83c\udfac", "\ud83c\udfa7", "\ud83c\udfa4", "\ud83c\udfb6", "\ud83c\udfb2", "\ud83c\udfa1", "\ud83c\udfa8" }
			},
			{
				"entertainment",
				new List<string> { "\ud83c\udfae", "\ud83c\udfac", "\ud83c\udfa7", "\ud83c\udfa4", "\ud83c\udfb6", "\ud83c\udfb2", "\ud83c\udfa1", "\ud83c\udfa8" }
			}
		};
		NumEmjDict = new Dictionary<string, string>
		{
			{ "0\u20e3", "0\ufe0f\u20e3" },
			{ "1\u20e3", "1\ufe0f\u20e3" },
			{ "2\u20e3", "2\ufe0f\u20e3" },
			{ "3\u20e3", "3\ufe0f\u20e3" },
			{ "4\u20e3", "4\ufe0f\u20e3" },
			{ "5\u20e3", "5\ufe0f\u20e3" },
			{ "6\u20e3", "6\ufe0f\u20e3" },
			{ "7\u20e3", "7\ufe0f\u20e3" },
			{ "8\u20e3", "8\ufe0f\u20e3" },
			{ "9\u20e3", "9\ufe0f\u20e3" }
		};
		GetEmojiSearchDict();
	}

	public static void Init()
	{
	}

	private static void GetEmojiSearchDict()
	{
		LoadCiKeys();
		LoadSearchDict(AppPaths.LocaleDir + "\\en-US\\emojiSearch.txt", "EnKeys");
		LoadSearchDict(AppPaths.LocaleDir + "\\zh-CN\\emojiSearch.txt", "CnKeys");
	}

	private static async Task LoadCiKeys()
	{
		if (!Utils.IsCn())
		{
			string text = App.Ci.ToString();
			string filePath = AppPaths.DataDir + "Locales\\" + text + "\\emojiSearch.txt";
			await IOUtils.CheckResourceExist(AppPaths.DataDir + "Locales\\" + text + "\\", "emojiSearch.txt", "https://" + BaseUrl.PullDomain + "/emoji/v0/" + text.Substring(0, 2) + ".txt");
			if (File.Exists(filePath))
			{
				LoadSearchDict(filePath, "CiKeys");
			}
		}
	}

	private static void LoadSearchDict(string file, string listName)
	{
		if (!File.Exists(file))
		{
			return;
		}
		foreach (string item in File.ReadLines(file).ToList())
		{
			List<string> list = item.SplitByStr("=>");
			if (list.Count == 2)
			{
				string key = list[0];
				if (NumEmjDict.ContainsKey(key))
				{
					key = NumEmjDict[key];
				}
				if (EmojiSearchDict.ContainsKey(key))
				{
					EmojiSearchDict[key].AddToList(list[1], listName);
					continue;
				}
				EmojiKeyLists emojiKeyLists = new EmojiKeyLists();
				emojiKeyLists.AddToList(list[1], listName);
				EmojiSearchDict.Add(key, emojiKeyLists);
			}
		}
	}

	public static List<string> GetMatchedEmoji(string searchKey)
	{
		List<string> keys = searchKey.Split(' ').ToList();
		List<string> list = new List<string>();
		if (PresetList.TryGetValue(searchKey, out var value))
		{
			list.AddRange(value);
		}
		else
		{
			foreach (KeyValuePair<string, EmojiKeyLists> item in EmojiSearchDict)
			{
				Match match = EmojiData.MatchOne.Match(item.Key);
				if (match.Success && !(match.Groups[0].Value != item.Key) && !IsInBlackList(searchKey, match.Groups[0].Value) && item.Value.CheckKeys(keys))
				{
					list.Add(item.Key);
				}
			}
		}
		return list;
	}

	private static bool IsInBlackList(string key, string val)
	{
		if (BlackList.TryGetValue(key, out var value))
		{
			return value == val;
		}
		return false;
	}
}
