using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickSelectionControl : UserControl, IComponentConnector
{
	private IQuickInput _quickInputItems;

	private readonly Popup _popup;

	private string _avatarProject;

	private DateTime _lastPullTime;

	private List<AvatarViewModel> _avatars;

	internal Grid ItemsContainer;

	private bool _contentLoaded;

	public event EventHandler<QuickSetModel> ItemSelected;

	public event EventHandler<bool> PopupOpenChanged;

	public QuickSelectionControl(Popup popup)
	{
		_popup = popup ?? new EscPopup
		{
			StaysOpen = false,
			PopupAnimation = PopupAnimation.Fade,
			Placement = PlacementMode.Absolute
		};
		InitializeComponent();
		_popup.Opened += delegate
		{
			this.PopupOpenChanged?.Invoke(this, e: true);
		};
		_popup.Closed += delegate
		{
			this.PopupOpenChanged?.Invoke(this, e: false);
		};
		_popup.Child = this;
	}

	public bool PopupOpened()
	{
		return _popup?.IsOpen ?? false;
	}

	public bool SetSelectionItems(string mark, string content, bool usedInCal, string calId, string projectId, int priority, List<string> tags, string assignee, bool withPriority = true, bool withAttachment = true, bool withTemplate = true, bool isNote = false, bool isCalendar = false, bool isNoteProject = false, bool canConvertToEvent = false)
	{
		if (mark != null)
		{
			switch (mark.Length)
			{
			case 1:
				switch (mark[0])
				{
				case '!':
				case '！':
					if (!withPriority || usedInCal || isNote)
					{
						_quickInputItems = null;
					}
					else if (!(_quickInputItems is PriorityInputItems))
					{
						_quickInputItems = new PriorityInputItems(priority);
						((PriorityInputItems)_quickInputItems).OnItemSelected -= OnPrioritySelect;
						((PriorityInputItems)_quickInputItems).OnItemSelected += OnPrioritySelect;
					}
					else
					{
						((PriorityInputItems)_quickInputItems).SetSelected(priority);
					}
					break;
				case '^':
				case '~':
					if (!ShouldUseCalendarItems(mark, usedInCal, isCalendar))
					{
						if (!(_quickInputItems is ProjectInputItems))
						{
							_quickInputItems = new ProjectInputItems(projectId);
							((ProjectInputItems)_quickInputItems).OnItemSelected -= OnProjectSelect;
							((ProjectInputItems)_quickInputItems).OnItemSelected += OnProjectSelect;
						}
						else
						{
							((ProjectInputItems)_quickInputItems).SetSelected(projectId);
						}
					}
					else if (!(_quickInputItems is CalendarInputItems calendarInputItems) || !(calendarInputItems.AccountId == calId))
					{
						_quickInputItems = new CalendarInputItems(calId);
						((CalendarInputItems)_quickInputItems).OnItemSelected -= OnCalendarSelect;
						((CalendarInputItems)_quickInputItems).OnItemSelected += OnCalendarSelect;
					}
					break;
				case '*':
					if (!(_quickInputItems is DateInputItems))
					{
						_quickInputItems = new DateInputItems();
						((DateInputItems)_quickInputItems).OnItemSelected -= OnQuickDateSelected;
						((DateInputItems)_quickInputItems).OnItemSelected += OnQuickDateSelected;
					}
					break;
				case '@':
					if (usedInCal)
					{
						break;
					}
					GetAvatars(projectId);
					if (_avatars == null)
					{
						_quickInputItems = null;
					}
					else if (_quickInputItems is AssignInputItems assignInputItems && (assignInputItems.Models == _avatars || IsOpen()))
					{
						assignInputItems.SetSelected(assignee);
						if (assignInputItems.Models != _avatars)
						{
							assignInputItems.SetModels(_avatars);
						}
					}
					else
					{
						_quickInputItems = new AssignInputItems(_avatars, selectFirst: true, assignee);
						((AssignInputItems)_quickInputItems).OnItemSelected -= AssignSelect;
						((AssignInputItems)_quickInputItems).OnItemSelected += AssignSelect;
					}
					break;
				case '#':
				case '＃':
					if (!usedInCal && !(_quickInputItems is TagSelectionControl))
					{
						_quickInputItems = new TagSelectionControl(tags);
						((TagSelectionControl)_quickInputItems).TagSelected -= TagSelect;
						((TagSelectionControl)_quickInputItems).TagSelected += TagSelect;
					}
					break;
				case '/':
				case '、':
					if (!(_quickInputItems is SplashInputItems splashInputItems) || splashInputItems.WithPriority != withPriority || splashInputItems.WithAttachment != withAttachment || splashInputItems.WithTemplate != withTemplate || splashInputItems.IsNote != isNote || splashInputItems.IsCalendar != isCalendar || splashInputItems.IsNoteProject != isNoteProject || splashInputItems.CanConvertToEvent != canConvertToEvent)
					{
						UserActCollectUtils.AddClickEvent("slash_om", "show", "inputbox");
						_quickInputItems = new SplashInputItems(withPriority, withAttachment, withTemplate, isNote, isCalendar, isNoteProject, canConvertToEvent);
						((SplashInputItems)_quickInputItems).OnItemSelected -= OnSplashCommandSelected;
						((SplashInputItems)_quickInputItems).OnItemSelected += OnSplashCommandSelected;
					}
					break;
				}
				break;
			case 2:
				if (mark == "[[" && !usedInCal)
				{
					List<TaskBaseViewModel> taskInQuickLink = TaskCache.GetTaskInQuickLink();
					if (taskInQuickLink.Count > 0 && !(_quickInputItems is TaskLinkInputItems))
					{
						_quickInputItems = new TaskLinkInputItems(taskInQuickLink);
						((TaskLinkInputItems)_quickInputItems).OnItemSelected -= TaskSelect;
						((TaskLinkInputItems)_quickInputItems).OnItemSelected += TaskSelect;
					}
				}
				break;
			}
		}
		return _quickInputItems != null;
	}

	public static bool ShouldUseCalendarItems(string mark, bool usedInCal, bool isCalendar)
	{
		if (mark == "^" || mark == "~")
		{
			return usedInCal || isCalendar;
		}
		return false;
	}

	private void OnSplashCommandSelected(object sender, InputItemViewModel<string> model)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Splash,
			SplashCommand = model.Entity,
			Title = model.Value
		});
		UserActCollectUtils.AddClickEvent("slash_om", "input_box", model.Value);
		if (model.Entity == "attachment")
		{
			_popup.IsOpen = false;
		}
	}

	private void TaskSelect(object sender, InputItemViewModel<TaskBaseViewModel> e)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.TaskLink,
			task = e.Entity,
			Title = e.Title
		});
		_popup.IsOpen = false;
	}

	private void GetAvatars(string avatarProjectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == avatarProjectId);
		if (projectModel == null || !projectModel.IsShareList())
		{
			_avatars = null;
			return;
		}
		_avatars = AvatarHelper.GetCacheProjectAvatars(avatarProjectId);
		TryPullRemoteUsers(avatarProjectId);
		_avatarProject = avatarProjectId;
	}

	private void OnQuickDateSelected(object sender, InputItemViewModel<DateTime> e)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Date,
			Date = e.Entity,
			Title = e.Value
		});
		_popup.IsOpen = false;
	}

	private void AssignSelect(object sender, InputItemViewModel<AvatarViewModel> e)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Assign,
			Avatar = e.Entity,
			Title = e.Title
		});
		_popup.IsOpen = false;
	}

	private void TagSelect(object sender, string e)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Tag,
			Tag = e,
			Title = e
		});
		_popup.IsOpen = false;
	}

	private void OnCalendarSelect(object sender, InputItemViewModel<string> e)
	{
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.CalId,
			CalId = e.Entity,
			Title = e.Title
		});
		_popup.IsOpen = false;
	}

	private void OnProjectSelect(object sender, InputItemViewModel<ProjectModel> e)
	{
		_popup.IsOpen = false;
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Project,
			Project = e.Entity,
			Title = e.Title
		});
	}

	private void OnPrioritySelect(object sender, InputItemViewModel<int> e)
	{
		_popup.IsOpen = false;
		this.ItemSelected?.Invoke(sender, new QuickSetModel
		{
			Type = QuickSetType.Priority,
			Priority = e.Entity,
			Title = e.Title
		});
	}

	public void TryShowPopup(string mark, string content, Point position, bool keepPreviousOffset = false)
	{
		if (_quickInputItems.Filter(content))
		{
			ItemsContainer.Children.Clear();
			if (_quickInputItems is UIElement element)
			{
				ItemsContainer.Children.Add(element);
			}
			if (!keepPreviousOffset)
			{
				_popup.HorizontalOffset = position.X;
				_popup.VerticalOffset = position.Y;
			}
			_popup.IsOpen = true;
			base.Tag = mark;
		}
		else
		{
			ItemsContainer.Children.Clear();
			_popup.IsOpen = false;
		}
	}

	public void ClosePopup(bool clearChildren = true)
	{
		_popup.IsOpen = false;
		if (clearChildren)
		{
			ItemsContainer.Children.Clear();
		}
	}

	public void TryMovePopupItem(bool forward)
	{
		if (_popup.IsOpen && ItemsContainer.IsVisible && ItemsContainer.Children[0] is IQuickInput quickInput)
		{
			quickInput.Move(forward);
		}
	}

	public void TrySelectItem()
	{
		if (ItemsContainer.IsVisible && ItemsContainer.Children[0] is IQuickInput quickInput)
		{
			quickInput.TrySelectItem();
		}
	}

	public void Move(bool forward)
	{
		if (ItemsContainer.IsVisible && ItemsContainer.Children[0] is IQuickInput quickInput)
		{
			quickInput.Move(forward);
		}
	}

	public async void TryPullRemoteUsers(string projectId)
	{
		if (!(_avatarProject != projectId) && !((DateTime.Now - _lastPullTime).TotalMinutes >= 5.0))
		{
			return;
		}
		_avatarProject = projectId;
		_lastPullTime = DateTime.Now;
		await AvatarHelper.ResetProjectAvatars(projectId);
		if (_quickInputItems is AssignInputItems assignInputItems)
		{
			List<AvatarViewModel> cacheProjectAvatars = AvatarHelper.GetCacheProjectAvatars(projectId);
			List<string> list = cacheProjectAvatars?.Select((AvatarViewModel a) => a.UserCode).ToList();
			List<string> list2 = _avatars?.Select((AvatarViewModel a) => a.UserCode).ToList();
			if (list == null || list2 == null || list.Count != list2.Count || list.Union(list2).Count() != list2.Count)
			{
				_avatars = cacheProjectAvatars;
				assignInputItems.SetModels(_avatars);
			}
		}
	}

	internal void TryRemoveTaskSelector()
	{
		if (_quickInputItems is TaskLinkInputItems)
		{
			_quickInputItems = null;
		}
	}

	public bool IsOpen()
	{
		return _popup.IsOpen;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/quickadd/quickselectioncontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			ItemsContainer = (Grid)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
