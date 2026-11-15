using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emoji.Wpf;
using Mapster;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public class CountdownService
{
	static CountdownService()
	{
		TypeAdapterConfig<CountdownModel, CountdownModel>.NewConfig().Ignore((CountdownModel dest) => dest._Id, (CountdownModel dest) => dest.Id, (CountdownModel dest) => dest.UserId, (CountdownModel dest) => dest.LocalImagePath);
	}

	public static async Task<CountdownCollection> PullRemoteCountdowns()
	{
		try
		{
			return await Communicator.PullRemoteCountdowns();
		}
		catch (Exception ex)
		{
			UtilLog.Error("Failed to pull remote countdowns" + ex);
		}
		return null;
	}

	public static async Task<bool> MergeRemoteCountdowns(CountdownModel[] remoteCountdowns)
	{
		bool changed = false;
		if (remoteCountdowns == null)
		{
			return false;
		}
		foreach (CountdownModel countdown in remoteCountdowns)
		{
			CountdownModel countdownModel = await CountdownDao.GetCountdownByIdWithDeleted(countdown.Id);
			if (countdownModel == null)
			{
				UtilLog.Info("add countdown: " + countdown.Id);
				countdown.SyncStatus = 2;
				countdown.UserId = Utils.GetCurrentUserIdInt().ToString();
				await CountdownDao.InsertAsync(countdown);
				changed = true;
			}
			else
			{
				if ((!(countdown.Etag != countdownModel.Etag) && countdown.AnnoyingAlert == countdownModel.AnnoyingAlert) || countdownModel.Deleted == 1 || countdownModel.SyncStatus != 2)
				{
					continue;
				}
				try
				{
					countdown.Adapt(countdownModel);
				}
				catch (Exception ex)
				{
					UtilLog.Error("Adapt Countdown Error:" + ex.Message);
					countdownModel.Name = countdown.Name;
					countdownModel.IconRes = countdown.IconRes;
					countdownModel.Color = countdown.Color;
					countdownModel.Date = countdown.Date;
					countdownModel.TypeOfSmartList = countdown.TypeOfSmartList;
					countdownModel.IgnoreYear = countdown.IgnoreYear;
					countdownModel.Type = countdown.Type;
					countdownModel.ShowCalendarType = countdown.ShowCalendarType;
					countdownModel.Reminders = countdown.Reminders;
					countdownModel.RepeatFlag = countdown.RepeatFlag;
					countdownModel.Style = countdown.Style;
					countdownModel.SortOrder = countdown.SortOrder;
					countdownModel.Background = countdown.Background;
					countdownModel.StyleColor = countdown.StyleColor;
					countdownModel.DateDisplayFormat = countdown.DateDisplayFormat;
					countdownModel.Etag = countdown.Etag;
					countdownModel.Deleted = countdown.Deleted;
					countdownModel.ArchivedTime = countdown.ArchivedTime;
					countdownModel.pinnedTimeValue = countdown.pinnedTimeValue;
					countdownModel.CreatedTime = countdown.CreatedTime;
					countdownModel.ModifiedTime = countdown.ModifiedTime;
					countdownModel.Status = countdown.Status;
					countdownModel.Remark = countdown.Remark;
					countdownModel.TimerMode = countdown.TimerMode;
					countdownModel.ShowAge = countdown.ShowAge;
					countdownModel.AnnoyingAlert = countdown.AnnoyingAlert;
				}
				countdownModel.SyncStatus = 2;
				if (countdown.BackgroundObject != null && countdown.BackgroundObject.Id != null)
				{
					try
					{
						if (!string.IsNullOrEmpty(countdownModel.LocalImagePath) && File.Exists(countdownModel.LocalImagePath))
						{
							File.Delete(countdownModel.LocalImagePath);
						}
						countdownModel.LocalImagePath = "";
					}
					catch (Exception ex2)
					{
						UtilLog.Error("Failed to delete local image: " + ex2);
					}
				}
				UtilLog.Info("update countdown: " + countdown.Id);
				await CountdownDao.UpdateAsync(countdownModel);
				changed = true;
			}
		}
		List<CountdownModel> list = await CountdownDao.GetAllCountdowns();
		List<string> remoteIds = remoteCountdowns.Select((CountdownModel c) => c.Id).ToList();
		foreach (CountdownModel item in list)
		{
			if (!remoteIds.Contains(item.Id) && item.SyncStatus != 0 && item.SyncStatus != 4)
			{
				UtilLog.Info("delete countdown: " + item.Id);
				item.Deleted = 1;
				item.SyncStatus = 2;
				await CountdownDao.UpdateAsync(item);
				changed = true;
			}
		}
		return changed;
	}

	public static BitmapSource GetIcon(string iconRes, string color)
	{
		if (iconRes == null)
		{
			return null;
		}
		if (iconRes.StartsWith("countdown"))
		{
			return new BitmapImage(new Uri("pack://application:,,,/Assets/CountDown/Icons/" + iconRes.ToLower() + ".png"));
		}
		Border border = new Border();
		border.Width = 36.0;
		border.Height = 36.0;
		border.Background = ThemeUtil.GetColor("WindowBackground");
		border.CornerRadius = new CornerRadius(18.0);
		border.Child = new Border
		{
			Width = 36.0,
			Height = 36.0,
			Background = (string.IsNullOrEmpty(color) ? ThemeUtil.GetPrimaryColor(0.15) : ThemeUtil.GetColorInString(color)),
			CornerRadius = new CornerRadius(18.0),
			Child = new EmjTextBlock
			{
				FontSize = 18.0,
				Text = iconRes.Split('_').LastOrDefault(),
				Foreground = (string.IsNullOrEmpty(color) ? ThemeUtil.GetColor("PrimaryColor") : Brushes.White),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			}
		};
		return ThemeUtil.SaveAsWriteableBitmap(border);
	}
}
