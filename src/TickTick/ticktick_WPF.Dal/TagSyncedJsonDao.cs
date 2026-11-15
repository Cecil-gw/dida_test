using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class TagSyncedJsonDao
{
	public static async Task TrySaveTag(TagModel tag, string originName = null)
	{
		if (tag == null)
		{
			return;
		}
		string name = (string.IsNullOrEmpty(originName) ? tag.name : originName);
		if (await GetSavedJson(name) != null)
		{
			return;
		}
		TagModel tagModel = await TagDao.GetTagByName(name);
		if (tagModel != null)
		{
			if (!string.IsNullOrEmpty(originName))
			{
				tagModel.name = tag.name;
				tagModel.label = tag.label;
			}
			TagSyncedJsonModel item = new TagSyncedJsonModel
			{
				TagName = tagModel.name,
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				JsonString = JsonConvert.SerializeObject(tagModel)
			};
			await App.Connection.InsertAsync(item);
		}
	}

	public static async Task<TagSyncedJsonModel> GetSavedJson(string tagName)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<TagSyncedJsonModel>()
			where model.UserId == userId && model.TagName == tagName
			select model).FirstOrDefaultAsync();
	}

	public static async Task BatchDeleteTags(List<string> tags)
	{
		if (tags == null || !tags.Any())
		{
			return;
		}
		foreach (string tag in tags)
		{
			TagSyncedJsonModel tagSyncedJsonModel = await GetSavedJson(tag);
			if (tagSyncedJsonModel != null)
			{
				await App.Connection.DeleteAsync(tagSyncedJsonModel);
			}
		}
	}
}
