using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class TagSortTypeDao
{
	public static async Task<TagSortTypeModel> GetTagSortTypeDbByuserIdAndTagAsync(string userid, string tag)
	{
		return await Task.Run(async delegate
		{
			List<TagSortTypeModel> list = await (from v in App.Connection.Table<TagSortTypeModel>()
				where v.userid == userid && v.tag == tag
				select v).ToListAsync();
			return (list.Count != 0) ? list[0] : null;
		});
	}

	public static async Task UpdateOrInsertTagSortTypeAsync(TagSortTypeModel tagSortTypeModel)
	{
		List<TagSortTypeModel> list = await (from v in App.Connection.Table<TagSortTypeModel>()
			where v.userid == tagSortTypeModel.userid && v.tag == tagSortTypeModel.tag
			select v).ToListAsync();
		if (list.Count != 0)
		{
			tagSortTypeModel._Id = list[0]._Id;
			await App.Connection.UpdateAsync(tagSortTypeModel);
		}
		else
		{
			await App.Connection.InsertAsync(tagSortTypeModel);
		}
	}
}
