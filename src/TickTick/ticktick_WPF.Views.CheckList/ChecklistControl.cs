using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.DragAndDrop;
using ticktick_WPF.Util.KotlinUtils;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views.CheckList;

public class ChecklistControl : UserControl, IComponentConnector
{
	public delegate void ChecklistChangedDelegate(string taskId, string itemId, CheckItemModifyType type);

	public static readonly DependencyProperty InQuickAddWindowProperty = DependencyProperty.Register("InQuickAddWindow", typeof(bool), typeof(ChecklistControl), new PropertyMetadata(false, null));

	private bool _dragging;

	private Point _lastHoverIndex = new Point(-1.0, -1.0);

	private Popup _popup;

	public bool IsNewAddTask;

	private Window _window;

	private TaskDetailView _parentControl;

	private bool _inSticky;

	private string _uid;

	private bool _isStickyDark;

	private bool _dragOver;

	private bool _isSettingData;

	internal ItemsControl CheckListView;

	internal Popup TaskDragPopup;

	private bool _contentLoaded;

	public bool InQuickAddWindow
	{
		get
		{
			return (bool)GetValue(InQuickAddWindowProperty);
		}
		set
		{
			SetValue(InQuickAddWindowProperty, value);
		}
	}

	public string TaskId { get; set; }

	private ItemCollection Items => CheckListView.Items;

	public List<CheckItemViewModel> ChecklistItems { get; set; } = new List<CheckItemViewModel>();

	public bool InMainDetail { get; set; }

	public event EventHandler CaretMoveUp;

	public event ChecklistChangedDelegate Changed;

	public event EventHandler ItemsClear;

	public event EventHandler<string> ItemDrop;

	public event EventHandler<string> TagSelected;

	public event EventHandler<bool> PopOpened;

	public event EventHandler<bool> PopClosed;

	public event EventHandler<TaskDetailItemModel> CheckItemsDeleted;

	public event EventHandler<string> ToastUnableText;

	public event EventHandler<ProjectTask> Navigate;

	public event EventHandler DatePopOpened;

	public event EventHandler DatePopClosed;

	public event EventHandler QuickAddTask;

	public event EventHandler<QuickSetModel> QuickItemSelected;

	public event EventHandler<double> CaretVerticalOffsetChanged;

	public ChecklistControl()
	{
		InitializeComponent();
		_uid = Utils.GetGuid();
		base.Loaded += delegate
		{
			TickDragHelper.DragDrop += OnDragDrop;
			TickDragHelper.DragMoving += OnDragOver;
		};
		base.Unloaded += delegate
		{
			TickDragHelper.DragDrop -= OnDragDrop;
			TickDragHelper.DragMoving -= OnDragOver;
		};
	}

	public async Task SetData(List<CheckItemViewModel> data)
	{
		if (_isSettingData)
		{
			return;
		}
		_isSettingData = true;
		try
		{
			List<CheckItemViewModel> sortedData = (ChecklistItems = data.Where((CheckItemViewModel item) => item.IsValid).OrderBy((CheckItemViewModel x) => x, new ChecklistItemComparaer()).ToList());
			int originalCount = Items.Count;
			int revisedCount = sortedData.Count;
			if (originalCount > revisedCount)
			{
				List<object> list2 = new List<object>();
				for (int num = revisedCount; num < originalCount; num++)
				{
					list2.Add(Items[num]);
				}
				foreach (object item in list2)
				{
					Items.Remove(item);
				}
				for (int num2 = 0; num2 < revisedCount; num2++)
				{
					if (CheckListView.Items[num2] is ChecklistItemControl checklistItemControl)
					{
						checklistItemControl.DataContext = sortedData[num2];
					}
				}
				return;
			}
			for (int num3 = 0; num3 < originalCount; num3++)
			{
				if (Items[num3] is ChecklistItemControl checklistItemControl2)
				{
					checklistItemControl2.DataContext = sortedData[num3];
				}
			}
			for (int i = 0; i < revisedCount - originalCount; i++)
			{
				ChecklistItemControl newItem = new ChecklistItemControl(this, _inSticky, _isStickyDark)
				{
					DataContext = sortedData[originalCount + i]
				};
				Items.Add(newItem);
				await Task.Delay(1);
			}
		}
		finally
		{
			_isSettingData = false;
		}
	}

