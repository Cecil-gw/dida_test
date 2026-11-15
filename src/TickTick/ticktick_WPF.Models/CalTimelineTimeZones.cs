using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Notifier;

namespace ticktick_WPF.Models;

public class CalTimelineTimeZones : PreferenceBaseModel
{
	public bool enabled { get; set; }

	public List<CalTimeZone> current { get; set; }

	public List<CalTimeZone> additional { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is CalTimelineTimeZones calTimelineTimeZones)
		{
			if (calTimelineTimeZones.mtime > base.mtime)
			{
				current = calTimelineTimeZones.current;
				additional = calTimelineTimeZones.additional;
				enabled = calTimelineTimeZones.enabled;
				base.mtime = calTimelineTimeZones.mtime;
				GlobalEventManager.NotifyTimeLineTimeZonesChangedChanged(countChanged: true);
			}
			else if (calTimelineTimeZones.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsEmpty()
	{
		List<CalTimeZone> list = current;
		if (list != null && list.Count == 0)
		{
			List<CalTimeZone> list2 = additional;
			if (list2 == null)
			{
				return false;
			}
			return list2.Count == 0;
		}
		return false;
	}

	public void Save(CalTimelineTimeZones calTimeZones)
	{
		additional = calTimeZones.additional;
		if (current == null || current.Count == 0)
		{
			current = calTimeZones.current;
		}
		else
		{
			if (calTimeZones.current == null)
			{
				return;
			}
			foreach (CalTimeZone tz in calTimeZones.current)
			{
				CalTimeZone calTimeZone = current.FirstOrDefault((CalTimeZone t) => t.timeZone == tz.timeZone);
				if (calTimeZone != null)
				{
					calTimeZone.label = tz.label;
				}
				else
				{
					current.Add(tz);
				}
			}
		}
	}
}
