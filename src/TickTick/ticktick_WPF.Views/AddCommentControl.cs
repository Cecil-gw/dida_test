using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Network;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.MainListView.DetailView;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.QuickAdd;

namespace ticktick_WPF.Views;

public class AddCommentControl : System.Windows.Controls.UserControl, IComponentConnector, IStyleConnector
{
	private readonly List<Mention> _selectedMentionList = new List<Mention>();

	private CommentEditViewModel _commentEditViewModel;

	private List<Mention> _mentionList;

	private int _selectedAtLabelIndex = -1;

	private bool _isUploadingImage;

	private TaskDetailView _parent;

	private bool _initialized;

	private bool _isInOperation;

	internal Grid AddCommentRoot;

	internal Grid RecentCommentOverlay;

	internal TextBlock MoreText;

	internal TaskLinkNameEditor ContentTxt;

	internal TextBlock ExpandOrCollapseBtn;

	internal TextBlock CommentHint;

	internal ScrollViewer InputScrollViewer;

	internal EmojiEditor CommentInputTxt;

	internal Border AttachmentBorder;

	internal StackPanel ActionPanel;

	internal Run OverSizeText;

	internal ContentControl EmojiGrid;

	internal EscPopup EmojiSelectPopup;

	internal EmojiSelectControl EmojiSelector;

	internal Popup AtPopup;

	internal ScrollViewer AtPopupScrollViewer;

	internal StackPanel AtPopupStackPanel;

	private bool _contentLoaded;

	public CommentEditViewModel EditModel
	{
		get
		{
			return _commentEditViewModel;
		}
		set
		{
			_commentEditViewModel = value;
			base.DataContext = _commentEditViewModel;
		}
	}

	public event EventHandler CommentGotFocus;

	public event EventHandler<bool> PopupShowing;

	public event EventHandler<CommentModel> OnCommentAdded;

	public AddCommentControl(CommentEditViewModel commentEditViewModel, TaskDetailView parent)
	{
		InitializeComponent();
		_commentEditViewModel = commentEditViewModel;
		base.DataContext = _commentEditViewModel;
		base.Loaded += OnLoaded;
		ContentTxt.EditBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
		_parent = parent;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (!_initialized)
		{
			_initialized = true;
			InitView();
		}
	}

	private async Task InitMentionList(string projectId)
	{
		List<ShareUserModel> shareUsers = await AvatarHelper.GetProjectUsersAsync(projectId);
		UserInfoModel userInfoModel = await UserManager.GetUserInfo();
		_mentionList = new List<Mention>();
		if (shareUsers == null || shareUsers.Count <= 0)
		{
			return;
		}
		foreach (ShareUserModel item in shareUsers)
		{
			if (item.isAccept && item.userCode != userInfoModel?.userCode)
			{
				string atLabel = string.Empty;
				if (!string.IsNullOrEmpty(item.username))
				{
					atLabel = item.username;
				}
				if (!string.IsNullOrEmpty(item.displayName))
				{
					atLabel = item.displayName;
				}
				long num = item.userId ?? (-1);
				if (num != -1)
				{
					_mentionList.Add(new Mention
					{
						userId = num,
						atLabel = atLabel
					});
				}
			}
		}
	}

