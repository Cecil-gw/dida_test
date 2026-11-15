using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using TickTickModels;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Views.Calendar;

public class BindAccountWindow : Window, IOkCancelWindow, IComponentConnector
{
	private readonly SubscribeCalendar _parent;

	private readonly BindCalendarAccountModel _account;

	private string _email;

	private string _domain;

	private string _username;

	private string _description;

	private readonly bool _isReauthorize;

	private readonly Constants.BindAccountType _type;

	private bool _needDomain;

	private readonly string _kind;

	internal TextBlock CalendarMessage;

	internal TextBox EmailInput;

	internal Run UserNameRun;

	internal TextBox UsernameInput;

	internal TextBlock UsernameInputPlaceholder;

	internal PasswordBox PasswordInput;

	internal TextBlock PasswordInputPlaceholder;

	internal Run DomainStarRun;

	internal TextBox DomainInput;

	internal TextBlock ServerInputPlaceholder;

	internal TextBox DescriptionInput;

	internal TextBlock DescriptionInputPlaceholder;

	internal TextBlock ErrorText;

	internal TextBlock OperationGuide;

	internal Button CancelButton;

	internal Button SaveButton;

	private bool _contentLoaded;

	public bool IsExchange { get; }

	public BindAccountWindow(SubscribeCalendar parent, Constants.BindAccountType type, string kind = null)
	{
		IsExchange = type != Constants.BindAccountType.CalDAV;
		base.DataContext = this;
		InitializeComponent();
		_parent = parent;
		_type = type;
		_kind = kind;
		OperationGuide.Visibility = ((type != Constants.BindAccountType.CalDAV) ? Visibility.Collapsed : Visibility.Visible);
	}

