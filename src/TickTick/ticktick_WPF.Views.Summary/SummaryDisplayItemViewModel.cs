using System.Collections.Generic;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Summary;

public class SummaryDisplayItemViewModel : BaseViewModel
{
	private bool _enabled;

	private bool _isDragging;

	private bool _isDraggingOver;

	private readonly List<string> _proItems = new List<string> { "status", "dueDate", "parentTask", "tag" };

	private static readonly Dictionary<string, string> Key2NameMap = new Dictionary<string, string>
	{
		{ "status", "task_status" },
		{ "completedTime", "summary_completed_time" },
		{ "progress", "Progress" },
		{ "focus", "FocusData" },
		{ "project", "BelongList" },
		{ "dueDate", "task_time" },
		{ "title", "task_title" },
		{ "parentTask", "parent_task" },
		{ "tag", "tag" },
		{ "detail", "DetailContent" }
	};

	private static readonly Dictionary<string, List<string>> Key2StylesMap = new Dictionary<string, List<string>>
	{
		{
			"completedTime",
			new List<string> { "date", "time" }
		},
		{
			"dueDate",
			new List<string> { "date", "time" }
		},
		{
			"project",
			new List<string> { "project", "column" }
		}
	};

	private static readonly Dictionary<string, string> Style2NameMap = new Dictionary<string, string>
	{
		{ "date", "date" },
		{ "time", "date_plus_time" },
		{ "project", "List" },
		{ "column", "project_column" },
		{ "markdown", "Markdown" },
		{ "text", "Text" }
	};

	public string Key { get; set; }

	public long SortOrder { get; set; }

	public string Style { get; set; }

	public string Name { get; set; }

	public bool HasStyleOption { get; set; }

	public List<string> SupportedStyles { get; set; }

	public bool IsDraggingOver
	{
		get
		{
			return _isDraggingOver;
		}
		set
		{
			_isDraggingOver = value;
			OnPropertyChanged("IsDraggingOver");
		}
	}

	public bool IsDragging
	{
		get
		{
			return _isDragging;
		}
		set
		{
			_isDragging = value;
			OnPropertyChanged("IsDragging");
		}
	}

	public bool Draggable
	{
		get
		{
			if (Key != "status")
			{
				return Key != "detail";
			}
			return false;
		}
	}

	public bool ForceEnabled => Key == "title";

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			OnPropertyChanged("Enabled");
		}
	}

	public bool IsProItem => _proItems.Contains(Key);

	public SummaryDisplayItemModel ToModel()
	{
		return new SummaryDisplayItemModel(Key, SortOrder, Style, Enabled);
	}

	public SummaryDisplayItemViewModel(SummaryDisplayItemModel model)
	{
		Key = model.Key;
		SortOrder = model.SortOrder;
		Enabled = model.Enabled;
		Style = (model.Enabled ? model.Style : null);
		Name = GetNameByKey();
		SupportedStyles = (Key2StylesMap.TryGetValue(model.Key, out var value) ? value : new List<string>());
		HasStyleOption = SupportedStyles != null && SupportedStyles.Count > 0;
	}

	public SummaryDisplayItemViewModel(SummaryDisplayItem model)
	{
		Key = model.key;
		SortOrder = model.sortOrder;
		Enabled = model.enabled;
		Style = (model.enabled ? model.style : null);
		Name = GetNameByKey();
		SupportedStyles = (Key2StylesMap.TryGetValue(model.key, out var value) ? value : new List<string>());
		HasStyleOption = SupportedStyles != null && SupportedStyles.Count > 0;
	}

	public SummaryDisplayItem ToItemModel()
	{
		return new SummaryDisplayItem
		{
			key = Key,
			sortOrder = SortOrder,
			enabled = Enabled,
			style = GetStyle()
		};
	}

	private string GetStyle()
	{
		if (Key == "detail" && string.IsNullOrEmpty(Style))
		{
			return "text";
		}
		if (string.IsNullOrEmpty(Style))
		{
			return null;
		}
		return Style;
	}

	public SummaryDisplayItemViewModel()
	{
	}

	public static string GetStyleName(string style)
	{
		if (!Style2NameMap.TryGetValue(style, out var value))
		{
			return style;
		}
		return Utils.GetString(value);
	}

	public SummaryDisplayItemViewModel(string key, long sortOrder, string style, bool enabled)
	{
		Key = key;
		SortOrder = sortOrder;
		Style = style;
		Enabled = enabled;
		Name = GetNameByKey();
		HasStyleOption = !string.IsNullOrEmpty(style);
	}

	private string GetNameByKey()
	{
		if (!Key2NameMap.TryGetValue(Key, out var value))
		{
			return Key;
		}
		return Utils.GetString(value);
	}
}
