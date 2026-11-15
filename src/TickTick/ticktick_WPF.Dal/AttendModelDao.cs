using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class AttendModelDao : BaseDao<AttendModel>
{
	public static async Task<TaskAttendModel> GetTaskAttendModelById(string attendId)
	{
		if (!string.IsNullOrEmpty(attendId))
		{
			AttendModel attendModel = await GetAttendModelById(attendId);
			if (attendModel != null && !string.IsNullOrEmpty(attendModel.json))
			{
				return JsonConvert.DeserializeObject<TaskAttendModel>(attendModel.json);
			}
		}
		return null;
	}

	private static async Task<AttendModel> GetAttendModelById(string attendId)
	{
		return await (from model in App.Connection.Table<AttendModel>()
			where model.attendId == attendId
			select model).FirstOrDefaultAsync();
	}

	public static async Task<bool> SaveTaskAttendModel(string attendId, TaskAttendModel model)
	{
		if (model != null)
		{
			AttendModel attendModel = await GetAttendModelById(attendId);
			if (attendModel == null)
			{
				await App.Connection.InsertAsync(new AttendModel
				{
					attendId = attendId,
					json = JsonConvert.SerializeObject(model)
				});
				return true;
			}
			string text = JsonConvert.SerializeObject(model);
			if (attendModel.json != text)
			{
				attendModel.json = text;
				await App.Connection.UpdateAsync(attendModel);
				return true;
			}
		}
		return false;
	}
}
