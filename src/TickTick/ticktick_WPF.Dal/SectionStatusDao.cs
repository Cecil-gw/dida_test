using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public static class SectionStatusDao
{
	private static async Task<bool> IsProjectSectionClosed(string identity, string name)
	{
		return await (from v in App.Connection.Table<SectionStatusModel>()
			where v.Identity == identity && v.Name == name
			select v).CountAsync() > 0;
	}

	public static async Task OpenProjectSection(string identity, string name)
	{
		List<SectionStatusModel> list = await (from v in App.Connection.Table<SectionStatusModel>()
			where v.Identity == identity && v.Name == name
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (SectionStatusModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
		CacheManager.DeleteSectionStatus(new SectionStatusModel
		{
			Identity = identity,
			Name = name
		});
	}

	public static async Task CloseProjectSection(string identity, string name)
	{
		if (!(await IsProjectSectionClosed(identity, name)))
		{
			SectionStatusModel item = new SectionStatusModel
			{
				Identity = identity,
				Name = name
			};
			await App.Connection.InsertAsync(item);
			CacheManager.AddSectionStatus(new SectionStatusModel
			{
				Identity = identity,
				Name = name
			});
		}
	}

	public static async Task<List<SectionStatusModel>> GetSectionStatus()
	{
		return await App.Connection.Table<SectionStatusModel>().ToListAsync();
	}

	public static async Task<List<SectionStatusModel>> GetSectionStatus(List<string> identities)
	{
		return await (from v in App.Connection.Table<SectionStatusModel>()
			where identities.Contains(v.Identity)
			select v).ToListAsync();
	}

	public static async Task<List<SectionStatusModel>> GetSectionStatus(string projectId)
	{
		return await (from v in App.Connection.Table<SectionStatusModel>()
			where v.Identity == projectId
			select v).ToListAsync();
	}
}
