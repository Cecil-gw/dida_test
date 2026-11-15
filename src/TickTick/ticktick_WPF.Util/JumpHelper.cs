using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.AI;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Util;

public static class JumpHelper
{
	public static void ClearJumpList()
	{
		JumpList.SetJumpList(Application.Current, new JumpList(new List<JumpItem>(), showFrequent: false, showRecent: false));
		RemoveChangeEvents();
	}

	public static async void InitJumpList()
	{
		await Task.Delay(400);
		ThreadUtil.DetachedRunOnUiBackThread(async delegate
		{
			await SetJumpList();
		});
	}

	private static async Task SetJumpList()
	{
		string path = AppPaths.ExeDir + "\\JumpIcons.dll";
		JumpList jumpList = new JumpList();
		JumpTask addProjectWidget = new JumpTask
		{
			Title = Utils.GetString("AddListWidget"),
			Arguments = "other_add_list_widget",
			CustomCategory = Utils.GetString("Others"),
			IconResourcePath = path,
			IconResourceIndex = 3
		};
		JumpTask addCalendarWidget = new JumpTask
		{
			Title = Utils.GetString("AddCalendarWidget"),
			Arguments = "other_add_calendar_widget",
			CustomCategory = Utils.GetString("Others"),
			IconResourcePath = path,
			IconResourceIndex = 1
		};
		JumpTask removeCalendarWidget = new JumpTask
		{
			Title = Utils.GetString("CloseCalendarWidget"),
			Arguments = "other_remove_calendar_widget",
			CustomCategory = Utils.GetString("Others"),
			IconResourcePath = path,
			IconResourceIndex = 1
		};
		bool num = await AppLockCache.GetAppLocked();
		if (!num)
		{
			jumpList.JumpItems.Add(addProjectWidget);
			jumpList.JumpItems.Add(CalendarWidgetHelper.CanAddProject() ? addCalendarWidget : removeCalendarWidget);
			bool flag = MatrixWidgetHelper.CanAddWidget();
			jumpList.JumpItems.Add(new JumpTask
			{
				Title = Utils.GetString(flag ? "AddMatrixWidget" : "CloseMatrixWidget"),
				Arguments = (flag ? "other_add_matrix_widget" : "other_remove_matrix_widget"),
				CustomCategory = Utils.GetString("Others"),
				IconResourcePath = path,
				IconResourceIndex = 4
			});
			if (LocalSettings.Settings.EnableAI)
			{
				bool flag2 = AIWindow.DesktopWidget == null;
				jumpList.JumpItems.Add(new JumpTask
				{
					Title = Utils.GetString(flag2 ? "AddAIWidget" : "CloseAIWidget"),
					Arguments = (flag2 ? "other_add_ai_widget" : "other_remove_ai_widget"),
					CustomCategory = Utils.GetString("Others"),
					IconResourcePath = path,
					IconResourceIndex = 11
				});
			}
		}
		if (LocalSettings.Settings.EnableFocus)
		{
			if (TickFocusManager.Working)
			{
				bool flag3 = LocalSettings.Settings.PomoType == FocusConstance.Focus;
				jumpList.JumpItems.Add(new JumpTask
				{
					Title = Utils.GetString(flag3 ? "ShowPomo" : "ShowTiming"),
					Arguments = (flag3 ? "other_show_pomo" : "other_show_timing"),
					CustomCategory = Utils.GetString("Others"),
					IconResourcePath = path,
					IconResourceIndex = (flag3 ? 5 : 6)
				});
			}
			else
			{
				jumpList.JumpItems.Add(new JumpTask
				{
					Title = Utils.GetString("StartPomo"),
					Arguments = "other_start_pomo",
					CustomCategory = Utils.GetString("Others"),
					IconResourcePath = path,
					IconResourceIndex = 5
				});
				jumpList.JumpItems.Add(new JumpTask
				{
					Title = Utils.GetString("StartTiming"),
					Arguments = "other_start_timing",
					CustomCategory = Utils.GetString("Others"),
					IconResourcePath = path,
					IconResourceIndex = 6
				});
			}
		}
		if (!num)
		{
			jumpList.JumpItems.Add(new JumpTask
			{
				Title = Utils.GetString("MoreWidgets"),
				Arguments = "other_more_widgets",
				CustomCategory = Utils.GetString("Others"),
				IconResourcePath = path,
				IconResourceIndex = 10
			});
		}
		JumpTask item = new JumpTask
		{
			Title = Utils.GetString("AddaTask"),
			Arguments = "add_task",
			CustomCategory = Utils.GetString("Task"),
			IconResourcePath = path,
			IconResourceIndex = 0
		};
		JumpTask item2 = new JumpTask
		{
			Title = Utils.GetString("Calendar"),
			Arguments = "task_show_calendar",
			CustomCategory = Utils.GetString("Task"),
			IconResourcePath = path,
			IconResourceIndex = 1
		};
		JumpTask item3 = new JumpTask
		{
			Title = Utils.GetString("Today"),
			Arguments = "task_show_today",
			CustomCategory = Utils.GetString("Task"),
			IconResourcePath = path,
			IconResourceIndex = 7
		};
		JumpTask item4 = new JumpTask
		{
			Title = Utils.GetString("Inbox"),
			Arguments = "task_show_inbox",
			CustomCategory = Utils.GetString("Task"),
			IconResourcePath = path,
			IconResourceIndex = 2
		};
		jumpList.JumpItems.Add(item);
		jumpList.JumpItems.Add(item3);
		jumpList.JumpItems.Add(item2);
		jumpList.JumpItems.Add(item4);
		List<JumpTask> list = InitCustomList();
		if (list.Any())
		{
			jumpList.JumpItems.AddRange(list);
		}
		jumpList.ShowFrequentCategory = false;
		jumpList.ShowRecentCategory = false;
		try
		{
			JumpList.SetJumpList(Application.Current, jumpList);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	private static List<JumpTask> InitCustomList()
	{
		List<JumpTask> list = new List<JumpTask>();
		string recentProjects = LocalSettings.Settings.RecentProjects;
		string iconResourcePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\JumpIcons.dll";
		if (!string.IsNullOrEmpty(recentProjects))
		{
			List<string> list2 = recentProjects.Split(',').ToList();
			if (!list2.Any())
			{
				return list;
			}
			foreach (string item in list2)
			{
				if (item.Contains(":"))
				{
					string[] array = item.Split(':');
					if (array.Length != 2)
					{
						continue;
					}
					string text = array[0];
					string value = array[1];
					switch (text)
					{
					case "project":
					{
						ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == value);
						if (projectModel != null)
						{
							list.Add(new JumpTask
							{
								Title = projectModel.name,
								Arguments = item,
								CustomCategory = Utils.GetString("Recent"),
								IconResourcePath = iconResourcePath,
								IconResourceIndex = 3
							});
						}
						break;
					}
					case "tag":
					{
						TagModel tagModel = CacheManager.GetTags().FirstOrDefault((TagModel p) => p.name == value);
						if (tagModel != null)
						{
							list.Add(new JumpTask
							{
								Title = tagModel.GetDisplayName(),
								Arguments = item,
								CustomCategory = Utils.GetString("Recent"),
								IconResourcePath = iconResourcePath,
								IconResourceIndex = 9
							});
						}
						break;
					}
					case "filter":
					{
						FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel p) => p.id == value);
						if (filterModel != null)
						{
							list.Add(new JumpTask
							{
								Title = filterModel.name,
								Arguments = item,
								CustomCategory = Utils.GetString("Recent"),
								IconResourcePath = iconResourcePath,
								IconResourceIndex = 8
							});
						}
						break;
					}
					}
				}
				if (list.Count == 2)
				{
					break;
				}
			}
		}
		return list;
	}

	private static void RemoveChangeEvents()
	{
		DataChangedNotifier.ProjectChanged -= OnProjectChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
		DataChangedNotifier.FilterChanged -= OnFilterChanged;
	}

	private static void OnFilterChanged(object sender, FilterChangeArgs e)
	{
		InitJumpList();
	}

	public static void InitChangeEvents()
	{
		DataChangedNotifier.ProjectChanged += OnProjectChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
		DataChangedNotifier.FilterChanged += OnFilterChanged;
	}

	private static void OnProjectChanged(object sender, EventArgs e)
	{
		InitJumpList();
	}
}