	public BindAccountWindow(BindCalendarAccountModel account, bool isReauthorize = true, SubscribeCalendar parent = null)
	{
		base.DataContext = this;
		InitializeComponent();
		string kind = account.Kind;
		if (!(kind == "caldav"))
		{
			if (kind == "exchange")
			{
				_type = Constants.BindAccountType.Exchange;
			}
		}
		else
		{
			_type = Constants.BindAccountType.CalDAV;
		}
		base.Title = string.Format(Utils.GetString("ReauthorizeAccount"), _type.ToString());
		_account = account;
		_parent = parent;
		_isReauthorize = isReauthorize;
		DomainInput.Text = account.Domain;
		UsernameInput.Text = account.Username;
		DescriptionInput.Text = account.Description;
		OperationGuide.Visibility = ((!(account.Kind == "caldav")) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		switch (_type)
		{
		case Constants.BindAccountType.CalDAV:
			switch (_kind)
			{
			case "wxwork":
				base.Title = Utils.GetString("WeComCalendar");
				CalendarMessage.Visibility = Visibility.Visible;
				CalendarMessage.Text = Utils.GetString("WeComCalendarDesc");
				DescriptionInputPlaceholder.Text = Utils.GetString("MyWeComCalendar");
				break;
			case "dingtalk":
				base.Title = Utils.GetString("DingTalkCalendar");
				CalendarMessage.Visibility = Visibility.Visible;
				CalendarMessage.Text = Utils.GetString("DingTalkCalendarDesc");
				DescriptionInputPlaceholder.Text = Utils.GetString("MyDingTalkCalendar");
				break;
			case "feishu":
				base.Title = Utils.GetString("FeishuCalendar");
				CalendarMessage.Visibility = Visibility.Visible;
				CalendarMessage.Text = Utils.GetString("FeishuCalendarDesc");
				DescriptionInputPlaceholder.Text = Utils.GetString("MyFeishuCalendar");
				break;
			default:
				base.Title = Utils.GetString("AddCalDav");
				break;
			}
			_needDomain = true;
			break;
		case Constants.BindAccountType.Exchange:
			base.Title = Utils.GetString("AddExchange");
			DomainStarRun.Text = "";
			DescriptionInputPlaceholder.Text = Utils.GetString("MyExchangeAccount");
			UsernameInputPlaceholder.Text = Utils.GetString("ExchangeUserName");
			_needDomain = false;
			break;
		}
		if (_isReauthorize)
		{
			base.Title = string.Format(Utils.GetString("ReauthorizeAccount"), _type.ToString());
		}
	}

	private async void UpdateAccount()
	{
		BindAccountModel model = new BindAccountModel
		{
			domain = _domain,
			username = _username,
			password = PasswordInput.Password,
			desc = _description
		};
		string text = ((_type != Constants.BindAccountType.CalDAV) ? (await Communicator.UpdateExchangeAccount(_account.Id, model)) : (await Communicator.UpdateCalDavAccount(_account.Id, model)));
		string text2 = text;
		if (text2.Contains("calendar_bind_faild") || text2.Contains("false"))
		{
			ErrorText.Text = Utils.GetString("CalendarBindFaild");
			SetContentIsEnabled(isEnabled: true);
			return;
		}
		PullEvent();
		SetContentIsEnabled(isEnabled: true);
		if (_parent != null)
		{
			await _parent.LoadData(needPull: false);
		}
		Close();
	}

	private async Task PullEvent()
	{
		await CalendarService.PullBindCalEvents(_account.Id, null, null, _type);
		CalendarEventChangeNotifier.NotifyRemoteChanged();
	}

	private async void BindAccount()
	{
		_ = 3;
		try
		{
			BindCalendarAccountModel calendar = ((_type != Constants.BindAccountType.CalDAV) ? (await Communicator.BindExchangeAccount(new BindAccountModel
			{
				domain = _domain,
				email = _email,
				username = _username,
				password = PasswordInput.Password,
				desc = _description
			})) : (await Communicator.BindCalDavAccount(new BindAccountModel
			{
				domain = _domain,
				username = _username,
				password = PasswordInput.Password,
				desc = _description
			})));
			if (calendar == null)
			{
				throw new CustomException.CalendarBindException("caldav_bind_faild");
			}
			await BindCalendarAccountDao.SaveBindCalendarAccount(calendar);
			if (_parent != null)
			{
				await _parent.LoadData(needPull: false);
			}
			ListViewContainer.ReloadProjectData();
			CalendarService.PullBindCalEvents(calendar.Id, null, null, _type);
			SetContentIsEnabled(isEnabled: true);
			Close();
		}
		catch (CustomException.CalendarBindException ex)
		{
			if (ex.Message == "calendar_bind_duplicate")
			{
				ErrorText.Text = Utils.GetString("CalendarBindDuplicate");
			}
			else if (ex.Message == "calendar_bind_faild")
			{
				ErrorText.Text = Utils.GetString("CalendarBindFaild");
			}
			else if (ex.Message == "calendar_bind_exceed")
			{
				ErrorText.Text = string.Format(Utils.GetString("CalendarBindExceed"), _type.ToString());
			}
			else
			{
				ErrorText.Text = ex.Message;
			}
		}
		finally
		{
			SetContentIsEnabled(isEnabled: true);
		}
	}

	private void SetContentIsEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			SaveButton.Content = Utils.GetString("OK");
			SaveButton.IsEnabled = true;
			SaveButton.Opacity = 1.0;
			DomainInput.IsEnabled = !_isReauthorize;
			UsernameInput.IsEnabled = true;
			PasswordInput.IsEnabled = true;
			DescriptionInput.IsEnabled = true;
			CancelButton.IsEnabled = true;
		}
		else
		{
			SaveButton.Content = Utils.GetString("Subscribing");
			SaveButton.IsEnabled = false;
			SaveButton.Opacity = 0.699999988079071;
			CancelButton.IsEnabled = false;
			DomainInput.IsEnabled = false;
			UsernameInput.IsEnabled = false;
			PasswordInput.IsEnabled = false;
			DescriptionInput.IsEnabled = false;
		}
	}

	public async void Ok()
	{
		if ((_needDomain && string.IsNullOrEmpty(_domain)) || string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(PasswordInput.Password))
		{
			return;
		}
		if (_needDomain)
		{
			if (!CheckDomain())
			{
				SaveButton.IsEnabled = false;
				ErrorText.Text = Utils.GetString("CalDavServerError");
				return;
			}
		}
		else
		{
			ErrorModel errorModel = await Communicator.CheckExchangeAccount(_username);
			if (!string.IsNullOrEmpty(errorModel?.errorCode))
			{
				SaveButton.IsEnabled = false;
				ErrorText.Text = errorModel.errorMessage;
				return;
			}
		}
		ErrorText.Text = string.Empty;
		SetContentIsEnabled(isEnabled: false);
		if (_isReauthorize)
		{
			UpdateAccount();
		}
		else
		{
			BindAccount();
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public void OnCancel()
	{
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Ok();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		OnCancel();
	}

	private void OnPasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox)
		{
			if (passwordBox.Password.Length > 0)
			{
				PasswordInputPlaceholder.Visibility = Visibility.Collapsed;
			}
			else
			{
				PasswordInputPlaceholder.Visibility = Visibility.Visible;
			}
		}
		if (IsExchange)
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_email) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
		else
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_domain) && !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
	}

	private void OnDomainTextChanged(object sender, TextChangedEventArgs e)
	{
		_domain = DomainInput.Text;
		ErrorText.Text = string.Empty;
		if (IsExchange)
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_email) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
		else
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_domain) && !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		_description = DescriptionInput.Text;
		_username = UsernameInput.Text;
		_email = EmailInput.Text;
		ErrorText.Text = string.Empty;
		if (IsExchange)
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_email) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
		else
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(_domain) && !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(PasswordInput.Password);
		}
	}

	private bool CheckDomain()
	{
		if (string.IsNullOrEmpty(_domain))
		{
			return false;
		}
		if (new Regex("^[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9](?:\\.[a-zA-Z]{2,})").IsMatch(_domain))
		{
			return true;
		}
		return new Regex("(https?):\\/\\/[-A-Za-z0-9+&@#/%?=~_|!:,.;]+[-A-Za-z0-9+&@#/%=~_|]").IsMatch(_domain);
	}

	protected override void OnClosed(EventArgs eventArgs)
	{
		try
		{
			if (base.Owner != null)
			{
				base.Owner.Activate();
			}
		}
		catch (Exception)
		{
		}
	}

	private void ClearEmailText(object sender, MouseButtonEventArgs e)
	{
		EmailInput.Text = string.Empty;
	}

	private void ClearDomainText(object sender, MouseButtonEventArgs e)
	{
		DomainInput.Text = string.Empty;
	}

	private void ClearNameText(object sender, MouseButtonEventArgs e)
	{
		UsernameInput.Text = string.Empty;
	}

	private void ClearDescText(object sender, MouseButtonEventArgs e)
	{
		DescriptionInput.Text = string.Empty;
	}

	private void OpenGuide(object sender, MouseButtonEventArgs e)
	{
		string empty = string.Empty;
		Utils.TryProcessStartUrl(_kind switch
		{
			"wxwork" => "https://help.dida365.com/articles/6950000554735042560#%E4%BC%81%E4%B8%9A%E5%BE%AE%E4%BF%A1", 
			"dingtalk" => "https://help.dida365.com/articles/6950000554735042560#%E9%92%89%E9%92%89%E6%97%A5%E5%8E%86", 
			"feishu" => "https://help.dida365.com/articles/6950000554735042560#%E9%A3%9E%E4%B9%A6%E6%97%A5%E5%8E%86", 
			_ => Utils.IsDida() ? "https://help.dida365.com/articles/6950000554735042560#caldav-%E6%97%A5%E5%8E%86%E5%90%8C%E6%AD%A5" : "https://help.ticktick.com/articles/7055781593733922816#caldav-calendar-sync", 
		});
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/calendar/bindaccountwindow.xaml", UriKind.Relative);
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
			((BindAccountWindow)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			CalendarMessage = (TextBlock)target;
			break;
		case 3:
			EmailInput = (TextBox)target;
			EmailInput.TextChanged += OnTextChanged;
			break;
		case 4:
			((Border)target).MouseLeftButtonUp += ClearEmailText;
			break;
		case 5:
			UserNameRun = (Run)target;
			break;
		case 6:
			UsernameInput = (TextBox)target;
			UsernameInput.TextChanged += OnTextChanged;
			break;
		case 7:
			UsernameInputPlaceholder = (TextBlock)target;
			break;
		case 8:
			((Border)target).MouseLeftButtonUp += ClearNameText;
			break;
		case 9:
			PasswordInput = (PasswordBox)target;
			PasswordInput.PasswordChanged += OnPasswordChanged;
			break;
		case 10:
			PasswordInputPlaceholder = (TextBlock)target;
			break;
		case 11:
			DomainStarRun = (Run)target;
			break;
		case 12:
			DomainInput = (TextBox)target;
			DomainInput.TextChanged += OnDomainTextChanged;
			break;
		case 13:
			ServerInputPlaceholder = (TextBlock)target;
			break;
		case 14:
			((Border)target).MouseLeftButtonUp += ClearDomainText;
			break;
		case 15:
			DescriptionInput = (TextBox)target;
			DescriptionInput.TextChanged += OnTextChanged;
			break;
		case 16:
			DescriptionInputPlaceholder = (TextBlock)target;
			break;
		case 17:
			((Border)target).MouseLeftButtonUp += ClearDescText;
			break;
		case 18:
			ErrorText = (TextBlock)target;
			break;
		case 19:
			OperationGuide = (TextBlock)target;
			break;
		case 20:
			((Run)target).MouseLeftButtonUp += OpenGuide;
			break;
		case 21:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 22:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
