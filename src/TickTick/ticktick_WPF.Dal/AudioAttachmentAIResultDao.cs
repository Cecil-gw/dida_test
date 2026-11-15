using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

internal class AudioAttachmentAIResultDao : BaseDao<AudioAttachmentAIResultModel>
{
	public static async Task<AudioAttachmentAIResultModel> GetByAttachmentId(string id)
	{
		return await (from t in App.Connection.Table<AudioAttachmentAIResultModel>()
			where t.AttachmentId == id
			select t).FirstOrDefaultAsync();
	}

	public static async Task DeleteByAttachmentId(string id)
	{
		AudioAttachmentAIResultModel audioAttachmentAIResultModel = await GetByAttachmentId(id);
		if (audioAttachmentAIResultModel != null)
		{
			await App.Connection.DeleteAsync(audioAttachmentAIResultModel);
		}
	}

	public static async Task<bool> ExistRecord()
	{
		string userId = Utils.GetCurrentUserStr();
		return await (from x in App.Connection.Table<AudioAttachmentAIResultModel>()
			where x.UserId == userId
			select x).CountAsync() > 0;
	}
}
