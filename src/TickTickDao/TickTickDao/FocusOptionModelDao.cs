using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickModels;

namespace TickTickDao;

public class FocusOptionModelDao : BaseDao<FocusOptionModel>
{
	public static async Task DeleteAll(string ignoreId)
	{
		List<FocusOptionModel> list = await BaseDao<FocusOptionModel>.GetAllAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		list.ForEach(async delegate(FocusOptionModel model)
		{
			if (ignoreId != model.id)
			{
				await BaseDao<FocusOptionModel>.DeleteAsync(model);
			}
		});
	}
}