	private void InitView()
	{
		CommentInputTxt.EditBox.TextArea.TextView.LineSpacing = 2.0;
		CommentInputTxt.TextChanged -= OnCommentTextChanged;
		CommentInputTxt.TextChanged += OnCommentTextChanged;
		CommentInputTxt.GotFocus += OnCommentGotFocus;
		CommentInputTxt.EditBox.TextArea.Caret.PositionChanged -= OnCaretChanged;
		CommentInputTxt.EditBox.TextArea.Caret.PositionChanged += OnCaretChanged;
		CommentInputTxt.EditBox.PreviewKeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.V && Utils.IfCtrlPressed())
			{
				TryPasteAttachment(e);
			}
		};
		CommentInputTxt.TextChanged += delegate(object sender, EventArgs e)
		{
			if (sender != null && sender is EmojiEditor emojiEditor && _commentEditViewModel != null)
			{
				string text = emojiEditor.Text;
				_commentEditViewModel.Content = text;
				if (text.EndsWith("@"))
				{
					TryShowAtDialog();
				}
				else
				{
					CloseAtDialog();
				}
			}
		};
		CommentInputTxt.PreviewKeyDown += delegate(object sender, System.Windows.Input.KeyEventArgs e)
		{
			switch (e.Key)
			{
			case Key.Return:
				if (!AtPopup.IsOpen)
				{
					if (!Utils.IfShiftPressed())
					{
						if (CommentInputTxt.Text.Length <= 1024)
						{
							AddComment();
						}
						e.Handled = true;
					}
				}
				else
				{
					SelectAtLabel();
					e.Handled = true;
				}
				break;
			case Key.Up:
				MoveUp();
				break;
			case Key.Down:
				MoveDown();
				break;
			case Key.Escape:
				if (AtPopup.IsOpen)
				{
					AtPopup.IsOpen = false;
					e.Handled = true;
				}
				break;
			}
		};
		MoreText.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
		{
			_commentEditViewModel.RecentScrollHeight = 0.0;
			(_parent ?? Utils.FindParent<TaskDetailView>(this))?.ExpandCommentAndScrollIntoView();
			e.Handled = true;
		};
	}

	private void OnCaretChanged(object sender, EventArgs e)
	{
		RelocateTitleLocation();
	}

	private void OnCommentTextChanged(object sender, EventArgs e)
	{
		if (_commentEditViewModel != null)
		{
			_commentEditViewModel.Content = CommentInputTxt.Text;
		}
		CommentHint.Visibility = ((!string.IsNullOrEmpty(CommentInputTxt.Text)) ? Visibility.Collapsed : Visibility.Visible);
		SetPanelLocation();
		OverSizeText.SetResourceReference(TextElement.ForegroundProperty, (CommentInputTxt.Text.Length <= 1024) ? "BaseColorOpacity60" : "TextRedColor");
	}

	private void SetPanelLocation()
	{
		if (_commentEditViewModel != null)
		{
			TextViewPosition position = new TextViewPosition(CommentInputTxt.EditBox.Document.GetLocation(CommentInputTxt.EditBox.Text.Length));
			Point visualPosition = CommentInputTxt.EditBox.TextArea.TextView.GetVisualPosition(position, VisualYPosition.LineBottom);
			bool flag = visualPosition.X >= CommentInputTxt.ActualWidth - ActionPanel.ActualWidth - 20.0 || visualPosition.Y > 24.0 || _commentEditViewModel.Files.Count > 0;
			_commentEditViewModel.ActionPanelRow = (flag ? 1 : 0);
		}
	}

	private async void AddComment()
	{
		if ((string.IsNullOrEmpty(CommentInputTxt.Text.Trim()) && _commentEditViewModel.Files.Count <= 0) || _commentEditViewModel.LeftInputCount < 0)
		{
			return;
		}
		string commentTitle = CommentInputTxt.Text;
		string replyCommentId = string.Empty;
		if (!string.IsNullOrEmpty(_commentEditViewModel.ReplyName) && commentTitle.Contains(_commentEditViewModel.ReplyName))
		{
			string replyContent = _commentEditViewModel.GetReplyContent();
			if (commentTitle.Contains(replyContent))
			{
				commentTitle = commentTitle.Substring(_commentEditViewModel.GetReplyContent().Length);
			}
			replyCommentId = _commentEditViewModel.ReplyCommentId;
		}
		string commentId = _commentEditViewModel.CommentId;
		List<AttachmentModel> attachments = await SaveAttachments();
		this.OnCommentAdded?.Invoke(this, new CommentModel
		{
			id = commentId,
			title = commentTitle,
			mentions = _selectedMentionList,
			replyCommentId = replyCommentId,
			projectSid = _commentEditViewModel.ProjectId,
			taskSid = _commentEditViewModel.TaskId,
			attachmentCount = _commentEditViewModel.Files.Count,
			isMySelf = true
		});
		CommentInputTxt.Text = string.Empty;
		_commentEditViewModel.RecentScrollHeight = 0.0;
		_commentEditViewModel.ClearFiles(needDelete: false);
		_commentEditViewModel.CommentId = Utils.GetGuid();
		UploadAttachments(commentId, attachments);
	}

	private async void UploadAttachments(string commentId, List<AttachmentModel> attachments)
	{
		foreach (AttachmentModel attachment in attachments)
		{
			await AttachmentUploadUtils.UploadCommentFile(_commentEditViewModel.ProjectId, _commentEditViewModel.TaskId, attachment);
			await CommentService.AddCommentUploadAttachment(commentId, attachment);
		}
	}

	private void SelectAtLabel()
	{
		if (_selectedAtLabelIndex >= 0 && _selectedAtLabelIndex < AtPopupStackPanel.Children.Count && AtPopupStackPanel.Children[_selectedAtLabelIndex] is System.Windows.Controls.TextBox textBox)
		{
			SelectAtLabel((Mention)textBox.Tag);
		}
	}

	private async void CloseAtDialog()
	{
		if (AtPopup.IsOpen)
		{
			AtPopup.IsOpen = false;
		}
		await Task.Delay(500);
	}

	private Point GetPopupOffset()
	{
		TextView textView = CommentInputTxt.EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(CommentInputTxt.EditBox.Document.GetLocation(CommentInputTxt.EditBox.CaretOffset));
		return textView.GetVisualPosition(position, VisualYPosition.LineBottom);
	}

	private async void TryShowAtDialog()
	{
		if (_mentionList == null || _mentionList.Count == 0)
		{
			await InitMentionList(EditModel.ProjectId);
		}
		if (_mentionList == null || _mentionList.Count <= 0)
		{
			return;
		}
		AtPopupStackPanel.Children.Clear();
		foreach (Mention mention in _mentionList)
		{
			AtPopupStackPanel.Children.Add(AddAtItem(mention));
		}
		AtPopup.PlacementTarget = CommentInputTxt;
		Point popupOffset = GetPopupOffset();
		AtPopup.VerticalOffset = popupOffset.Y + 10.0;
		AtPopup.HorizontalOffset = popupOffset.X;
		if (AtPopupStackPanel.Children.Count != 0)
		{
			if (AtPopupStackPanel.Children[0] is System.Windows.Controls.TextBox textBox)
			{
				textBox.Foreground = ThemeUtil.GetColor("PrimaryColor");
			}
			_selectedAtLabelIndex = 0;
			AtPopupScrollViewer.ScrollToTop();
		}
		AtPopup.IsOpen = true;
	}

	public async void FocusInput()
	{
		if (!CommentInputTxt.IsFocused || CommentInputTxt.Text != EditModel?.Content)
		{
			CommentInputTxt.Text = EditModel?.Content ?? string.Empty;
			await Task.Delay(20);
			CommentInputTxt.Focus();
			CommentInputTxt.EditBox.CaretOffset = CommentInputTxt.Text.Length;
		}
	}

	public void ClearText()
	{
		CommentInputTxt.Text = string.Empty;
	}

	private void MoveDown()
	{
		if (AtPopup.IsOpen && AtPopupStackPanel.Children.Count > 0 && _selectedAtLabelIndex < AtPopupStackPanel.Children.Count - 1)
		{
			SetUnSelected(AtPopupStackPanel.Children[_selectedAtLabelIndex] as System.Windows.Controls.TextBox);
			SetSelected(AtPopupStackPanel.Children[_selectedAtLabelIndex + 1] as System.Windows.Controls.TextBox);
			_selectedAtLabelIndex++;
		}
	}

	private void MoveUp()
	{
		if (AtPopup.IsOpen && AtPopupStackPanel.Children.Count > 0 && _selectedAtLabelIndex > 0)
		{
			SetUnSelected(AtPopupStackPanel.Children[_selectedAtLabelIndex] as System.Windows.Controls.TextBox);
			SetSelected(AtPopupStackPanel.Children[_selectedAtLabelIndex - 1] as System.Windows.Controls.TextBox);
			_selectedAtLabelIndex--;
		}
	}

	private static void SetUnSelected(System.Windows.Controls.TextBox textbox)
	{
		textbox.Background = ThemeUtil.GetColor("WindowBackgroundPrimary");
		textbox.Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80");
	}

	private static void SetSelected(System.Windows.Controls.TextBox textbox)
	{
		textbox.Foreground = ThemeUtil.GetColor("PrimaryColor");
	}

	private System.Windows.Controls.TextBox AddAtItem(Mention mention)
	{
		System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
		textBox.Style = (Style)(System.Windows.Application.Current?.FindResource("TagPopupItemTextBoxStyle"));
		textBox.MouseEnter += delegate(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (AtPopupStackPanel.Children.Count != 0)
			{
				for (int i = 0; i < AtPopupStackPanel.Children.Count; i++)
				{
					if (AtPopupStackPanel.Children[i] is System.Windows.Controls.TextBox textBox2)
					{
						textBox2.Background = ThemeUtil.GetColor("WindowBackgroundPrimary");
						textBox2.Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80");
					}
				}
			}
			if (sender is System.Windows.Controls.TextBox textBox3)
			{
				textBox3.Background = ThemeUtil.GetColor("BaseColorOpacity5");
				textBox3.Foreground = ThemeUtil.GetColor("PrimaryColor");
			}
		};
		textBox.MouseLeave += delegate(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (sender is System.Windows.Controls.TextBox textBox2)
			{
				textBox2.Background = ThemeUtil.GetColor("WindowBackgroundPrimary");
			}
		};
		textBox.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
		{
			if (sender is System.Windows.Controls.TextBox textBox2)
			{
				SelectAtLabel((Mention)textBox2.Tag);
			}
		};
		textBox.Tag = mention;
		textBox.Text = mention.atLabel;
		textBox.Foreground = ThemeUtil.GetColor("BaseColorOpacity100_80");
		return textBox;
	}

	private void SelectAtLabel(Mention selected)
	{
		Mention item = new Mention
		{
			userId = selected.userId,
			atLabel = "@" + selected.atLabel
		};
		_selectedMentionList.Add(item);
		_commentEditViewModel.Content = _commentEditViewModel.Content + selected.atLabel + " ";
		AtPopup.IsOpen = false;
		CommentInputTxt.Text = _commentEditViewModel.Content;
		CommentInputTxt.Focus();
		CommentInputTxt.EditBox.CaretOffset = _commentEditViewModel.Content.Length;
	}

	private void expandOrCollapseBtn_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		ToggleExpandOrCollapse();
	}

	private void ToggleExpandOrCollapse()
	{
		if (ExpandOrCollapseBtn.Text == Utils.GetString("expand"))
		{
			ExpandOrCollapseBtn.Text = Utils.GetString("Collapse");
		}
		else
		{
			ExpandOrCollapseBtn.Text = Utils.GetString("expand");
		}
	}

	private void contentTxt_TextChanged(object sender, TextChangedEventArgs e)
	{
	}

	private void OnCommentGotFocus(object sender, RoutedEventArgs e)
	{
		if (_commentEditViewModel != null)
		{
			_commentEditViewModel.ContentFocused = true;
		}
		this.CommentGotFocus?.Invoke(sender, e);
	}

	private void OnCommentLostFocus(object sender, RoutedEventArgs e)
	{
		if (_commentEditViewModel != null)
		{
			_commentEditViewModel.ContentFocused = false;
		}
	}

	public bool IsEditing()
	{
		if (CommentInputTxt.Text.Length <= 0)
		{
			return _isInOperation;
		}
		return true;
	}

	public void SetModel(CommentEditViewModel editCommentModel)
	{
		_commentEditViewModel = editCommentModel;
		base.DataContext = _commentEditViewModel;
	}

	private async void OnEmojiSelected(string emoji, bool closepopup)
	{
		EmojiSelectPopup.IsOpen = false;
		CommentInputTxt.Text += emoji;
		CommentInputTxt.EditBox.CaretOffset = CommentInputTxt.Text.Length;
		await Task.Delay(200);
		CommentInputTxt.EditBox.Focus();
	}

	private void OnEmojiClick(object sender, MouseButtonEventArgs e)
	{
		EmojiSelector.GetItems();
		EmojiSelector.SetActionPanelEnabled(enabled: false);
		EmojiSelectPopup.IsOpen = true;
	}

	private async void OnImageClick(object sender, MouseButtonEventArgs e)
	{
		if (!Utils.IsNetworkAvailable())
		{
			Utils.Toast(Utils.GetString("NetworkException"));
			return;
		}
		await DoAddAttachment();
		SetPanelLocation();
		await Task.Delay(200);
		CommentInputTxt.EditBox.Focus();
	}

	private async Task DoAddAttachment()
	{
		_parent.SetPopupShow();
		_isInOperation = true;
		this.PopupShowing?.Invoke(this, e: true);
		string filter = Utils.GetString("Image") + "|*.png;*.jepg;*.jpg";
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = UserDao.IsPro(),
			Filter = filter
		};
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			int fileNum = openFileDialog.FileNames.Cast<object>().Count((object item) => File.Exists(item.ToString()));
			if (!(await CheckAttachmentLimit(fileNum)))
			{
				_isInOperation = false;
				return;
			}
			string[] fileNames = openFileDialog.FileNames;
			foreach (string text in fileNames)
			{
				if (!File.Exists(text))
				{
					continue;
				}
				string fileName = text.Split('\\').Last();
				fileName = FileUtils.TrimFileName(fileName, 240 - AppPaths.ImageDir.Length);
				string text2 = AppPaths.ImageDir + fileName;
				if (File.Exists(text2))
				{
					int num2 = fileName.LastIndexOf('.');
					if (num2 < 0)
					{
						num2 = fileName.Length;
					}
					int num3 = 1;
					do
					{
						num3++;
						text2 = AppPaths.ImageDir + fileName.Insert(num2, $"({num3})");
					}
					while (File.Exists(text2));
				}
				File.Copy(text, text2);
				_commentEditViewModel.AddFile(text2);
			}
		}
		this.PopupShowing?.Invoke(this, e: false);
		_isInOperation = false;
	}

	private async Task<List<AttachmentModel>> SaveAttachments()
	{
		string commentId = _commentEditViewModel.CommentId;
		List<AddTaskAttachmentInfo> list = _commentEditViewModel.Files.Where((AddTaskAttachmentInfo it) => !it.Uploading).ToList();
		if (list.Count > 0)
		{
			foreach (AddTaskAttachmentInfo item in list)
			{
				item.Uploading = true;
			}
			AttachmentCache.SetTodayAttachmentCount(AttachmentCache.TodayAttachmentCount() + list.Count);
			return await AttachmentProvider.AddAttachmentModels(commentId, list);
		}
		return new List<AttachmentModel>();
	}

	public async Task<bool> CheckAttachmentLimit(int fileNum)
	{
		if (_commentEditViewModel.Files.Count + fileNum > 9)
		{
			Utils.Toast(string.Format(Utils.GetString("photo_add_limit"), 9));
			return false;
		}
		long taskAttachmentLimitCount = Utils.GetUserLimit(Constants.LimitKind.TaskAttachmentNumber);
		int num = await AttachmentDao.GetTaskAttachmentCount(_commentEditViewModel.TaskId);
		if (_commentEditViewModel.Files.Count + fileNum + num > taskAttachmentLimitCount)
		{
			Utils.Toast(string.Format(Utils.GetString("photo_add_limit"), taskAttachmentLimitCount));
			return false;
		}
		fileNum += _commentEditViewModel.Files.Count;
		int num2 = await ApiClient.GetJsonAsync<int>(BaseUrl.GetUrl("/api/v1/attachment/dailyLimit"));
		Tuple<long, long> residueAttachmentCount = Utils.GetResidueAttachmentCount(fileNum);
		if (num2 < fileNum)
		{
			if (UserDao.IsPro())
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("attachments_upload_limit_dialog_msg"), residueAttachmentCount.Item1, residueAttachmentCount.Item2), MessageBoxButton.OK);
				customerDialog.Owner = Window.GetWindow(this);
				customerDialog.ShowDialog();
			}
			else
			{
				ProChecker.ShowUpgradeDialog(ProType.MoreAttachments);
			}
			return false;
		}
		if (fileNum > taskAttachmentLimitCount)
		{
			CustomerDialog customerDialog2 = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("TaskAttachmentsLimitContent"), MessageBoxButton.OK);
			customerDialog2.Owner = Window.GetWindow(this);
			customerDialog2.ShowDialog();
			return false;
		}
		return true;
	}

	private void OnAttachmentClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddTaskAttachmentInfo dataContext })
		{
			dataContext.OnAttachmentClick();
		}
	}

	private BitmapSource GetPastImage()
	{
		BitmapSource result = null;
		try
		{
			if (string.IsNullOrEmpty(System.Windows.Clipboard.GetText()))
			{
				result = System.Windows.Clipboard.GetImage();
			}
		}
		catch (Exception)
		{
		}
		return result;
	}

	public void TryPasteAttachment(System.Windows.Input.KeyEventArgs e)
	{
		try
		{
			BitmapSource pastImage = GetPastImage();
			if (pastImage != null)
			{
				e.Handled = true;
				TryPasteImage(pastImage);
				return;
			}
			List<string> pastFiles = FileUtils.GetPastFiles();
			if (pastFiles != null && pastFiles.Any())
			{
				pastFiles = pastFiles.Where((string it) => it.EndsWith(".png") || it.EndsWith(".jpg") || it.EndsWith(".jepg") || it.EndsWith(".webp")).ToList();
				if (pastFiles.Count > 0)
				{
					TryPasteFile(pastFiles);
				}
				else
				{
					Utils.Toast(Utils.GetString("OptionNotSupport"));
				}
				e.Handled = true;
			}
		}
		catch (Exception)
		{
			Utils.Toast(Utils.GetString("FileNotFound"));
		}
	}

	private async Task TryPasteImage(BitmapSource image)
	{
		if (image != null && await CheckAttachmentLimit(1))
		{
			string name = Utils.GetGuid() + ".png";
			string path = ImageUtils.SavePasteImage(image, name);
			if (File.Exists(path))
			{
				_commentEditViewModel.AddFile(path);
			}
		}
	}

	private void DeleteFile(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AddTaskAttachmentInfo dataContext } && _commentEditViewModel != null)
		{
			_commentEditViewModel.Files.Remove(dataContext);
		}
	}

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetPanelLocation();
	}

	private void OnTitleMouseScroll(object sender, MouseWheelEventArgs e)
	{
		if (e != null)
		{
			InputScrollViewer.ScrollToVerticalOffset(InputScrollViewer.VerticalOffset - (double)((float)e.Delta / 2f));
		}
	}

	private static List<string> GetDropFiles(System.Windows.DragEventArgs e)
	{
		Regex regex = new Regex(".*\\.(png|jpg|jpeg)$", RegexOptions.Compiled);
		List<string> list = new List<string>();
		if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
		{
			string[] array = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
			if (array != null)
			{
				list.AddRange(array.Where((string item) => regex.IsMatch(item)));
			}
		}
		return list;
	}

	private void RelocateTitleLocation()
	{
		TextView textView = CommentInputTxt.EditBox.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(CommentInputTxt.EditBox.Document.GetLocation(CommentInputTxt.EditBox.CaretOffset));
		Point visualPosition = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		double num = InputScrollViewer.MaxHeight + InputScrollViewer.VerticalOffset;
		int num2 = 16;
		if (visualPosition.Y >= num)
		{
			InputScrollViewer.ScrollToVerticalOffset(visualPosition.Y);
		}
		else if (visualPosition.Y - (double)num2 < InputScrollViewer.VerticalOffset)
		{
			InputScrollViewer.ScrollToVerticalOffset(visualPosition.Y - (double)num2);
		}
	}

	private void OnFileDrop(object sender, System.Windows.DragEventArgs e)
	{
		e.Handled = true;
		TryPasteFile(GetDropFiles(e));
	}

	private async Task TryPasteFile(IReadOnlyCollection<string> files)
	{
		if (files == null || files.Count <= 0 || !(await CheckAttachmentLimit(files.Count)) || files.Count <= 0)
		{
			return;
		}
		foreach (string file in files)
		{
			string fileName = IOUtils.GetFileName(file);
			string path = FileUtils.SavePasteFile(file, fileName);
			try
			{
				if (File.Exists(path))
				{
					_commentEditViewModel.AddFile(path);
				}
			}
			catch (Exception)
			{
				Utils.Toast(Utils.GetString("AddFailed"));
			}
		}
	}

	private void OnEmojiPopupOpen(object sender, EventArgs e)
	{
		Utils.FindParent<TaskDetailView>(this)?.SetPopupStatus(show: true);
	}

	private void OnEmojiPopupClosed(object sender, EventArgs e)
	{
		Utils.FindParent<TaskDetailView>(this)?.SetPopupStatus(show: false);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/addcommentcontrol.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
			((AddCommentControl)target).SizeChanged += OnContentSizeChanged;
			break;
		case 2:
			AddCommentRoot = (Grid)target;
			AddCommentRoot.Drop += OnFileDrop;
			break;
		case 3:
			RecentCommentOverlay = (Grid)target;
			break;
		case 4:
			MoreText = (TextBlock)target;
			break;
		case 5:
			ContentTxt = (TaskLinkNameEditor)target;
			break;
		case 6:
			ExpandOrCollapseBtn = (TextBlock)target;
			ExpandOrCollapseBtn.MouseLeftButtonUp += expandOrCollapseBtn_MouseLeftButtonUp;
			break;
		case 7:
			CommentHint = (TextBlock)target;
			break;
		case 8:
			InputScrollViewer = (ScrollViewer)target;
			InputScrollViewer.PreviewMouseWheel += OnTitleMouseScroll;
			break;
		case 9:
			CommentInputTxt = (EmojiEditor)target;
			break;
		case 10:
			AttachmentBorder = (Border)target;
			break;
		case 13:
			ActionPanel = (StackPanel)target;
			break;
		case 14:
			OverSizeText = (Run)target;
			break;
		case 15:
			EmojiGrid = (ContentControl)target;
			EmojiGrid.MouseLeftButtonUp += OnEmojiClick;
			break;
		case 16:
			EmojiSelectPopup = (EscPopup)target;
			break;
		case 17:
			EmojiSelector = (EmojiSelectControl)target;
			break;
		case 18:
			((ContentControl)target).MouseLeftButtonUp += OnImageClick;
			break;
		case 19:
			AtPopup = (Popup)target;
			break;
		case 20:
			AtPopupScrollViewer = (ScrollViewer)target;
			break;
		case 21:
			AtPopupStackPanel = (StackPanel)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 11:
			((Border)target).MouseLeftButtonUp += OnAttachmentClick;
			break;
		case 12:
			((Grid)target).MouseLeftButtonUp += DeleteFile;
			break;
		}
	}
}
