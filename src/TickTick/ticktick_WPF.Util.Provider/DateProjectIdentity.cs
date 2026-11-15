using System;
using System.Globalization;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class DateProjectIdentity : ProjectIdentity
{
	public string DateStamp { get; }

	public override string Id => "_special_id_date";

	public override string CatId => Id + DateStamp;

	public DateTime TargetDate { get; }

	public DateProjectIdentity(string dateStamp)
	{
		base.CanDrag = true;
		DateStamp = dateStamp;
		try
		{
			TargetDate = DateTime.ParseExact(DateStamp, "yyyyMMdd", CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			UtilLog.Info("DateProjectIdentity.Error" + dateStamp);
			TargetDate = DateTime.Today;
		}
		base.SortOption = new SortOption
		{
			groupBy = "none",
			orderBy = "dueDate"
		};
	}

	public override TimeData GetTimeData()
	{
		TimeData timeData = TimeData.InitDefaultTime();
		timeData.StartDate = TargetDate;
		return timeData;
	}

	public override string GetDisplayTitle()
	{
		return (TargetDate - DateTime.Today).Days switch
		{
			0 => Utils.GetString("Today"), 
			1 => Utils.GetString("Tomorrow"), 
			_ => TargetDate.ToString("yyyy'-'MM'-'dd", App.Ci), 
		};
	}
}
