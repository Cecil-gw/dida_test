using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class MatrixModel : PreferenceBaseModel
{
	public const int CurrentVersion = 2;

	public string defaultViewId { get; set; }

	[JsonIgnore]
	public string LegacyActiveViewId { get; internal set; }

	[JsonProperty("activeViewId")]
	private string LegacyActiveView
	{
		set
		{
			LegacyActiveViewId = value;
		}
	}

	public List<QuadrantModel> quadrants { get; set; }

	public bool? show_completed { get; set; }

	public int version { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		bool result = false;
		if (model is MatrixModel matrixModel)
		{
			if (!matrixModel.IsEmpty())
			{
				if (matrixModel.mtime > base.mtime || (matrixModel.mtime == base.mtime && !string.IsNullOrEmpty(matrixModel.defaultViewId) && matrixModel.defaultViewId != defaultViewId))
				{
					if (!string.IsNullOrEmpty(matrixModel.defaultViewId))
					{
						defaultViewId = matrixModel.defaultViewId;
					}
					if (matrixModel.quadrants != null && matrixModel.quadrants.Count > 0)
					{
						quadrants = matrixModel.quadrants;
					}
					show_completed = matrixModel.show_completed;
					version = Math.Max(version, matrixModel.version);
					base.mtime = matrixModel.mtime;
					DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
					UtilLog.Info("MatrixModel.SetRemoteValue");
				}
				else
				{
					CheckSortOrder(matrixModel);
				}
				if (matrixModel.mtime < base.mtime)
				{
					result = true;
				}
			}
			else
			{
				base.mtime = Utils.GetNowTimeStampInMills();
				result = true;
			}
			if (!show_completed.HasValue)
			{
				show_completed = true;
				result = true;
			}
		}
		return result;
	}

	public static MatrixModel GetDefault()
	{
		UtilLog.Info("GetDefaultMatrix");
		MatrixModel matrixModel = new MatrixModel();
		matrixModel.mtime = 1L;
		matrixModel.version = 2;
		matrixModel.quadrants = new List<QuadrantModel>();
		for (int i = 1; i <= 4; i++)
		{
			matrixModel.quadrants.Add(QuadrantModel.GetDefault(i));
		}
		return matrixModel;
	}

	public bool IsEmpty()
	{
		if (string.IsNullOrEmpty(defaultViewId) && (quadrants == null || quadrants.Count == 0))
		{
			return !show_completed.HasValue;
		}
		return false;
	}

	public void SetShowComplete(bool show)
	{
		show_completed = show;
		base.mtime = Utils.GetNowTimeStampInMills();
	}

	public void SetDefaultView(string sid)
	{
		defaultViewId = sid;
		version = 2;
		base.mtime = Math.Max(Utils.GetNowTimeStampInMills(), LocalSettings.Settings.UserPreference.GetMaxMTime() + 1);
	}

	public void CheckSortOrder(MatrixModel remote)
	{
		if ((string.IsNullOrEmpty(defaultViewId) || string.IsNullOrEmpty(remote?.defaultViewId) || !(defaultViewId != remote.defaultViewId)) && remote?.quadrants != null && remote.quadrants.Any((QuadrantModel q) => q.sortOrder.HasValue) && quadrants.Any((QuadrantModel q) => !q.sortOrder.HasValue))
		{
			quadrants = remote.quadrants;
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
		}
	}

	public void ResetRule(bool isSimple = true)
	{
		foreach (QuadrantModel quadrant in quadrants)
		{
			if (quadrant.id.Length > 8)
			{
				int.TryParse(quadrant.id.Substring(8), out var result);
				quadrant.rule = MatrixManager.GetDefaultQuadrantRule((!isSimple) ? MatrixType.Complex : MatrixType.Simple, result);
			}
		}
	}

	public void InitSortOption()
	{
		foreach (QuadrantModel quadrant in quadrants)
		{
			quadrant.SortOption = quadrant.GetSortOption();
		}
		base.mtime = Utils.GetNowTimeStampInMills();
	}
}
