using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.MainListView;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Setting;

public class SmartListConfig : UserControl, IComponentConnector
{
	internal ItemsControl SmartListView;

	internal ItemsControl ExtraView;

	internal ItemsControl BottomView;

	private bool _contentLoaded;

	public SmartListConfig()
	{
		InitializeComponent();
		InitData();
	}

	private void InitData()
	{
		ObservableCollection<SmartListViewModel> itemsSource = new ObservableCollection<SmartListViewModel>
		{
			new SmartListViewModel(SmartListType.All, withAuto: false),
			new SmartListViewModel(SmartListType.Today),
			new SmartListViewModel(SmartListType.Tomorrow),
			new SmartListViewModel(SmartListType.Week),
			new SmartListViewModel(SmartListType.Assign),
			new SmartListViewModel(SmartListType.Inbox),
			new SmartListViewModel(SmartListType.Summary, withAuto: false)
		};
		SmartListView.ItemsSource = itemsSource;
		ObservableCollection<SmartListViewModel> itemsSource2 = new ObservableCollection<SmartListViewModel>
		{
			new SmartListViewModel(SmartListType.Tag),
			new SmartListViewModel(SmartListType.Filter, withAuto: false)
		};
		ExtraView.ItemsSource = itemsSource2;
		ObservableCollection<SmartListViewModel> itemsSource3 = new ObservableCollection<SmartListViewModel>
		{
			new SmartListViewModel(SmartListType.Completed, withAuto: false),
			new SmartListViewModel(SmartListType.Abandoned),
			new SmartListViewModel(SmartListType.Trash, withAuto: false)
		};
		BottomView.ItemsSource = itemsSource3;
	}

	private async void OnItemSelectionChanged(object sender, SimpleComboBoxViewModel e)
	{
		if (!(sender is CustomSimpleComboBox customSimpleComboBox))
		{
			return;
		}
		bool flag = customSimpleComboBox.ItemsSource.Count == 3;
		ShowStatus status = ((customSimpleComboBox.SelectedIndex != 0) ? ((customSimpleComboBox.SelectedIndex != 1) ? ShowStatus.Hide : ((!flag) ? ShowStatus.Hide : ShowStatus.Auto)) : ShowStatus.Show);
		if (!(customSimpleComboBox.Tag is SmartListType type))
		{
			return;
		}
		if (type == SmartListType.Inbox && status == ShowStatus.Hide && LocalSettings.Settings.SmartListInbox != 1 && TaskDefaultDao.GetDefaultSafely().ProjectId == LocalSettings.Settings.InServerBoxId)
		{
			ModifyDefaultProjectDialog modifyDefaultProjectDialog = new ModifyDefaultProjectDialog();
			modifyDefaultProjectDialog.Owner = Window.GetWindow(this);
			modifyDefaultProjectDialog.ShowDialog();
			if (TaskDefaultDao.GetDefaultSafely().ProjectId == LocalSettings.Settings.InServerBoxId)
			{
				customSimpleComboBox.SelectedIndex = LocalSettings.Settings.SmartListInbox;
				return;
			}
			Utils.FindParent<MoreSettingsConfig>(this)?.ResetTaskDefault();
		}
		await TryToast(type, status);
		switch (type)
		{
		case SmartListType.All:
			LocalSettings.Settings.SmartListAll = (int)status;
			break;
		case SmartListType.Today:
			LocalSettings.Settings.SmartListToday = (int)status;
			break;
		case SmartListType.Tomorrow:
			LocalSettings.Settings.SmartListTomorrow = (int)status;
			break;
		case SmartListType.Week:
			LocalSettings.Settings.SmartList7Day = (int)status;
			break;
		case SmartListType.Assign:
			LocalSettings.Settings.SmartListForMe = (int)status;
			break;
		case SmartListType.Completed:
			LocalSettings.Settings.SmartListComplete = (int)status;
			break;
		case SmartListType.Abandoned:
			LocalSettings.Settings.SmartListAbandoned = (int)status;
			break;
		case SmartListType.Trash:
			LocalSettings.Settings.SmartListTrash = (int)status;
			break;
		case SmartListType.Summary:
			LocalSettings.Settings.SmartListSummary = (int)status;
			break;
		case SmartListType.Inbox:
			LocalSettings.Settings.SmartListInbox = (int)status;
			break;
		case SmartListType.Filter:
			LocalSettings.Settings.ShowCustomSmartList = (int)status;
			ListViewContainer.ReloadProjectData();
			if (LocalSettings.Settings.ShowCustomSmartList == 1)
			{
				ListViewContainer.OnProjectHide(PtfType.Filter);
			}
			return;
		case SmartListType.Tag:
			LocalSettings.Settings.SmartListTag = (int)status;
			ListViewContainer.ReloadProjectData();
			if (LocalSettings.Settings.SmartListTag == 1)
			{
				ListViewContainer.OnProjectHide(PtfType.Tag);
			}
			return;
		}
		if (status == ShowStatus.Hide)
		{
			ListViewContainer.OnProjectHide(type);
		}
		ListViewContainer.ReloadProjectData();
	}

	private async Task TryToast(SmartListType type, ShowStatus status)
	{
		string name = null;
		bool flag = false;
		switch (type)
		{
		case SmartListType.All:
			name = Utils.GetString("All");
			break;
		case SmartListType.Today:
			name = Utils.GetString("Today");
			flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_today")) > 0;
			break;
		case SmartListType.Tomorrow:
			name = Utils.GetString("Tomorrow");
			flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_tomorrow")) > 0;
			break;
		case SmartListType.Week:
			name = Utils.GetString("Next7Day");
			flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_week")) > 0;
			break;
		case SmartListType.Assign:
			name = Utils.GetString("AssignToMe");
			flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateSmartIdentity("_special_id_assigned")) > 0;
			break;
		case SmartListType.Completed:
			name = Utils.GetString("Completed");
			break;
		case SmartListType.Abandoned:
			name = Utils.GetString("Abandoned");
			flag = TaskCache.GetAllTask().Any((TaskBaseViewModel t) => t.Status == -1 && t.Deleted == 0);
			break;
		case SmartListType.Trash:
			name = Utils.GetString("Trash");
			break;
		case SmartListType.Summary:
			name = Utils.GetString("Summary");
			break;
		case SmartListType.Inbox:
			name = Utils.GetString("Inbox");
			flag = await TaskCountCache.TryGetCount(ProjectIdentity.CreateInboxProject()) > 0;
			break;
		}
		switch (status)
		{
		case ShowStatus.Auto:
			if (!flag)
			{
				Utils.FindParent<SettingDialog>(this)?.Toast(string.Format(Utils.GetString("AutoSmartProjectTip"), name));
			}
			break;
		case ShowStatus.Hide:
			Utils.FindParent<SettingDialog>(this)?.Toast(string.Format(Utils.GetString("HideHint"), name));
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setting/smartlistconfig.xaml", UriKind.Relative);
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
			SmartListView = (ItemsControl)target;
			break;
		case 2:
			ExtraView = (ItemsControl)target;
			break;
		case 3:
			BottomView = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
