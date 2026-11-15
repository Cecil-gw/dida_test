using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views;

public class SetAssigneeDialog : UserControl, ITabControl, IComponentConnector
{
	private readonly Popup _popup;

	private string _projectId;

	private string _assignee;

	internal ContentControl Container;

	private bool _contentLoaded;

	public event EventHandler<AvatarInfo> AssigneeSelect;

	public event EventHandler Closed;

	public Popup GetPopup()
	{
		return _popup;
	}

	public SetAssigneeDialog(string projectId, Popup popup = null, string assignee = null)
	{
		_projectId = projectId;
		_assignee = assignee;
		if (popup != null)
		{
			_popup = popup;
		}
		else
		{
			_popup = new EscPopup
			{
				StaysOpen = false,
				Placement = PlacementMode.MousePoint,
				VerticalOffset = 0.0,
				HorizontalOffset = -120.0
			};
		}
		_popup.Closed -= OnClosed;
		_popup.Closed += OnClosed;
		InitializeComponent();
	}

	private void OnClosed(object sender, EventArgs e)
	{
		this.Closed?.Invoke(this, null);
	}

	private void OnInitialized(object sender, EventArgs e)
	{
		SetItems(_projectId, _assignee);
	}

	public async void SetItems(string projectId, string assignee = null)
	{
		_projectId = projectId;
		List<AvatarViewModel> obj = await AvatarHelper.GetProjectAvatars(_projectId);
		obj.Insert(0, new AvatarViewModel
		{
			UserId = "-1",
			Name = Utils.GetString("NoAvatar"),
			IsNoAvatar = true
		});
		AssignInputItems assignInputItems = new AssignInputItems(obj, selectFirst: false, assignee);
		assignInputItems.OnItemSelected -= OnAssignSelect;
		assignInputItems.OnItemSelected += OnAssignSelect;
		Container.Content = assignInputItems;
	}

	private void OnAssignSelect(object sender, InputItemViewModel<AvatarViewModel> selected)
	{
		this.AssigneeSelect?.Invoke(this, new AvatarInfo(selected.Value, selected.ImageUrl));
		_popup.IsOpen = false;
	}

	public void Show()
	{
		_popup.Child = this;
		_popup.IsOpen = true;
	}

	public void Move(bool forward)
	{
		if (Container.Content is AssignInputItems assignInputItems)
		{
			assignInputItems.Move(forward);
		}
	}

	public void EnterSelect()
	{
		if (Container.Content is AssignInputItems assignInputItems)
		{
			assignInputItems.TrySelectItem();
		}
	}

	public bool HandleTab(bool shift)
	{
		Move(shift);
		return true;
	}

	public bool HandleEnter()
	{
		EnterSelect();
		return true;
	}

	public bool HandleEsc()
	{
		return false;
	}

	public bool UpDownSelect(bool isUp)
	{
		Move(isUp);
		return true;
	}

	public bool LeftRightSelect(bool isLeft)
	{
		return false;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/setassigneedialog.xaml", UriKind.Relative);
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
			((SetAssigneeDialog)target).Initialized += OnInitialized;
			break;
		case 2:
			Container = (ContentControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
