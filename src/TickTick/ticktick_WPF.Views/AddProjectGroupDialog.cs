using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
using Emoji.Wpf;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Widget;

namespace ticktick_WPF.Views;

public class AddProjectGroupDialog : Window, IOkCancelWindow, IComponentConnector
{
	private ProjectGroupModel _originalProjectGroupModel;

	private string _teamId;

	private readonly bool _isAdd;

	internal AddProjectGroupDialog Root;

	internal Grid EmojiSelectGrid;

	internal Grid ProjectPathGrid;

	internal Path ProjectPath;

	internal Path SetEmojiIcon;

	internal EmjTextBlock EmojiText;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal EmojiEditor FolderNameTextBox;

	internal TextBlock FolderNameRepeatTextBlock;

	private bool _contentLoaded;

	public event EventHandler<ProjectGroupModel> ProjectGroupEdit;

	public event EventHandler Cancel;

	private AddProjectGroupDialog()
	{
		InitializeComponent();
	}

	public AddProjectGroupDialog(string id, string teamId = "")
	{
		InitializeComponent();
		_isAdd = true;
		_teamId = teamId;
		_originalProjectGroupModel = new ProjectGroupModel
		{
			sortType = Constants.SortType.project.ToString(),
			SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.project, inKanban: false)
		};
		CheckEmojiStart(_originalProjectGroupModel);
		Root.DataContext = _originalProjectGroupModel;
		base.Title = Utils.GetString("AddFolder");
	}

	public AddProjectGroupDialog(ProjectGroupModel model)
	{
		InitializeComponent();
		_isAdd = false;
		if (model != null)
		{
			_originalProjectGroupModel = model.Clone();
			CheckEmojiStart(_originalProjectGroupModel);
			Root.DataContext = _originalProjectGroupModel;
			base.Title = Utils.GetString("EditFolder");
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.Owner?.Activate();
		base.OnClosing(e);
	}

	protected override void OnClosed(EventArgs e)
	{
		this.Cancel?.Invoke(null, null);
		base.OnClosed(e);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	protected override void OnContentRendered(EventArgs e)
	{
		base.OnContentRendered(e);
		Keyboard.Focus(FolderNameTextBox);
		FolderNameTextBox.FocusEnd();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.Topmost = false;
		base.OnDeactivated(e);
	}

	protected override async void OnActivated(EventArgs e)
	{
		Keyboard.Focus(FolderNameTextBox);
		FolderNameTextBox.FocusEnd();
		base.Topmost = true;
		await Task.Delay(1);
		base.Topmost = false;
		base.OnActivated(e);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		_originalProjectGroupModel.name = FolderNameTextBox.Text;
		SaveProjectGroup();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnFolderNameTextBoxKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			_originalProjectGroupModel.name = FolderNameTextBox.Text;
			SaveProjectGroup();
		}
	}

	private async void SaveProjectGroup()
	{
		if (!string.IsNullOrEmpty(_originalProjectGroupModel.name))
		{
			_originalProjectGroupModel.name = EmojiText.Text.Trim() + _originalProjectGroupModel.name;
			FolderNameRepeatTextBlock.Visibility = Visibility.Collapsed;
			if (_isAdd)
			{
				_originalProjectGroupModel.id = Utils.GetGuid();
				_originalProjectGroupModel.userId = Utils.GetCurrentUserIdInt();
				_originalProjectGroupModel.sync_status = Constants.SyncStatus.SYNC_NEW.ToString();
				_originalProjectGroupModel.teamId = _teamId;
			}
			await ProjectGroupDao.TrySaveProjectGroup(_originalProjectGroupModel);
			SyncManager.Sync();
			ProjectWidgetsHelper.OnProjectChanged(new GroupProjectIdentity(_originalProjectGroupModel, null));
			this.ProjectGroupEdit?.Invoke(this, _originalProjectGroupModel);
			Close();
		}
		else
		{
			FolderNameRepeatTextBlock.Visibility = Visibility.Visible;
		}
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		_originalProjectGroupModel.name = FolderNameTextBox.Text;
		SaveProjectGroup();
	}

	private void CheckEmojiStart(ProjectGroupModel model)
	{
		string emojiIcon = EmojiHelper.GetEmojiIcon(model.name);
		if (!string.IsNullOrEmpty(emojiIcon) && model.name.StartsWith(emojiIcon))
		{
			emojiIcon = EmojiHelper.GetEmojiIcon(model.name);
			model.name = model.name.Remove(0, emojiIcon.Length);
			SetProjectEmoji(emojiIcon);
		}
	}

	private void ShowEmojiSelector(object sender, MouseButtonEventArgs e)
	{
		EmojiSelector.GetItems();
		EmojiSelector.SetCanReset(!string.IsNullOrEmpty(EmojiText.Text));
		EmojiSelectPopup.IsOpen = true;
	}

	private void SetProjectEmoji(string emoji)
	{
		EmojiText.Text = emoji ?? string.Empty;
		ProjectPathGrid.Visibility = ((!string.IsNullOrEmpty(emoji)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnEmojiSelected(string emoji, bool closePopup)
	{
		SetProjectEmoji(emoji);
		if (closePopup)
		{
			EmojiSelectPopup.IsOpen = false;
		}
	}

	private void OnTextEnterUp(object sender, EventArgs e)
	{
		Ok();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/addprojectgroupdialog.xaml", UriKind.Relative);
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
			Root = (AddProjectGroupDialog)target;
			break;
		case 2:
			EmojiSelectGrid = (Grid)target;
			EmojiSelectGrid.MouseLeftButtonUp += ShowEmojiSelector;
			break;
		case 3:
			ProjectPathGrid = (Grid)target;
			break;
		case 4:
			ProjectPath = (Path)target;
			break;
		case 5:
			SetEmojiIcon = (Path)target;
			break;
		case 6:
			EmojiText = (EmjTextBlock)target;
			break;
		case 7:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 8:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 9:
			FolderNameTextBox = (EmojiEditor)target;
			break;
		case 10:
			FolderNameRepeatTextBlock = (TextBlock)target;
			break;
		case 11:
			((Button)target).Click += OnSaveClick;
			break;
		case 12:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
