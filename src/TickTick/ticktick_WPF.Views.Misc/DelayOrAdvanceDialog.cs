using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Misc;

public class DelayOrAdvanceDialog
{
	private bool _placeTop;

	public event EventHandler<long> DelayOrAdvanced;

	public void ShowDialog(Popup popup, bool placeTop = false, bool canPostPoneTime = true)
	{
		_placeTop = placeTop;
		List<CustomMenuItemViewModel> list = ((!canPostPoneTime) ? new List<CustomMenuItemViewModel>() : new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"15m", string.Format(Utils.GetString("PostponeBy"), "15 " + Utils.GetString("PublicMinutes")), (Geometry)null, "")
			{
				ShowSelected = false,
				TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
			},
			new CustomMenuItemViewModel((object)"30m", string.Format(Utils.GetString("PostponeBy"), "30 " + Utils.GetString("PublicMinutes")), (Geometry)null, "")
			{
				ShowSelected = false,
				TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
			},
			new CustomMenuItemViewModel((object)"1h", string.Format(Utils.GetString("PostponeBy"), "1 " + Utils.GetString("UpHour").ToLower()), (Geometry)null, "")
			{
				ShowSelected = false,
				TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
			},
			new CustomMenuItemViewModel((object)"2h", string.Format(Utils.GetString("PostponeBy"), "2 " + Utils.GetString("Hours").ToLower()), (Geometry)null, "")
			{
				ShowSelected = false,
				TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
			}
		});
		list.Add(new CustomMenuItemViewModel((object)"1d", string.Format(Utils.GetString("PostponeBy"), "1 " + Utils.GetString("PublicDay").ToLower()), (Geometry)null, "")
		{
			ShowSelected = false,
			TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
		});
		list.Add(new CustomMenuItemViewModel((object)"1w", string.Format(Utils.GetString("PostponeBy"), "1 " + Utils.GetString("PublicWeek").ToLower()), (Geometry)null, "")
		{
			ShowSelected = false,
			TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
		});
		list.Add(new CustomMenuItemViewModel((object)"custom", Utils.GetString("Custom"), (Geometry)null, "")
		{
			ShowSelected = false,
			TextMargin = new Thickness(12.0, 0.0, 18.0, 0.0)
		});
		CustomMenuList menuList = new CustomMenuList(list, popup)
		{
			AutoClose = false
		};
		menuList.Operated += delegate(object sender, object o)
		{
			switch (o.ToString())
			{
			case "15m":
				this.DelayOrAdvanced?.Invoke(null, 15L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "15m");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "30m":
				this.DelayOrAdvanced?.Invoke(null, 30L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "30m");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "1h":
				this.DelayOrAdvanced?.Invoke(null, 60L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "1h");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "2h":
				this.DelayOrAdvanced?.Invoke(null, 120L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "2h");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "1d":
				this.DelayOrAdvanced?.Invoke(null, 1440L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "1d");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "1w":
				this.DelayOrAdvanced?.Invoke(null, 10080L);
				UserActCollectUtils.AddClickEvent("tasklist", "postpone", "1w");
				UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
				break;
			case "custom":
				if (_placeTop)
				{
					popup.IsOpen = false;
				}
				new AdvanceOrDelayPopup(popup, canPostPoneTime).DelayOrAdvanced += delegate(object obj, long l)
				{
					UserActCollectUtils.AddClickEvent("tasklist", "cm_batch_task", "postpone");
					this.DelayOrAdvanced?.Invoke(sender, l);
					if (l != 0L)
					{
						JObject jObject = new JObject();
						if (l > 0)
						{
							jObject["label"] = "postpone_custom";
							jObject["postpone"] = GetPostponeTimeText(l);
						}
						else
						{
							jObject["label"] = "advance_custom";
							jObject["advance"] = GetPostponeTimeText(Math.Abs(l));
						}
						UserActCollectUtils.AddClickEventWithData("tasklist", "postpone", jObject);
					}
					menuList.Close();
				};
				if (_placeTop)
				{
					popup.IsOpen = true;
				}
				return;
			}
			menuList.Close();
		};
		menuList.Show();
	}

	private string GetPostponeTimeText(long l)
	{
		if (l >= 10080)
		{
			return l / 10080 + "w";
		}
		if (l >= 1440)
		{
			return l / 1440 + "d";
		}
		if (l >= 60)
		{
			return l / 60 + "h";
		}
		return l + "m";
	}
}
