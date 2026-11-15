using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Team;

public class InviteNormalItem : UserControl, IComponentConnector
{
	private InviteGroupItem _groupParent;

	private InviteUsersDialog _parent;

	internal InviteNormalItem Root;

	internal ColumnDefinition TheThirdColumn;

	private bool _contentLoaded;

	public InviteNormalItem()
	{
		InitializeComponent();
	}

	private void InviteNormalItem_OnLoaded(object sender, RoutedEventArgs e)
	{
		_groupParent = Utils.FindParent<InviteGroupItem>(this);
		_parent = Utils.FindParent<InviteUsersDialog>(this);
	}

	private void OnSelectedItem(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is InviteUserModel inviteUserModel)
		{
			inviteUserModel.Selected = !inviteUserModel.Selected;
			if (_groupParent != null)
			{
				_groupParent.ItemSelected(inviteUserModel.UserCode, inviteUserModel.Selected);
			}
			else
			{
				_parent?.CheckInviteEnable(inviteUserModel.Selected);
			}
		}
	}

	private void OnDeleteClick(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		if (base.DataContext is InviteUserModel model)
		{
			_parent?.DeleteRecentRecord(model);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/team/invitenormalitem.xaml", UriKind.Relative);
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
			Root = (InviteNormalItem)target;
			Root.Loaded += InviteNormalItem_OnLoaded;
			break;
		case 2:
			((Grid)target).PreviewMouseLeftButtonUp += OnSelectedItem;
			break;
		case 3:
			TheThirdColumn = (ColumnDefinition)target;
			break;
		case 4:
			((Image)target).MouseLeftButtonUp += OnDeleteClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