	public string ToText(string description)
	{
		return ChecklistUtils.Items2Text(description, ChecklistItems?.Select((CheckItemViewModel item) => item.Title).ToList());
	}

	public async void ScrollToItem(string id)
	{
		int indexById = GetIndexById(id);
		if (indexById >= 0 && indexById < Items.Count && Items[indexById] is ChecklistItemControl checklistItemControl)
		{
			checklistItemControl.Blink();
			Point point = checklistItemControl.TranslatePoint(new Point(0.0, 40.0), this);
			this.CaretVerticalOffsetChanged?.Invoke(this, point.Y + 8.0);
		}
	}

	public ChecklistItemControl GetItemControlByIndex(int index)
	{
		try
		{
			if (index < Items.Count && Items[index] is ChecklistItemControl result)
			{
				return result;
			}
		}
		catch (ArgumentOutOfRangeException)
		{
		}
		return null;
	}

	public void Unregister(ChecklistItemControl item)
	{
		item.Merge -= MergeItem;
		item.MoveUp -= MoveUp;
		item.MoveDown -= MoveDown;
		item.Check -= CompleteItem;
		item.UnCheck -= UnCompleteItem;
		item.TitleChanged -= OnItemTitleChanged;
		item.TagSelected -= OnTagSelected;
		item.PopOpened -= OnPopOpened;
		item.PopClosed -= OnPopClosed;
		item.ToastString -= OnItemToastString;
		item.QuickItemSelected -= OnQuickItemSelected;
		item.TitleTextBox.Navigate -= OnNavigateTask;
		item.QuickAddTask -= OnQuickAddTask;
		item.CaretVerticalOffsetChanged -= OnItemCaretVOffsetChanged;
	}

	public void Register(ChecklistItemControl item)
	{
		Unregister(item);
		item.Merge += MergeItem;
		item.MoveUp += MoveUp;
		item.MoveDown += MoveDown;
		item.Check += CompleteItem;
		item.UnCheck += UnCompleteItem;
		item.TitleChanged += OnItemTitleChanged;
		item.TagSelected += OnTagSelected;
		item.PopOpened += OnPopOpened;
		item.PopClosed += OnPopClosed;
		item.ToastString += OnItemToastString;
		item.QuickItemSelected += OnQuickItemSelected;
		item.TitleTextBox.Navigate += OnNavigateTask;
		item.QuickAddTask += OnQuickAddTask;
		item.CaretVerticalOffsetChanged += OnItemCaretVOffsetChanged;
	}

	public TaskDetailView GetParent()
	{
		if (_parentControl == null)
		{
			_parentControl = Utils.FindParent<TaskDetailView>(this);
		}
		return _parentControl;
	}

	private void OnQuickAddTask(object sender, EventArgs e)
	{
		this.QuickAddTask?.Invoke(sender, e);
	}

	private void OnItemCaretVOffsetChanged(object sender, double e)
	{
		if (sender is ChecklistItemControl checklistItemControl)
		{
			Point point = checklistItemControl.TranslatePoint(new Point(0.0, 0.0), this);
			this.CaretVerticalOffsetChanged?.Invoke(this, e + point.Y + 8.0);
		}
	}

	private void OnQuickItemSelected(object sender, QuickSetModel e)
	{
		this.QuickItemSelected?.Invoke(sender, e);
	}

	private void OnNavigateTask(object sender, ProjectTask task)
	{
		this.Navigate?.Invoke(sender, task);
	}

	private void OnPopClosed(object sender, bool e)
	{
		this.PopClosed?.Invoke(this, e);
	}

	public void OnPopupClosed(bool b)
	{
		this.PopClosed?.Invoke(this, b);
	}

	private void OnPopOpened(object sender, bool e)
	{
		this.PopOpened?.Invoke(this, e);
	}

	private void OnTagSelected(object sender, string tag)
	{
		this.TagSelected?.Invoke(sender, tag);
	}

	private async void OnItemDrop(CheckItemViewModel model)
	{
		if (model != null)
		{
			await SetSortOrderOnDrop(model);
			if (IsNewAddTask)
			{
				SortChecklistItems();
			}
		}
	}

