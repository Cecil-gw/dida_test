using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils;

namespace TickTickDao;

public class CalendarConnectInfoDao : BaseDao<CalendarConnectInfo>
{
	public static async Task<List<CalendarConnectInfo>> GetAllConnectsAsync()
	{
		return await (from c in Connection.DbConnection.Table<CalendarConnectInfo>()
			where c.userId == GlobalSettings.UserId
			select c).ToListAsync();
	}

	public static async Task DeleteConnectsAsync(List<CalendarConnectInfo> connects)
	{
		foreach (CalendarConnectInfo connect in connects)
		{
			await Connection.DbConnection.DeleteAsync(connect);
		}
	}

	public static async Task<CalendarConnectInfo> GetInfoByIdAsync(string id)
	{
		return await (from c in Connection.DbConnection.Table<CalendarConnectInfo>()
			where c.userId == GlobalSettings.UserId && c.id == id
			select c).FirstOrDefaultAsync();
	}
}
