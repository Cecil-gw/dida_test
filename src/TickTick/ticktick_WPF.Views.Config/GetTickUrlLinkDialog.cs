using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Newtonsoft.Json;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Config;

public class GetTickUrlLinkDialog : Window, IComponentConnector
{
	internal Grid Container;

	internal ScrollViewer Scroller;

	internal ItemsControl FeedCodeItems;

	internal StackPanel AddFeedCodePanel;

	internal EscPopup SelectProjectPopup;

	private bool _contentLoaded;

	public GetTickUrlLinkDialog()
	{
		InitializeComponent();
		LoadProjectCodes();
		base.Title = string.Format(Utils.GetString("SubscribeTickTick"), Utils.GetAppName());
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async Task LoadProjectCodes()
	{
		ObservableCollection<ProjectFeedViewModel> vms = new ObservableCollection<ProjectFeedViewModel>();
		FeedCodeItems.ItemsSource = vms;
		string value = await Communicator.GetAllProjectFeedsCode();
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		try
		{
			List<ProjectFeedCode> list = JsonConvert.DeserializeObject<List<ProjectFeedCode>>(value);
			if (list == null || list.Count <= 0)
			{
				return;
			}
			list.Sort((ProjectFeedCode a, ProjectFeedCode b) => (a.createdTime.HasValue && b.createdTime.HasValue) ? a.createdTime.Value.CompareTo(b.createdTime.Value) : 0);
			foreach (ProjectFeedCode item in list)
			{
				vms.Add(new ProjectFeedViewModel(item));
			}
		}
		catch (Exception)
		{
		}
	}

	internal async void RemoveFeedItem(ProjectFeedViewModel vm)
	{
		IEnumerable itemsSource = FeedCodeItems.ItemsSource;
		if (itemsSource is ObservableCollection<ProjectFeedViewModel> vms && await Communicator.DeleteProjectFeedsCode(vm.ProjectId))
		{
			vms.Remove(vm);
		}
	}

	private void OnAddFeedCodeClick(object sender, MouseButtonEventArgs e)
	{
		ProjectOrGroupPopup projectOrGroupPopup = new ProjectOrGroupPopup(SelectProjectPopup, new ProjectExtra(), new ProjectSelectorExtra
		{
			showAll = false,
			batchMode = false,
			showSmartAll = true,
			canSelectGroup = false,
			onlyShowPermission = false,
			showNoteProject = false,
			showSharedProject = true,
			CanSearch = true
		});
		projectOrGroupPopup.ItemSelect += OnProjectSelect;
		projectOrGroupPopup.Show();
	}

	private async void OnProjectSelect(object sender, SelectableItemViewModel e)
	{
		if (e is ProjectGroupViewModel)
		{
			return;
		}
		SelectProjectPopup.IsOpen = false;
		string projectId = e.Id;
		if (projectId == "_special_id_all")
		{
			projectId = "all";
		}
		if (FeedCodeItems.ItemsSource is ObservableCollection<ProjectFeedViewModel> source && source.Any((ProjectFeedViewModel m) => m.ProjectId == projectId))
		{
			Toast(Utils.GetString("ListUrlExisted"));
		}
		else
		{
			if (new CustomerDialog(Utils.GetString("EnableURL"), Utils.GetString("EnableUrlNotify"), Utils.GetString("GetEnable"), Utils.GetString("Close"), Window.GetWindow(this)).ShowDialog() != true)
			{
				return;
			}
			string text = await Communicator.GetProjectFeedsCode(projectId);
			if (!string.IsNullOrEmpty(text))
			{
				text = text.Replace("\"", "");
				if (FeedCodeItems.ItemsSource is ObservableCollection<ProjectFeedViewModel> observableCollection)
				{
					observableCollection.Add(new ProjectFeedViewModel(new ProjectFeedCode
					{
						projectId = projectId,
						code = text,
						createdTime = DateTime.Now
					}));
					await Task.Delay(50);
					Scroller.ScrollToBottom();
				}
			}
		}
	}

	public void Toast(string text)
	{
		UiUtils.Toast(Container, text);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/gettickurllinkdialog.xaml", UriKind.Relative);
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
			Container = (Grid)target;
			break;
		case 2:
			Scroller = (ScrollViewer)target;
			break;
		case 3:
			FeedCodeItems = (ItemsControl)target;
			break;
		case 4:
			AddFeedCodePanel = (StackPanel)target;
			AddFeedCodePanel.MouseLeftButtonUp += OnAddFeedCodeClick;
			break;
		case 5:
			SelectProjectPopup = (EscPopup)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
