using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Team;

public class InviteGroupItem : UserControl, IComponentConnector
{
	private InviteUsersDialog _parent;

	internal Polygon OpenIcon;

	internal StackPanel Container;

	internal Grid AllGrid;

	internal ItemsControl ItemsControl;

	private bool _contentLoaded;

	public InviteGroupItem()
	{
		InitializeComponent();
	}

	private void InviteGroupItem_OnLoaded(object sender, RoutedEventArgs e)
	{
		_parent = Utils.FindParent<InviteUsersDialog>(this);
		_parent.OnSwitch -= Refresh;
		_parent.OnSwitch += Refresh;
	}

	private void OnProjectClick(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is InviteGroupModel inviteGroupModel)
		{
			if (inviteGroupModel.Opened)
			{
				DoubleAnimation doubleAnimation = (DoubleAnimation)FindResource("HideUsers");
				DoubleAnimation animation = (DoubleAnimation)FindResource("Close");
				doubleAnimation.From = ItemsControl.ActualHeight + (double)(AllGrid.IsVisible ? 32 : 0);
				OpenIcon.BeginAnimation(RotateTransform.AngleProperty, animation);
				Container.BeginAnimation(FrameworkElement.HeightProperty, doubleAnimation);
			}
			else
			{
				DoubleAnimation doubleAnimation2 = (DoubleAnimation)FindResource("ShowUsers");
				DoubleAnimation animation2 = (DoubleAnimation)FindResource("Open");
				doubleAnimation2.To = ItemsControl.ActualHeight + (double)(AllGrid.IsVisible ? 32 : 0);
				OpenIcon.BeginAnimation(RotateTransform.AngleProperty, animation2);
				Container.BeginAnimation(FrameworkElement.HeightProperty, doubleAnimation2);
			}
			inviteGroupModel.Opened = !inviteGroupModel.Opened;
		}
	}

	private void Refresh(object sender, EventArgs e)
	{
		if (base.DataContext is InviteGroupModel inviteGroupModel)
		{
			inviteGroupModel.SelectNum = 0;
			inviteGroupModel.Users.ForEach(delegate(InviteUserModel user)
			{
				user.Selected = false;
			});
		}
	}

	private async void OnAllSelected(object sender, MouseButtonEventArgs e)
	{
		if (!(base.DataContext is InviteGroupModel inviteGroupModel))
		{
			return;
		}
		if (inviteGroupModel.SelectNum < inviteGroupModel.Users.Count)
		{
			inviteGroupModel.Users.ForEach(delegate(InviteUserModel model)
			{
				model.Selected = true;
			});
			_parent.OnProjectItemSelected(inviteGroupModel.Users.Select((InviteUserModel user) => user.UserCode).ToList(), selected: true);
		}
		else
		{
			inviteGroupModel.Users.ForEach(delegate(InviteUserModel model)
			{
				model.Selected = false;
			});
			_parent.OnProjectItemSelected(inviteGroupModel.Users.Select((InviteUserModel user) => user.UserCode).ToList(), selected: false);
		}
	}

	public void ItemSelected(string userCode, bool selected)
	{
		_parent?.OnProjectItemSelected(new List<string> { userCode }, selected, clickAll: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/team/invitegroupitem.xaml", UriKind.Relative);
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
			((InviteGroupItem)target).Loaded += InviteGroupItem_OnLoaded;
			break;
		case 2:
			((Grid)target).MouseLeftButtonUp += OnProjectClick;
			break;
		case 3:
			OpenIcon = (Polygon)target;
			break;
		case 4:
			Container = (StackPanel)target;
			break;
		case 5:
			AllGrid = (Grid)target;
			break;
		case 6:
			((Grid)target).MouseLeftButtonUp += OnAllSelected;
			break;
		case 7:
			((Grid)target).MouseLeftButtonUp += OnAllSelected;
			break;
		case 8:
			ItemsControl = (ItemsControl)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
