using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Team;

public class TeamExpiredRemindDialog : Window, IComponentConnector
{
	private List<TeamModel> _teams;

	internal TextBlock TitleText;

	internal TextBlock RemindText;

	internal Button UploadButton;

	internal Button CloseButton;

	private bool _contentLoaded;

	public TeamExpiredRemindDialog(List<TeamModel> teams)
	{
		_teams = teams;
		InitializeComponent();
		base.Closing += delegate
		{
			base.Owner?.Activate();
		};
	}

	private async void OnUploadClick(object sender, RoutedEventArgs e)
	{
		if (_teams.Count == 1)
		{
			string arg = await Communicator.GetSignOnToken();
			Utils.TryProcessStartUrl(string.Format(BaseUrl.GetApiDomain() + "/sign/autoSignOn?token={0}&dest={1}", arg, string.Format(Utils.GetString("RenewTeamUrl"), _teams[0].id)));
		}
		Close();
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (_teams == null)
		{
			return;
		}
		if (_teams.Count == 1)
		{
			TitleText.Text = string.Format(Utils.GetString("TeamOutDate"), _teams[0].name);
			RemindText.Text = string.Format(Utils.GetString("TeamOutDateText"), _teams[0].name);
		}
		if (_teams.Count > 1)
		{
			string text = "\"" + _teams[0].name + "\"";
			for (int i = 1; i < _teams.Count; i++)
			{
				text = text + ",\"" + _teams[i].name + "\"";
			}
			TitleText.Text = string.Format(Utils.GetString("TeamsOutDate"), _teams.Count);
			RemindText.Text = string.Format(Utils.GetString("TeamsOutDateText"), text);
			CloseButton.Visibility = Visibility.Visible;
			UploadButton.Visibility = Visibility.Collapsed;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/team/teamexpiredreminddialog.xaml", UriKind.Relative);
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
			((TeamExpiredRemindDialog)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			TitleText = (TextBlock)target;
			break;
		case 3:
			RemindText = (TextBlock)target;
			break;
		case 4:
			((Grid)target).MouseLeftButtonDown += OnCloseClick;
			break;
		case 5:
			UploadButton = (Button)target;
			UploadButton.Click += OnUploadClick;
			break;
		case 6:
			CloseButton = (Button)target;
			CloseButton.Click += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
