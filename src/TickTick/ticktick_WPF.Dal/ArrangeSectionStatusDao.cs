using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public static class ArrangeSectionStatusDao
{
	private static List<string> _listNames = new List<string>();

	private static List<string> _tagNames = new List<string>();

	private static List<string> _priorityNames = new List<string>();

	public static async Task InitClosedSectionNames()
	{
		List<ArrangeSectionStatusModel> list = await (from m in App.Connection.Table<ArrangeSectionStatusModel>()
			where m.UserId == LocalSettings.Settings.LoginUserId
			select m).ToListAsync();
		_listNames.AddRange((from m in list?.Where((ArrangeSectionStatusModel m) => m.Type == 0).ToList()
			select m.Name).ToList() ?? new List<string>());
		_tagNames.AddRange((from m in list?.Where((ArrangeSectionStatusModel m) => m.Type == 1).ToList()
			select m.Name).ToList() ?? new List<string>());
		_priorityNames.AddRange((from m in list?.Where((ArrangeSectionStatusModel m) => m.Type == 2).ToList()
			select m.Name).ToList() ?? new List<string>());
	}

	public static async Task AddSectionStatusModel(ArrangeSectionStatusModel model)
	{
		bool flag = false;
		switch (model.Type)
		{
		case 0:
			if (!_listNames.Contains(model.Name))
			{
				_listNames.Add(model.Name);
				flag = true;
			}
			break;
		case 1:
			if (!_tagNames.Contains(model.Name))
			{
				_tagNames.Add(model.Name);
				flag = true;
			}
			break;
		case 2:
			if (!_priorityNames.Contains(model.Name))
			{
				_priorityNames.Add(model.Name);
				flag = true;
			}
			break;
		}
		if (flag)
		{
			await App.Connection.InsertAsync(model);
		}
	}

	public static async Task DeleteSectionStatusModel(ArrangeSectionStatusModel model)
	{
		switch (model.Type)
		{
		case 0:
			if (_listNames.Contains(model.Name))
			{
				_listNames.Remove(model.Name);
			}
			break;
		case 1:
			if (_tagNames.Contains(model.Name))
			{
				_tagNames.Remove(model.Name);
			}
			break;
		case 2:
			if (_priorityNames.Contains(model.Name))
			{
				_priorityNames.Remove(model.Name);
			}
			break;
		}
		List<ArrangeSectionStatusModel> list = await (from m in App.Connection.Table<ArrangeSectionStatusModel>()
			where m.Name == model.Name && m.UserId == LocalSettings.Settings.LoginUserId && m.Type == model.Type
			select m).ToListAsync();
		if (list == null)
		{
			return;
		}
		foreach (ArrangeSectionStatusModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static bool CheckSectionClosed(int type, string name)
	{
		return type switch
		{
			0 => _listNames.Contains(name), 
			1 => _tagNames.Contains(name), 
			2 => _priorityNames.Contains(name), 
			_ => false, 
		};
	}

	public static bool CheckSectionClosed(int type, string name, string legacyName)
	{
		if (!CheckSectionClosed(type, name))
		{
			if (!string.IsNullOrEmpty(legacyName) && legacyName != name)
			{
				return CheckSectionClosed(type, legacyName);
			}
			return false;
		}
		return true;
	}
}
