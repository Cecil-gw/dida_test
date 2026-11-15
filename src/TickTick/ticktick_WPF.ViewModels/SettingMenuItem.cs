using System.Collections.Generic;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class SettingMenuItem : BaseViewModel
{
	private bool _expanded;

	private bool _visible = true;

	private bool _isSelected;

	private bool _isSubItem;

	private bool _hasChildren;

	private bool _showNew;

	public string Title { get; set; }

	public Geometry Icon { get; set; }

	public SettingsType Type { get; set; }

	public bool IsSplit { get; set; }

	public bool IsSubItem
	{
		get
		{
			return _isSubItem;
		}
		set
		{
			_isSubItem = value;
			OnPropertyChanged("IsSubItem");
		}
	}

	public SettingsType ParentType { get; set; }

	public bool HasChildren
	{
		get
		{
			return _hasChildren;
		}
		set
		{
			_hasChildren = value;
			OnPropertyChanged("HasChildren");
		}
	}

	public List<SettingMenuItem> Children { get; set; }

	public bool Expanded
	{
		get
		{
			return _expanded;
		}
		set
		{
			_expanded = value;
			OnPropertyChanged("Expanded");
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				if (value && _showNew)
				{
					SettingsMenuHelper.CheckNewFeature(Type);
				}
				OnPropertyChanged("IsSelected");
			}
		}
	}

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			OnPropertyChanged("Visible");
		}
	}

	public bool ShowExpired { get; set; }

	public bool ShowNew
	{
		get
		{
			return _showNew;
		}
		set
		{
			_showNew = value;
			OnPropertyChanged("ShowNew");
		}
	}

	public SettingMenuItem()
	{
		IsSplit = true;
	}

	public SettingMenuItem(string title, SettingsType type, SettingsType parent)
	{
		Title = Utils.GetString(title);
		Type = type;
		IsSubItem = true;
		ParentType = parent;
		Visible = false;
	}

	public SettingMenuItem(string title, SettingsType type)
	{
		Title = Utils.GetString(title);
		Type = type;
		string iconKey = GetIconKey(type);
		if (!string.IsNullOrEmpty(iconKey))
		{
			Icon = Utils.GetGeometry(iconKey);
		}
		else
		{
			Icon = Utils.GetIcon("SettingsIc" + type);
		}
	}

	private string GetIconKey(SettingsType type)
	{
		return type switch
		{
			SettingsType.Account => "Ic.Setting.AccountSetting", 
			SettingsType.Premium => "Ic.Setting.Premium", 
			SettingsType.Feature => "Ic.Setting.FuncModule", 
			SettingsType.SmartList => "Ic.Setting.Filter", 
			SettingsType.Notification => "Ic.Setting.Notification", 
			SettingsType.DateTime => "Ic.Setting.DateTime", 
			SettingsType.Theme => "Ic.Setting.Appearance", 
			SettingsType.AIFeatures => "Ic.Setting.AIFunction", 
			SettingsType.More => "Ic.Setting.MoreSetting", 
			SettingsType.Calendar => "Ic.Setting.Import", 
			SettingsType.Share => "Ic.Setting.Sharing", 
			SettingsType.Shortcuts => "Ic.Setting.HotKey", 
			SettingsType.About => "Ic.Setting.About", 
			SettingsType.Debug => "Ic.Setting.About", 
			_ => null, 
		};
	}
}
