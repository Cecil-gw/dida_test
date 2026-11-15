using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Project;

public class TransferProjectWindow : Window, IComponentConnector
{
	private readonly string _projectId;

	private string _selectedUserCode;

	private string _selectedUserName;

	private int _userCount;

	internal TextBlock ComboHint;

	internal ComboBox AvatarComboBox;

	internal Button SaveButton;

	private bool _contentLoaded;

	public TransferProjectWindow(string projectId)
	{
		InitializeComponent();
		_projectId = projectId;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (await TransferProject())
		{
			base.DialogResult = true;
			Close();
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		InitData();
	}

	private async Task<bool> TransferProject()
	{
		if (!string.IsNullOrEmpty(_selectedUserCode))
		{
			ErrorModel errorModel = await Communicator.TransferProject(_projectId, _selectedUserCode);
			if (errorModel != null)
			{
				string content = string.Empty;
				switch (errorModel.errorCode)
				{
				case "no_project_permission":
				case "no_share_permission":
					content = string.Format(Utils.GetString("TransferCannotFound"), _selectedUserName);
					break;
				case "not_project_owner":
					content = Utils.GetString("TransferNotOwner");
					break;
				case "user_not_exist":
					content = string.Format(Utils.GetString("TransferCannotFound"), _selectedUserName);
					break;
				case "project_transfer":
					content = string.Format(Utils.GetString("TransferNotProAccount"), _selectedUserName, _userCount);
					break;
				case "exceed_team_to_pro_max_share_limit":
					content = string.Format(Utils.GetString("TransferNotTeamAccount"), _selectedUserName);
					break;
				}
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("TransferFailed"), content, Utils.GetString("GotIt"), string.Empty);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
				return false;
			}
		}
		return true;
	}

	private async void InitData()
	{
		List<ShareUserModel> list = await AvatarHelper.GetProjectUsersAsync(_projectId, ownerTop: true);
		List<AvatarComboBoxViewModel> list2 = new List<AvatarComboBoxViewModel>();
		if (list != null && list.Count != 0)
		{
			list2.AddRange(from item in list
				where item.isAccept && item.userId != Utils.GetCurrentUserIdInt() && !item.deleted && !item.visitor
				select new AvatarComboBoxViewModel
				{
					AvatarUrl = item.avatarUrl,
					Name = GetDisplayName(item),
					UserId = item.userId.ToString(),
					UserCode = item.userCode
				});
			_userCount = list.Count;
		}
		AvatarComboBox.ItemsSource = list2;
		AvatarComboBox.SelectionChanged += OnSelectionChanged;
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ComboHint.Visibility = Visibility.Collapsed;
		SaveButton.IsEnabled = true;
		AvatarComboBoxViewModel avatarComboBoxViewModel = (AvatarComboBoxViewModel)AvatarComboBox.SelectedItem;
		_selectedUserCode = avatarComboBoxViewModel.UserCode;
		_selectedUserName = avatarComboBoxViewModel.Name;
	}

	private static string GetDisplayName(ShareUserModel item)
	{
		if (!string.IsNullOrEmpty(item.displayName))
		{
			return item.displayName;
		}
		return item.username;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/project/transferprojectwindow.xaml", UriKind.Relative);
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
			((TransferProjectWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			ComboHint = (TextBlock)target;
			break;
		case 3:
			AvatarComboBox = (ComboBox)target;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 5:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
