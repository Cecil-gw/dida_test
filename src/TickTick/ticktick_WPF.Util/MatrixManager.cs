using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class MatrixManager
{
	private static MatrixRuleModel _defaultMatrixRuleModel;

	public static QuadrantRulesModel GetDefaultQuadrantRules(MatrixType type)
	{
		if (_defaultMatrixRuleModel == null)
		{
			try
			{
				_defaultMatrixRuleModel = JsonConvert.DeserializeObject<MatrixRuleModel>(IOUtils.GetFileContentInResourceFile("/Assets/MatrixRulesJson.json"));
			}
			catch (Exception ex)
			{
				UtilLog.Warn(ex.Message);
			}
		}
		if (_defaultMatrixRuleModel == null)
		{
			return QuadrantRulesModel.GetDefault();
		}
		return _defaultMatrixRuleModel.GetQuadrantRules(type);
	}

	public static string GetDefaultQuadrantRule(MatrixType type, int level)
	{
		QuadrantRulesModel defaultQuadrantRules = GetDefaultQuadrantRules(type);
		if (defaultQuadrantRules != null)
		{
			switch (level)
			{
			case 1:
				return defaultQuadrantRules.Quadrant1;
			case 2:
				return defaultQuadrantRules.Quadrant2;
			case 3:
				return defaultQuadrantRules.Quadrant3;
			case 4:
				return defaultQuadrantRules.Quadrant4;
			}
		}
		return string.Empty;
	}

	public static QuadrantModel GetQuadrantByLevel(int level)
	{
		return MatrixProfileDao.GetActiveQuadrantsSync().FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + level) ?? QuadrantModel.GetDefault(level);
	}

	public static async Task<(int, string)> GetTaskQuadrantChangeString(int previous, string taskId)
	{
		int num = await TaskService.GetQuadrantTaskBelong(taskId, previous);
		if (num == previous)
		{
			return (previous, null);
		}
		if (num > 0)
		{
			string item = string.Format(Utils.GetString("MovedTo"), GetQuadrantNameByLevel(num));
			return (num, item);
		}
		return (-1, Utils.GetString("MoveOutOfMatrix"));
	}

	public static string GetQuadrantNameByLevel(int level)
	{
		QuadrantModel quadrantByLevel = GetQuadrantByLevel(level);
		if (string.IsNullOrEmpty(quadrantByLevel.name))
		{
			return Utils.GetString("MatrixTitleQuadrant" + level);
		}
		return quadrantByLevel.name;
	}

	public static string GetRuleSummary(string rule)
	{
		try
		{
			NormalFilterViewModel normalFilterViewModel = Parser.ToNormalModel(rule);
			List<string> list = new List<string>();
			AddSegment(list, GetProjectSummary(normalFilterViewModel));
			AddSegment(list, TagCardViewModel.ToNormalDisplayText(normalFilterViewModel.Tags ?? new List<string>()));
			AddSegment(list, DateCardViewModel.ToNormalDisplayText(normalFilterViewModel.DueDates ?? new List<string>()));
			AddSegment(list, PriorityCardViewModel.ToCardText(normalFilterViewModel.Priorities ?? new List<int>()));
			AddSegment(list, AssigneeCardViewModel.ToNormalDisplayText(normalFilterViewModel.Assignees ?? new List<string>()));
			AddSegment(list, TaskTypeViewModel.ToDisplayList(normalFilterViewModel.TaskTypes ?? new List<string>()));
			return (list.Count == 0) ? Utils.GetString("All") : string.Join(" & ", list);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("MatrixManager.GetRuleSummary " + ex.Message);
			return Utils.GetString("All");
		}
	}

	private static void AddSegment(ICollection<string> segments, string value)
	{
		if (!string.IsNullOrWhiteSpace(value) && value != Utils.GetString("All") && value != Utils.GetString("AllTags") && value != Utils.GetString("priority") && value != Utils.GetString("TaskType"))
		{
			segments.Add(value);
		}
	}

	private static string GetProjectSummary(NormalFilterViewModel normal)
	{
		string text = new ProjectOrGroupCardViewModel
		{
			Values = (normal.Projects ?? new List<string>()),
			GroupIds = (normal.Groups ?? new List<string>()),
			CalendarAccounts = (normal.CalendarAccounts ?? new List<string>()),
			Calendars = (normal.Calendars ?? new List<string>())
		}.ToCardText();
		if (!(text == Utils.GetString("lists")))
		{
			return text;
		}
		return Utils.GetString("All");
	}

	public static async Task UpdateAffectedQuadrantsOnTagChanged(string original, string revised)
	{
		if (await MatrixProfileDao.UpdateAffectedQuadrantsOnTagChangedAsync(original, revised))
		{
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
		}
	}

	public static void UpdateQuadrantsSortOrder(Dictionary<string, long> orders, object sender)
	{
		MatrixProfileDao.UpdateCurrentQuadrantOrdersSync(orders);
		GlobalEventManager.NotifyQuadrantSortChanged(sender);
		LocalSettings.Settings.OnMatrixQuadrantChanged();
	}

	public static bool CheckNeedUpdateDefaultRule(MatrixModel model)
	{
		if (model.version >= 2)
		{
			return false;
		}
		foreach (QuadrantModel quadrant in model.quadrants)
		{
			if (quadrant.id.Length > 8 && int.TryParse(quadrant.id.Substring(8), out var result))
			{
				if (GetDefaultQuadrantRule(MatrixType.Version0, result) != quadrant.rule)
				{
					return false;
				}
				continue;
			}
			return false;
		}
		return true;
	}

	public static async Task ResetMatrixRule(bool isSimple = true)
	{
		MatrixProfileDao.EnsureMigratedSync();
		MatrixModel matrixModel = new MatrixModel();
		MatrixModel matrixModel2 = matrixModel;
		matrixModel2.quadrants = await MatrixProfileDao.GetActiveQuadrantsAsync();
		MatrixModel display = matrixModel;
		display.ResetRule(isSimple);
		int i;
		for (i = 1; i <= 4; i++)
		{
			QuadrantModel quadrantModel = display.quadrants.FirstOrDefault((QuadrantModel q) => q.id == "quadrant" + i);
			if (quadrantModel != null)
			{
				await MatrixProfileDao.UpdateCurrentQuadrantAsync(i, quadrantModel.rule, quadrantModel.SortOption, quadrantModel.name);
			}
		}
		DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
	}
}