	private async Task SetSortOrderOnDrop(CheckItemViewModel model)
	{
		CheckItemViewModel dropTarget = ChecklistItems.FirstOrDefault((CheckItemViewModel item) => item.ShowBottomDropLine || item.ShowTopDropLine);
		if (dropTarget == null)
		{
			return;
		}
		List<CheckItemViewModel> list = ChecklistItems.OrderBy((CheckItemViewModel item) => item.SortOrder).ToList();
		List<long> sortOrders = list.Select((CheckItemViewModel i) => i.SortOrder).ToList();
		await CheckSortRepeated(list, sortOrders);
		int num = -1;
		for (int num2 = 0; num2 < sortOrders.Count; num2++)
		{
			if (sortOrders[num2] == dropTarget.SortOrder)
			{
				num = num2;
				break;
			}
		}
		long num3 = dropTarget.SortOrder;
		if (num >= 0)
		{
			if (dropTarget.ShowTopDropLine)
			{
				num3 = ((num != 0) ? ((sortOrders[num - 1] + sortOrders[num]) / 2) : (num3 - 268435456));
			}
			else if (dropTarget.ShowBottomDropLine)
			{
				num3 = ((num != sortOrders.Count - 1) ? ((sortOrders[num] + sortOrders[num + 1]) / 2) : (num3 + 268435456));
			}
			model.SourceViewModel.SortOrder = num3;
			CheckItemViewModel checkItemViewModel = ChecklistItems.FirstOrDefault((CheckItemViewModel item) => item.Id == model.Id);
			if (checkItemViewModel != null)
			{
				checkItemViewModel.SourceViewModel.SortOrder = num3;
			}
			await model.Save(checkCount: false);
		}
		if (!IsNewAddTask)
		{
			await SyncStatusDao.AddModifySyncStatus(model.TaskServerId);
			SyncManager.TryDelaySync(1000);
		}
	}

