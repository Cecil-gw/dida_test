using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public static class HabitSectionCache
{
	private static List<HabitSectionModel> _sections = new List<HabitSectionModel>();

	public static async Task SetSections()
	{
		_sections = await HabitSectionDao.GetAllHabitSections();
	}

	public static List<HabitSectionModel> GetSections()
	{
		return _sections.Where((HabitSectionModel s) => s.SyncStatus != -1).ToList();
	}

	public static HabitSectionModel GetSectionById(string id)
	{
		return _sections.FirstOrDefault((HabitSectionModel s) => s.Id == id);
	}

	public static Dictionary<string, HabitSectionModel> GetSectionDict()
	{
		Dictionary<string, HabitSectionModel> dictionary = new Dictionary<string, HabitSectionModel>();
		foreach (HabitSectionModel section in _sections)
		{
			if (!string.IsNullOrEmpty(section.Id) && section.SyncStatus != -1)
			{
				dictionary[section.Id] = section;
			}
		}
		return dictionary;
	}
}
