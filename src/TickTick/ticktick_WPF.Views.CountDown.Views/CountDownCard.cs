using System;
using System.CodeDom.Compiler;
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
using System.Windows.Shapes;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountDownCard : UserControl, IComponentConnector
{
	private double _lastHeight;

	internal Border MainBorder;

	internal ContentControl TemplateControl;

	internal ArrowHintUserControl SwitchFormatHint;

	internal Grid PinButton2Contianer;

	internal Button PinButton2;

	internal Path PinPath2;

	internal Button PinButton;

	internal Path PinPath;

	internal Button MoreButton2;

	internal Button MoreButton;

	internal EscPopup MorePopup;

	internal EscPopup RightMorePopup;

	private bool _contentLoaded;

	private CountDownViewModel ViewModel => base.DataContext as CountDownViewModel;

	private MainCountDownViewModel MainCountDownViewModel => App.Window.CountDownContainer?.DataContext as MainCountDownViewModel;

	public CountDownCard()
	{
		InitializeComponent();
		base.Loaded += CountDownCard_Loaded;
		base.Unloaded += CountDownCard_Unloaded;
		base.LayoutUpdated += CountDownCard_LayoutUpdated;
		base.DataContextChanged += CountDownCard_DataContextChanged;
	}

	private void CountDownCard_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is CountDownViewModel countDownViewModel)
		{
			countDownViewModel.OnShowRemarkDetail = (Action)Delegate.Remove(countDownViewModel.OnShowRemarkDetail, new Action(ShowRemarkWindow));
		}
		if (e.NewValue is CountDownViewModel countDownViewModel2)
		{
			countDownViewModel2.OnShowRemarkDetail = (Action)Delegate.Combine(countDownViewModel2.OnShowRemarkDetail, new Action(ShowRemarkWindow));
			if (!LocalSettings.Settings.ExtraSettings.CheckHasFlag(HasSetFlag.CountdownFormatHint) && countDownViewModel2.CanShowSwitchHint())
			{
				LocalSettings.Settings.ExtraSettings.SetHasFlag(HasSetFlag.CountdownFormatHint);
				SwitchFormatHint.Visibility = Visibility.Visible;
			}
		}
		try
		{
			TryDownloadImage();
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	private void TryDownloadImage()
	{
		if (ViewModel?.Model?.BackgroundObject == null || (!(ViewModel.StyleType == "fullscreen_image") && !(ViewModel.StyleType == "polaroid")))
		{
			return;
		}
		string id = ViewModel.Model.BackgroundObject.Id;
		if (id.StartsWith("preset") || !string.IsNullOrEmpty(ViewModel.CustomImagePath))
		{
			return;
		}
		string countdownDir = AppPaths.CountdownDir;
		AttachmentDownloadUtils.DownloadCountdownFile(ViewModel.Model.Id, id, countdownDir, delegate(bool success, string filePath)
		{
			if (success)
			{
				ViewModel.CustomImagePath = filePath;
				CountdownDao.UpdateLocalImagePath(ViewModel.Model.Id, filePath);
			}
		});
	}

	private void CountDownCard_Loaded(object sender, RoutedEventArgs e)
	{
		_lastHeight = base.ActualHeight;
	}

	private void CountDownCard_Unloaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			CountDownViewModel viewModel = ViewModel;
			viewModel.OnShowRemarkDetail = (Action)Delegate.Remove(viewModel.OnShowRemarkDetail, new Action(ShowRemarkWindow));
		}
	}

	private void CountDownCard_LayoutUpdated(object sender, EventArgs e)
	{
		if (base.ActualHeight > 0.0 && Math.Abs(base.ActualHeight - _lastHeight) > 1.0)
		{
			_lastHeight = base.ActualHeight;
			CountDownViewModel countDownViewModel = (CountDownViewModel)base.DataContext;
			if (countDownViewModel != null && !countDownViewModel.IgnoreRatio)
			{
				countDownViewModel.ScaleRatio = base.ActualHeight / 191.0;
			}
		}
	}

	private void Right_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel.ShowActionButton)
		{
			ShowContextMenu(RightMorePopup);
		}
	}

	private void MoreButton_Click(object sender, RoutedEventArgs e)
	{
		ShowContextMenu(MorePopup);
	}

	private void ShowContextMenu(Popup popupControl)
	{
		CountDownViewModel viewModel = ViewModel;
		bool flag = viewModel != null && viewModel.Model?.Status == 1;
		bool flag2 = true;
		if (flag && string.IsNullOrEmpty(ViewModel?.Model?.Remark))
		{
			flag2 = false;
		}
		CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
		{
			(!flag) ? new CustomMenuItemViewModel("Edit", Utils.GetString("Edit"), Utils.GetGeometry("Ic.TaskOM.Edit")) : null,
			(!flag) ? new CustomMenuItemViewModel("Theme", Utils.GetString("Style"), Utils.GetGeometry("Ic.Countdown.Appearance")) : null,
			flag2 ? new CustomMenuItemViewModel("Remark", Utils.GetString("RemarkNotes"), Utils.GetGeometry("Ic.CalendarType.Note")) : null,
			new CustomMenuItemViewModel("Archive", Utils.GetString(flag ? "Restore" : "Archive"), Utils.GetGeometry(flag ? "Ic.Habit.Unarchive" : "Ic.Habit.Archive")),
			new CustomMenuItemViewModel("Delete", Utils.GetString("Delete"), Utils.GetGeometry("Ic.ProjectList.Delete"))
			{
				IsDelete = true
			}
		}.Where((CustomMenuItemViewModel item) => item != null).ToList(), popupControl);
		customMenuList.Operated += OnMoreItemSelected;
		customMenuList.Show();
	}

	private async void OnMoreItemSelected(object sender, object e)
	{
		switch (e as string)
		{
		case "Edit":
			EditCountdown();
			break;
		case "Theme":
			SelectBackground();
			break;
		case "Remark":
			ShowRemarkWindow();
			break;
		case "Archive":
		{
			bool isArchived = ViewModel.Model.Status == 1;
			if (isArchived)
			{
				long limit = LimitCache.GetLimitByKey(Constants.LimitKind.CountdownNumber);
				List<CountdownModel> list = await CountdownDao.GetActiveCountdowns();
				if (!UserDao.IsPro())
				{
					if (list.Count >= limit)
					{
						ProChecker.ShowUpgradeDialog(Utils.IsDida() ? ProType.MoreAnniversaries : ProType.MoreCountdowns, Window.GetWindow(this));
						break;
					}
				}
				else if (list.Count >= limit)
				{
					new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString(Utils.IsDida() ? "ProAnniversaryLimit" : "ProCountdownLimit"), limit), MessageBoxButton.OK, Utils.GetToastWindow() as Window).ShowDialog();
					break;
				}
			}
			await CountdownDao.ChangeCountdownArchiveStatus(ViewModel.Model.Id, !isArchived);
			Utils.Toast(Utils.GetString((!isArchived) ? "Archived" : "Recovered"));
			break;
		}
		case "Delete":
			if (new CustomerDialog(Utils.GetString("Delete"), Utils.GetString(Utils.IsTickPackage() ? "confirm_deletion_the_countdown_title" : "confirm_deletion_the_anniversary_title"), Utils.GetString("Delete"), Utils.GetString("Cancel"), Window.GetWindow(this)).ShowDialog() == true && ViewModel.Model != null)
			{
				await CountdownDao.DeleteCountdown(ViewModel.Model.Id);
			}
			break;
		}
	}

	private void ShowRemarkWindow()
	{
		Window parentWindow = Utils.GetParentWindow(this);
		parentWindow?.Activate();
		CountdownRemarkWindow countdownRemarkWindow = new CountdownRemarkWindow(ViewModel);
		countdownRemarkWindow.Owner = parentWindow;
		countdownRemarkWindow.ShowDialog();
	}

	private void SelectBackground()
	{
		if (ViewModel?.Model == null)
		{
			return;
		}
		CountdownStyleDialog obj = new CountdownStyleDialog(ViewModel.Model)
		{
			Owner = Window.GetWindow(this)
		};
		string countdownId = ViewModel.Model.Id;
		obj.StyleSelected += async delegate(object sender, StyleSelectionResult result)
		{
			if (result != null)
			{
				await CountdownDao.UpdateCountdownBackground(countdownId, result);
				await Task.Delay(50);
				CountdownModel model = await CountdownDao.GetCountdownById(countdownId);
				await (MainCountDownViewModel?.UpdateDisplayItem(model));
			}
		};
		obj.Show();
	}

	private void EditCountdown()
	{
		if (ViewModel?.Model == null)
		{
			return;
		}
		EditCountDownDialog editDialog = new EditCountDownDialog(ViewModel.Model, ViewModel.Model.Type)
		{
			Owner = App.Window
		};
		editDialog.Closed += async delegate
		{
			bool num = editDialog.ChangedType.HasValue && MainCountDownViewModel.IsShowGroup && editDialog.ChangedType != MainCountDownViewModel.lastGroupType && MainCountDownViewModel.lastGroupType != CountDownType.All;
			if (editDialog.Confirm)
			{
				DataChangedNotifier.NotifyCountdownsChanged(new CountdownNotifier
				{
					Model = editDialog.FinalModel,
					Type = CountdownNotifyType.Update
				});
			}
			App.Window.Activate();
			if (num)
			{
				App.Window.Toast(new MoveToastControl(string.Format(Utils.GetString("MovedTo"), CountdownModel.GetTypeText(editDialog.ChangedType.Value)), delegate
				{
					App.Window.CountDownContainer?.OnShowGroupTypeRefresh(editDialog.ChangedType.Value);
				}));
			}
		};
		editDialog.ShowDialog();
	}

	private async void PinButton_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel?.Model != null)
		{
			bool newPinState = !ViewModel.IsPinned;
			await CountdownDao.ChangeCountdownPinStatus(ViewModel.Model.Id, newPinState);
			Utils.Toast(Utils.GetString(newPinState ? "Pinned" : "Unpinned"));
		}
	}

	private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		SwitchFormatHint.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdowncard.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((CountDownCard)target).MouseRightButtonUp += Right_Click;
			break;
		case 2:
			MainBorder = (Border)target;
			break;
		case 3:
			((Grid)target).PreviewMouseLeftButtonDown += Grid_PreviewMouseLeftButtonDown;
			break;
		case 4:
			TemplateControl = (ContentControl)target;
			break;
		case 5:
			SwitchFormatHint = (ArrowHintUserControl)target;
			break;
		case 6:
			PinButton2Contianer = (Grid)target;
			break;
		case 7:
			PinButton2 = (Button)target;
			break;
		case 8:
			PinPath2 = (Path)target;
			break;
		case 9:
			PinButton = (Button)target;
			PinButton.Click += PinButton_Click;
			break;
		case 10:
			PinPath = (Path)target;
			break;
		case 11:
			MoreButton2 = (Button)target;
			break;
		case 12:
			MoreButton = (Button)target;
			MoreButton.Click += MoreButton_Click;
			break;
		case 13:
			MorePopup = (EscPopup)target;
			break;
		case 14:
			RightMorePopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
