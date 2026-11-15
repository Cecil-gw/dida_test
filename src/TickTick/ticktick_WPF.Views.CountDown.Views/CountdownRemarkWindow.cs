using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CountDown.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views.CountDown.Views;

public class CountdownRemarkWindow : Window, IComponentConnector
{
	public class CountdownRemarkViewModel : INotifyPropertyChanged
	{
		private ImageSource _image;

		private string _title;

		private string _content;

		private string _wordCount;

		public ImageSource Image
		{
			get
			{
				return _image;
			}
			set
			{
				_image = value;
				NotifyPropertyChanged("Image");
			}
		}

		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				_title = value;
				NotifyPropertyChanged("Title");
			}
		}

		public string Content
		{
			get
			{
				return _content;
			}
			set
			{
				_content = value;
				if (string.IsNullOrEmpty(value) || value.Length < 900)
				{
					WordCount = null;
				}
				else
				{
					WordCount = $"{value.Length}/1000";
				}
				NotifyPropertyChanged("Content");
			}
		}

		public string WordCount
		{
			get
			{
				return _wordCount;
			}
			set
			{
				if (!(_wordCount == value))
				{
					_wordCount = value;
					NotifyPropertyChanged("WordCount");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	internal TextBox ContentTextbox;

	internal Button InsertTimeButton;

	internal Popup DatePopup;

	internal StackPanel OperateGrid;

	internal Button SaveButton;

	private bool _contentLoaded;

	private string countdownId { get; set; }

	private CountDownViewModel cardVm { get; set; }

	private CountdownRemarkViewModel viewModel { get; set; }

	private DateTime popupCloseTime { get; set; }

	public CountdownRemarkWindow(CountDownViewModel countdownVM)
	{
		InitializeComponent();
		base.DataContext = (viewModel = new CountdownRemarkViewModel());
		if (countdownVM.IsArchived)
		{
			OperateGrid.Visibility = Visibility.Collapsed;
			ContentTextbox.IsReadOnly = true;
			ContentTextbox.Style = (Style)FindResource("EditTextStyleNoHightlight");
			InsertTimeButton.Visibility = Visibility.Collapsed;
		}
		cardVm = countdownVM;
		countdownId = countdownVM.Model.Id;
		viewModel.Image = countdownVM.Icon;
		viewModel.Title = countdownVM.Title;
		LoadData();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async Task LoadData()
	{
		CountdownModel countdownModel = await CountdownDao.GetCountdownById(countdownId);
		if (countdownModel != null)
		{
			viewModel.Content = countdownModel.Remark;
			if (!cardVm.IsArchived)
			{
				ContentTextbox.Focus();
				ContentTextbox.SelectionStart = ContentTextbox.Text.Length;
			}
		}
	}

	private void OkButton_Click(object sender, RoutedEventArgs e)
	{
		Utils.Toast(Utils.GetString("Saved"));
		Close();
		Task.Run(async delegate
		{
			CountdownModel countdownModel = await CountdownDao.UpdateRemark(countdownId, viewModel.Content);
			if (countdownModel != null)
			{
				cardVm.Model = countdownModel;
			}
		});
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void DatePopup_Closed(object sender, EventArgs e)
	{
		popupCloseTime = DateTime.Now;
	}

	private void InsertTimeButton_Click(object sender, RoutedEventArgs e)
	{
		if (!((DateTime.Now - popupCloseTime).TotalMilliseconds < 200.0))
		{
			DateTime now = DateTime.Now;
			string text = DateUtils.FormatYearMonthDay(now, DateFormatLen.Middle);
			string timeText = DateUtils.GetTimeText(now);
			string text2 = text + " " + timeText;
			string text3 = DateUtils.FormatYearMonthDay(now, DateFormatLen.Short);
			CustomMenuList customMenuList = new CustomMenuList(new List<CustomMenuItemViewModel>
			{
				new CustomMenuItemViewModel((object)text2, text2, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)text, text, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)text3, text3, (Geometry)null, "")
				{
					FontSize = 11
				},
				new CustomMenuItemViewModel((object)timeText, timeText, (Geometry)null, "")
				{
					FontSize = 11
				}
			}, DatePopup);
			customMenuList.Operated += OnDataInsertOperate;
			customMenuList.Show();
		}
	}

	private void OnDataInsertOperate(object sender, object text)
	{
		string text2 = text.ToString();
		TextBox contentTextbox = ContentTextbox;
		int caretIndex = contentTextbox.CaretIndex;
		string text3 = contentTextbox.Text.Insert(caretIndex, text2);
		contentTextbox.Text = text3;
		contentTextbox.CaretIndex = caretIndex + text2.Length;
		contentTextbox.Focus();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/countdown/views/countdownremarkwindow.xaml", UriKind.Relative);
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
			ContentTextbox = (TextBox)target;
			break;
		case 2:
			InsertTimeButton = (Button)target;
			InsertTimeButton.Click += InsertTimeButton_Click;
			break;
		case 3:
			DatePopup = (Popup)target;
			DatePopup.Closed += DatePopup_Closed;
			break;
		case 4:
			OperateGrid = (StackPanel)target;
			break;
		case 5:
			SaveButton = (Button)target;
			SaveButton.Click += OkButton_Click;
			break;
		case 6:
			((Button)target).Click += CancelButton_Click;
			break;
		case 7:
			((Button)target).Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
