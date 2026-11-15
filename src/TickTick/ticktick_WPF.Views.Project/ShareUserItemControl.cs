using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Project;

public class ShareUserItemControl : UserControl, IComponentConnector
{
	internal TextBlock DisplayName;

	internal TextBlock UserName;

	internal TextBlock OwnerOrMyPermission;

	internal ProjectPermissionSetControl OptionControl;

	internal CheckBox OpenCheckBox;

	private bool _contentLoaded;

	public ShareUserItemControl()
	{
		InitializeComponent();
		InitEvent();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (!(base.DataContext is ShareUserViewMode shareUserViewMode))
		{
			return;
		}
		if (shareUserViewMode.isTeam)
		{
			OptionControl.SetTeamItem();
			OpenCheckBox.IsChecked = shareUserViewMode.openToTeam;
			OpenCheckBox.Visibility = Visibility.Visible;
			OpenCheckBox.Checked += OnOpenCheckChanged;
			OpenCheckBox.Unchecked += OnOpenCheckChanged;
			base.IsEnabled = shareUserViewMode.editable;
			PropertyChangedEventManager.AddHandler(shareUserViewMode, OnEnabledChanged, "editable");
			PropertyChangedEventManager.AddHandler(shareUserViewMode, OnPermitChanged, "permission");
			return;
		}
		if (string.IsNullOrEmpty(shareUserViewMode.displayName))
		{
			DisplayName.Text = shareUserViewMode.username;
			UserName.Visibility = Visibility.Collapsed;
		}
		if (!shareUserViewMode.isAccept)
		{
			OptionControl.UnableChangePermission();
		}
		if (shareUserViewMode.deleted)
		{
			UserName.Visibility = Visibility.Collapsed;
		}
		if (shareUserViewMode.isOwner)
		{
			if (shareUserViewMode.userId.ToString() == LocalSettings.Settings.LoginUserId)
			{
				if (shareUserViewMode.userCount <= 1 || shareUserViewMode.noMembers)
				{
					OptionControl.Visibility = Visibility.Collapsed;
				}
				else
				{
					OptionControl.ShowTransfer();
				}
			}
			else
			{
				OptionControl.Visibility = Visibility.Collapsed;
			}
			OwnerOrMyPermission.Visibility = Visibility.Visible;
		}
		else if (shareUserViewMode.userId.ToString() == LocalSettings.Settings.LoginUserId)
		{
			if (!shareUserViewMode.openToTeam || shareUserViewMode.visitor)
			{
				OptionControl.ShowExit();
			}
			else
			{
				OptionControl.Visibility = Visibility.Collapsed;
			}
			OwnerOrMyPermission.Visibility = Visibility.Visible;
			string text = Utils.GetString("Editable");
			string permission = shareUserViewMode.permission;
			if (!(permission == "read"))
			{
				if (permission == "comment")
				{
					text = Utils.GetString("CanComment");
				}
			}
			else
			{
				text = Utils.GetString("ReadOnly");
			}
			OwnerOrMyPermission.Text = text;
			if (!shareUserViewMode.editable)
			{
				OptionControl.Width = 24.0;
				OptionControl.Height = 28.0;
				OptionControl.BorderThickness = new Thickness(0.0);
			}
		}
		else if (!shareUserViewMode.editable)
		{
			OptionControl.Visibility = Visibility.Collapsed;
		}
		else
		{
			Enum.TryParse<Constants.ProjectPermission>(shareUserViewMode.permission, ignoreCase: true, out var result);
			OptionControl.SwitchPermission((int)result);
		}
	}

	private void OnEnabledChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is ShareUserViewMode shareUserViewMode)
		{
			base.IsEnabled = shareUserViewMode.editable;
		}
	}

	private void OnPermitChanged(object sender, PropertyChangedEventArgs e)
	{
		if (base.DataContext is ShareUserViewMode { isTeam: not false } shareUserViewMode)
		{
			Enum.TryParse<Constants.ProjectPermission>(shareUserViewMode.permission, ignoreCase: true, out var result);
			OptionControl.SwitchPermission((int)result);
		}
	}

	private void OnOpenCheckChanged(object sender, RoutedEventArgs e)
	{
		Utils.FindParent<ShareProjectDialog>(this)?.OpenToTeamChanged(OpenCheckBox.IsChecked == true);
	}

	private void InitEvent()
	{
		ProjectPermissionSetControl optionControl = OptionControl;
		optionControl.Delete = (EventHandler)Delegate.Combine(optionControl.Delete, new EventHandler(DeleteUser));
		ProjectPermissionSetControl optionControl2 = OptionControl;
		optionControl2.Exit = (EventHandler)Delegate.Combine(optionControl2.Exit, new EventHandler(ExitClick));
		ProjectPermissionSetControl optionControl3 = OptionControl;
		optionControl3.Transfer = (EventHandler)Delegate.Combine(optionControl3.Transfer, new EventHandler(TransferClick));
		ProjectPermissionSetControl optionControl4 = OptionControl;
		optionControl4.PermissionSelect = (EventHandler<int>)Delegate.Combine(optionControl4.PermissionSelect, new EventHandler<int>(UserPermissionSet));
	}

	private void UserPermissionSet(object sender, int e)
	{
		if (base.DataContext is ShareUserViewMode mode)
		{
			Utils.FindParent<ShareProjectDialog>(this)?.SetUserPermission(mode, e);
		}
	}

	private void TransferClick(object sender, EventArgs e)
	{
		Utils.FindParent<ShareProjectDialog>(this)?.TransferProjectOwner();
	}

	private void DeleteUser(object sender, EventArgs e)
	{
		Utils.FindParent<ShareProjectDialog>(this)?.DeleteUser(base.DataContext, null);
	}

	private void ExitClick(object sender, EventArgs e)
	{
		Utils.FindParent<ShareProjectDialog>(this)?.ExitProject(sender, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/shareuseritemcontrol.xaml", UriKind.Relative);
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
			((ShareUserItemControl)target).Loaded += OnLoaded;
			break;
		case 2:
			DisplayName = (TextBlock)target;
			break;
		case 3:
			UserName = (TextBlock)target;
			break;
		case 4:
			OwnerOrMyPermission = (TextBlock)target;
			break;
		case 5:
			OptionControl = (ProjectPermissionSetControl)target;
			break;
		case 6:
			OpenCheckBox = (CheckBox)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
