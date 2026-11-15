using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

public class HolidayDao : BaseDao<HolidayModel>
{
	public static async Task SaveHolidays(List<HolidayModel> holidays)
	{
		await App.Connection.InsertAllAsync(holidays);
	}

	public static async Task<List<HolidayModel>> GetRecentHolidays()
	{
		return await App.Connection.Table<HolidayModel>().ToListAsync();
	}

	public static async Task ClearHolidays()
	{
		await App.Connection.ExecuteAsync("DELETE FROM HolidayModel WHERE 1=1");
	}
}