	private async Task CheckSortRepeated(List<CheckItemViewModel> items, List<long> sortOrders)
	{
		bool flag = false;
		for (int i = 0; i < sortOrders.Count - 1; i++)
		{
			if (sortOrders[i] < -9223372035781033984L || sortOrders[i] > 9223372035781033983L)
			{
				flag = true;
				break;
			}
			if (Math.Abs(sortOrders[i + 1] - sortOrders[i]) < 16)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		long order = 0L;
		foreach (CheckItemViewModel item in items)
		{
			item.SourceViewModel.SortOrder = order;
			order += 268435456;
			await item.Save(checkCount: false);
		}
		sortOrders.Clear();
		sortOrders.AddRange(items.Select((CheckItemViewModel checkItemViewModel) => checkItemViewModel.SortOrder).ToList());
	}

	private void MoveDragPopup(MouseEventArgs arg)
	{
		SetDropLine(arg);
	}

	private void SetDropLine(MouseEventArgs arg)
	{
		ClearDropLine();
		if (ChecklistItems == null || ChecklistItems.Count == 0)
		{
			return;
		}
		ChecklistItemControl mousePointItem = Utils.GetMousePointItem<ChecklistItemControl>(arg.GetPosition(this), this);
		if (mousePointItem == null || !(mousePointItem.DataContext is CheckItemViewModel checkItemViewModel))
		{
			return;
		}
		if (checkItemViewModel.Status != 0)
		{
			CheckItemViewModel checkItemViewModel2 = ChecklistItems.LastOrDefault((CheckItemViewModel i) => i.Status == 0);
			if (checkItemViewModel2 != null)
			{
				checkItemViewModel2.ShowBottomDropLine = true;
			}
		}
		else if (arg.GetPosition(mousePointItem).Y < 20.0)
		{
			checkItemViewModel.ShowTopDropLine = true;
		}
		else
		{
			checkItemViewModel.ShowBottomDropLine = true;
		}
	}

	private void OnItemTitleChanged(object sender, ItemTextChange change)
	{
		if (!sender.Equals(this))
		{
			if (ChecklistItems.Count == 1)
			{
				ChecklistItems[0].ShowAddHint = true;
			}
			if (!InQuickAddWindow && !IsNewAddTask)
			{
				TaskChangeNotifier.NotifyTaskTextChanged(TaskId);
			}
		}
	}

	public async void NotifyTaskChanged(CheckItemModifyType type)
	{
		if (!string.IsNullOrEmpty(TaskId) && !IsNewAddTask)
		{
			await SyncStatusDao.AddSyncStatus(TaskId, 0);
			this.Changed?.Invoke(TaskId, string.Empty, type);
		}
	}

	private async void UnCompleteItem(object sender, string id)
	{
		CheckItemViewModel item = GetItemModelById(id);
		if (item != null)
		{
			item.SourceViewModel.Status = 0;
			item.SourceViewModel.CompletedTime = null;
			await Task.Delay(50);
			await SaveAndNotify(item, CheckItemModifyType.Uncheck);
			if (!InQuickAddWindow)
			{
				ItemChangeNotifier.NotifyItemStatusChanged(item.Id);
			}
		}
	}

	private async void CompleteItem(object sender, string id)
	{
		CheckItemViewModel item = GetItemModelById(id);
		if (item == null)
		{
			return;
		}
		DateTime? dateTime = GetParent()?.GetDisplayDate();
		if (dateTime.HasValue)
		{
			IToastShowWindow toastWindow = Utils.FindParent<TaskDetailPopup>(this)?.DependentWindow;
			if (!(await ModifyRepeatHandler.CompleteOrSkipRecurrence(item.TaskServerId, dateTime, toastWindow)))
			{
				return;
			}
		}
		item.SourceViewModel.Status = 1;
		item.SourceViewModel.CompletedTime = DateTime.Now;
		await Task.Delay(50);
		await SaveAndNotify(item, CheckItemModifyType.Check);
		if (!InQuickAddWindow && !IsNewAddTask)
		{
			ItemChangeNotifier.NotifyItemStatusChanged(item.Id);
			Utils.PlayCompletionSound();
		}
	}

	private void NotifyItemCompleted(string id)
	{
		int indexById = GetIndexById(id);
		List<CheckItemViewModel> list = ChecklistItems.ToList();
		if (indexById >= 0 && indexById < list.Count)
		{
			SetData(list);
		}
	}

	private async Task SaveAndNotify(CheckItemViewModel item, CheckItemModifyType type)
	{
		if ((uint)(type - 3) <= 1u)
		{
			NotifyItemCompleted(item.Id);
		}
		else
		{
			SortChecklistItems();
		}
		if (!InQuickAddWindow && !IsNewAddTask)
		{
			await item.Save();
			await SyncStatusDao.AddSyncStatus(TaskId, 0);
			this.Changed?.Invoke(TaskId, item.Id, type);
		}
	}

	public void SortChecklistItems()
	{
		DelayActionHandlerCenter.TryDoAction(_uid, delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				SetData(ChecklistItems);
			});
		}, 100);
	}

	private void MoveUp(object sender, string id)
	{
		int indexById = GetIndexById(id);
		if (indexById > 0)
		{
			GetItemControlByIndex(indexById - 1)?.FocusEnd();
		}
		else if (indexById == 0)
		{
			this.CaretMoveUp?.Invoke(this, null);
		}
	}

	private async void FocusTitle(string id)
	{
		int indexById = GetIndexById(id);
		ChecklistItemControl item = GetItemControlByIndex(indexById);
		if (item != null)
		{
			await Task.Delay(10);
			item?.FocusTitle(-1, scroll: true);
		}
	}

	private void MoveDown(object sender, string id)
	{
		int indexById = GetIndexById(id);
		GetItemControlByIndex(indexById + 1)?.FocusEnd();
	}

	private async void MergeItem(object sender, ItemTextChange change)
	{
		int indexById = GetIndexById(change.Id);
		if (indexById > 0)
		{
			RemoveItemById(change.Id, allowRestore: false);
			CheckItemViewModel lastItem = ChecklistItems[indexById - 1];
			lastItem.SourceViewModel.Title += change.Text;
			FocusAndUpdateTitle(lastItem, indexById - 1, 1, 0, focusEnd: true);
			if (!IsNewAddTask)
			{
				await TaskDetailItemDao.DeleteById(change.Id);
				await lastItem.Save();
			}
		}
		else
		{
			if (indexById != 0 || !string.IsNullOrEmpty(change.Text))
			{
				return;
			}
			RemoveItemById(change.Id, allowRestore: false);
			if (ChecklistItems.Count >= 1)
			{
				GetItemControlByIndex(0)?.FocusTitle(0);
				if (!IsNewAddTask)
				{
					await TaskDetailItemDao.DeleteById(change.Id);
				}
			}
		}
	}

	public void SplitNewItem(ItemTextChange change)
	{
		int num = ChecklistItems.Count((CheckItemViewModel item) => item.IsValid);
		if (CheckItemOverLimit(num + 1))
		{
			return;
		}
		int indexById = GetIndexById(change.Id);
		if (indexById >= 0 && indexById < ChecklistItems.Count)
		{
			CheckItemViewModel checkItemViewModel = ChecklistItems[indexById];
			if (checkItemViewModel.Status == 0)
			{
				SplitNextItem(change, indexById);
			}
			else
			{
				InsertEmptyItemAbove(checkItemViewModel);
			}
		}
		List<CheckItemViewModel> list = ChecklistItems.OrderBy((CheckItemViewModel item) => item.SortOrder).ToList();
		List<long> sortOrders = list.Select((CheckItemViewModel i) => i.SortOrder).ToList();
		CheckSortRepeated(list, sortOrders);
	}

	private async void SplitNextItem(ItemTextChange change, int currentIndex)
	{
		int num = ChecklistItems.Count((CheckItemViewModel item) => item.IsValid);
		if (CheckItemOverLimit(num + 1) || currentIndex < 0 || currentIndex >= ChecklistItems.Count)
		{
			return;
		}
		CheckItemViewModel checkItemViewModel = ChecklistItems[currentIndex];
		ChecklistItemControl itemControlByIndex = GetItemControlByIndex(currentIndex);
		if (itemControlByIndex == null)
		{
			return;
		}
		if (Utils.GetDescendantByType(itemControlByIndex, typeof(DetailTextBox), "TitleTextBox") is DetailTextBox detailTextBox)
		{
			detailTextBox.SetTextAndOffset(change.Text, restoreOffset: true, clearUndo: true);
		}
		DateTime? startDate = checkItemViewModel.StartDate;
		bool flag = false;
		if (string.IsNullOrEmpty(change.Text))
		{
			checkItemViewModel.SourceViewModel.StartDate = null;
			flag = true;
		}
		string newId = Utils.GetGuid();
		CheckItemViewModel newItem = new CheckItemViewModel(new TaskBaseViewModel
		{
			Id = newId,
			ParentId = checkItemViewModel.TaskServerId,
			Title = change.Extra,
			SortOrder = CalcSortOrder(checkItemViewModel.SortOrder, isUp: false),
			Status = 0,
			StartDate = (flag ? startDate : ((DateTime?)null)),
			IsAllDay = (flag ? checkItemViewModel.IsAllDay : new bool?(true)),
			TimeZoneName = checkItemViewModel.TimeZoneName,
			IsFloating = checkItemViewModel.IsFloating
		})
		{
			IsValid = true,
			RepeatDiff = checkItemViewModel.RepeatDiff
		};
		int num2 = currentIndex + 1;
		List<CheckItemViewModel> list = ChecklistItems.ToList();
		if (list.Count > num2 && !list[num2].IsValid)
		{
			list[num2] = newItem;
		}
		else
		{
			list.Insert(num2, newItem);
		}
		InsertItem(num2, newItem);
		ChecklistItems = list;
		if (GetParent() != null && !IsNewAddTask)
		{
			ChecklistItemControl itemControlByIndex2 = GetItemControlByIndex(currentIndex);
			if (itemControlByIndex2 != null)
			{
				checkItemViewModel.SourceViewModel.Title = itemControlByIndex2.TitleTextBox.Text;
			}
			await checkItemViewModel.Save();
			await newItem.Insert();
			TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(newId);
			if (checkItemById != null)
			{
				newItem.SetSourceModel(checkItemById);
			}
			DelayNotifyAddCheckItem();
		}
	}

	private void InsertItem(int index, CheckItemViewModel newItem)
	{
		ChecklistItemControl checklistItemControl = new ChecklistItemControl(this, _inSticky, _isStickyDark)
		{
			DataContext = newItem
		};
		checklistItemControl.FocusOnLoaded();
		Items.Insert(index, checklistItemControl);
	}

	private bool CheckItemOverLimit(int count)
	{
		long limitByKey = LimitCache.GetLimitByKey(Constants.LimitKind.SubtaskNumber);
		if (count > limitByKey)
		{
			if (UserDao.IsPro())
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("ProSubtaskLimit"), limitByKey), MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			else
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreSubTasks);
			}
			return true;
		}
		return false;
	}

	private async void DelayNotifyAddCheckItem()
	{
		if (!IsNewAddTask)
		{
			await Task.Delay(100);
			NotifyTaskChanged(CheckItemModifyType.Add);
		}
	}

	private long CalcSortOrder(long currentOrder, bool isUp)
	{
		List<long> list = (from item in ChecklistItems
			select item.SortOrder into item
			orderby item
			select item).ToList();
		int num = -1;
		if (list.Count > 0)
		{
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				if (list[num2] == currentOrder)
				{
					num = num2;
					break;
				}
			}
			if (isUp)
			{
				if (num == 0)
				{
					return currentOrder - 268435456;
				}
				return currentOrder - (currentOrder - list[num - 1]) / 2;
			}
			if (num == list.Count - 1)
			{
				return currentOrder + 268435456;
			}
			return currentOrder + (list[num + 1] - currentOrder) / 2;
		}
		return 0L;
	}

	private async void InsertEmptyItemAbove(CheckItemViewModel currentItem)
	{
		CheckItemViewModel item = new CheckItemViewModel(new TaskBaseViewModel
		{
			Id = Utils.GetGuid(),
			ParentId = currentItem.TaskServerId,
			Title = string.Empty,
			SortOrder = CalcSortOrder(currentItem.SortOrder, isUp: true),
			Status = 0
		})
		{
			IsValid = true,
			RepeatDiff = currentItem.RepeatDiff
		};
		int firstCompleteIndex = GetFirstCompleteIndex();
		List<CheckItemViewModel> list = ChecklistItems.ToList();
		if (firstCompleteIndex < 0)
		{
			return;
		}
		if (list.Count > firstCompleteIndex)
		{
			list[firstCompleteIndex] = item;
		}
		else
		{
			list.Insert(firstCompleteIndex, item);
		}
		SetData(list);
		FocusAndUpdateTitle(item, firstCompleteIndex, 1);
		if (!IsNewAddTask)
		{
			await item.Insert();
			TaskBaseViewModel checkItemById = TaskDetailItemCache.GetCheckItemById(item.Id);
			if (checkItemById != null)
			{
				item.SetSourceModel(checkItemById);
			}
			NotifyTaskChanged(CheckItemModifyType.Add);
		}
	}

	private int GetFirstCompleteIndex()
	{
		for (int i = 0; i < ChecklistItems.Count; i++)
		{
			if (ChecklistItems[i].Status != 0)
			{
				return i;
			}
		}
		return -1;
	}

	public async void RemoveItemById(string id, bool allowRestore = true)
	{
		CheckItemViewModel item = ChecklistItems?.FirstOrDefault((CheckItemViewModel t) => t.Id == id);
		if (item == null)
		{
			return;
		}
		RemoveItem(item);
		ChecklistItems.Remove(item);
		if (IsNewAddTask)
		{
			if (ChecklistItems.Count == 0)
			{
				this.ItemsClear?.Invoke(this, null);
			}
			return;
		}
		TaskModel taskModel = await TaskService.DeleteCheckItem(id);
		if (taskModel != null && taskModel.kind == "TEXT")
		{
			this.ItemsClear?.Invoke(this, null);
		}
		else
		{
			NotifyTaskChanged(CheckItemModifyType.Delete);
		}
		if (allowRestore)
		{
			TaskDetailItemModel e = new TaskDetailItemModel
			{
				id = Utils.GetGuid(),
				TaskServerId = item.TaskServerId,
				title = item.Title,
				isAllDay = item.IsAllDay,
				startDate = item.StartDate,
				sortOrder = item.SortOrder,
				snoozeReminderTime = item.SnoozeReminderTime,
				completedTime = item.CompletedTime,
				status = item.Status
			};
			this.CheckItemsDeleted?.Invoke(this, e);
		}
	}

	private void RemoveItem(CheckItemViewModel model)
	{
		int indexById = GetIndexById(model.Id);
		if (indexById >= 0)
		{
			ChecklistItemControl itemControlByIndex = GetItemControlByIndex(indexById);
			if (itemControlByIndex.DataContext is CheckItemViewModel checkItemViewModel && checkItemViewModel.Id == model.Id)
			{
				Items.Remove(itemControlByIndex);
			}
		}
	}

	private async void FocusAndUpdateTitle(CheckItemViewModel model, int itemIndex, int delay = 0, int caretIndex = 0, bool focusEnd = false)
	{
		ChecklistItemControl itemControlByIndex = GetItemControlByIndex(itemIndex);
		if (itemControlByIndex != null && Utils.GetDescendantByType(itemControlByIndex, typeof(DetailTextBox), "TitleTextBox") is DetailTextBox detailTextBox)
		{
			detailTextBox.Text = model.Title;
			Utils.Focus(detailTextBox);
			_ = delay;
			_ = 0;
			if (focusEnd)
			{
				detailTextBox.CaretOffset = detailTextBox.Text.Length;
			}
			else if (caretIndex > 0)
			{
				detailTextBox.CaretOffset = caretIndex;
			}
		}
	}

	private CheckItemViewModel GetItemModelById(string id)
	{
		for (int i = 0; i < ChecklistItems.Count; i++)
		{
			CheckItemViewModel checkItemViewModel = ChecklistItems[i];
			if (checkItemViewModel.Id == id)
			{
				return checkItemViewModel;
			}
		}
		return null;
	}

	private int GetIndexById(string id)
	{
		for (int i = 0; i < ChecklistItems.Count; i++)
		{
			if (ChecklistItems[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}

	public void FocusFirstItem()
	{
		if (ChecklistItems != null && ChecklistItems.Count > 0)
		{
			CheckItemViewModel model = ChecklistItems[0];
			FocusAndUpdateTitle(model, 0, 50, 0, focusEnd: true);
		}
	}

	private void ClearDropLine()
	{
		ChecklistItems.ToList().ForEach(delegate(CheckItemViewModel item)
		{
			item.ShowTopDropLine = false;
			item.ShowBottomDropLine = false;
		});
	}

	public async Task HandleTaskDrop(string taskId)
	{
		if (!(GetParent()?.CheckEnable() ?? true))
		{
			return;
		}
		string toTaskId = TaskId;
		if (taskId == toTaskId)
		{
			return;
		}
		CheckItemViewModel targetItem = ChecklistItems?.FirstOrDefault((CheckItemViewModel item) => item.ShowTopDropLine || item.ShowBottomDropLine);
		if (targetItem == null)
		{
			return;
		}
		bool topOrBottom = targetItem.ShowTopDropLine;
		ClearDropLine();
		TaskModel fromTask = await TaskDao.GetThinTaskById(taskId);
		if (fromTask == null)
		{
			return;
		}
		List<TaskModel> tasks = (await TaskDao.GetAllSubTasksById(taskId, fromTask.projectId)) ?? new List<TaskModel>();
		tasks.Insert(0, fromTask);
		if (tasks.Any((TaskModel t) => t.id == toTaskId))
		{
			return;
		}
		if (!string.IsNullOrEmpty(fromTask.attendId))
		{
			this.ToastUnableText?.Invoke(null, Utils.GetString("DropAgendaHint"));
			return;
		}
		TaskModel dropTask = await TaskDao.GetThinTaskById(toTaskId);
		if (dropTask != null && !string.IsNullOrEmpty(dropTask.attendId) && !AgendaHelper.CanAccessAgenda(dropTask))
		{
			this.ToastUnableText?.Invoke(null, Utils.GetString("AttendeeModifyContent"));
		}
		else if (CanCreateSubtask())
		{
			if (await TaskService.TaskToSubtask(tasks, toTaskId, targetItem.Id, topOrBottom) != null && await TagDao.HandleDropTaskTags(fromTask, dropTask))
			{
				GetParent()?.Navigate(dropTask?.id);
			}
			TaskChangeNotifier.NotifyDropToItem(taskId);
		}
	}

	private bool CanCreateSubtask()
	{
		GetParent()?.SetPopupStatus(show: true);
		bool? flag = new CustomerDialog(Utils.GetString("DragTaskToChecklist"), Utils.GetString("DragTaskToChecklistWarning"), Utils.GetString("OK"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog();
		GetParent()?.SetPopupStatus(show: false);
		if (flag != true)
		{
			return false;
		}
		return true;
	}

	public void TryToastUnableText()
	{
		this.ToastUnableText?.Invoke(null, null);
	}

	private void OnItemToastString(object sender, string e)
	{
		this.ToastUnableText?.Invoke(null, e);
	}

	public void ScrollToSearchOffset()
	{
		if (ChecklistItems == null || ChecklistItems.Count == 0)
		{
			return;
		}
		IEnumerator enumerator = ((IEnumerable)Items).GetEnumerator();
		try
		{
			while (enumerator.MoveNext() && (!(enumerator.Current is ChecklistItemControl { IsVisible: not false } checklistItemControl) || !(checklistItemControl.GetSearchOffset() >= 0.0)))
			{
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}

	public void SetStickyMode()
	{
		_inSticky = true;
	}

	public void OnDatePopupOpenChange(bool isOpen)
	{
		if (isOpen)
		{
			this.DatePopOpened?.Invoke(this, null);
		}
		else
		{
			this.DatePopClosed?.Invoke(this, null);
		}
	}

	public void SetStickyTheme(bool isDark)
	{
		_isStickyDark = isDark;
		foreach (object item in (IEnumerable)Items)
		{
			if (item is ChecklistItemControl checklistItemControl)
			{
				checklistItemControl.SetStickyTheme(isDark);
			}
		}
	}

	public void SetTaskId(string taskId)
	{
		TaskId = taskId;
	}

	public bool ItemFocus()
	{
		return FocusManager.GetFocusedElement(this) != null;
	}

	public void RemoveEvents()
	{
		foreach (object item in (IEnumerable)Items)
		{
			if (item is ChecklistItemControl checklistItemControl)
			{
				Unregister(checklistItemControl);
				checklistItemControl.UnbindEvent();
			}
		}
	}

	private void OnDragDrop(object sender, MouseEventArgs e)
	{
		if (_dragOver)
		{
			object dragData = TickDragHelper.GetDragData();
			if (dragData is CheckItemViewModel model)
			{
				OnItemDrop(model);
			}
			if (dragData is DisplayItemModel displayItemModel)
			{
				HandleTaskDrop(displayItemModel.Id);
			}
			GetParent()?.StopAutoScroll();
			ClearDropLine();
		}
		_dragOver = false;
	}

	private void OnDragOver(object sender, MouseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		Window window = Window.GetWindow(this);
		if (window == null || !window.IsMouseCaptured || (TaskDetailPopup.IsDragOver(e) && Utils.FindParent<TaskDetailPopup>(this) == null))
		{
			return;
		}
		TaskDetailView parent = GetParent();
		if (parent != null && !parent.CheckEnable())
		{
			return;
		}
		Point position = e.GetPosition(this);
		bool flag = !(position.X < 0.0) && !(position.X > base.ActualWidth) && !(position.Y < 0.0) && !(position.Y > base.ActualHeight);
		if (_dragOver != flag)
		{
			if (!flag)
			{
				OnDragLeave();
			}
			_dragOver = flag;
		}
		if (flag)
		{
			if (TickDragHelper.GetDragData() is BaseViewModel)
			{
				GetParent()?.TryAutoScroll(e);
			}
			MoveDragPopup(e);
		}
	}

	private void OnDragLeave()
	{
		ClearDropLine();
		GetParent()?.StopAutoScroll();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/checklist/checklistcontrol.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			CheckListView = (ItemsControl)target;
			break;
		case 2:
			TaskDragPopup = (Popup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
