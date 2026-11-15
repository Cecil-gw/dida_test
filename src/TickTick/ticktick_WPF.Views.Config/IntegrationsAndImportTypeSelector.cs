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
using ticktick_WPF.Dal;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class IntegrationsAndImportTypeSelector : UserControl, IComponentConnector, IStyleConnector
{
	internal IntegrationsAndImportTypeSelector Root;

	internal StackPanel ItemsContainer;

	private bool _contentLoaded;

	public event EventHandler<SubscribeTypeItemViewModel> OnItemSelected;

	public IntegrationsAndImportTypeSelector()
	{
		InitializeComponent();
		ItemsContainer.Children.Add(new IntegrationItemsControl(Utils.GetString("Featured"), SubscribeTypeItemViewModel.GetRecommendItems()));
		ItemsContainer.Children.Add(new IntegrationItemsControl(Utils.GetString("Calendar"), SubscribeTypeItemViewModel.GetSubscribeItems()));
		ItemsContainer.Children.Add(new IntegrationItemsControl(Utils.GetString("Integrate"), SubscribeTypeItemViewModel.GetIntegrationItems()));
		ItemsContainer.Children.Add(new IntegrationItemsControl(Utils.GetString("Others"), SubscribeTypeItemViewModel.GetOtherItems()));
	}

	public void LoadIntegratedItems(List<SubscribeCalendarViewModel> integratedCals)
	{
		if (!UserDao.IsPro() || integratedCals == null || integratedCals.Count == 0)
		{
			if (ItemsContainer.Children.Count > 0 && ItemsContainer.Children[0] is IntegrationItemsControl integrationItemsControl && integrationItemsControl.Title.Text == Utils.GetString("Integrated1"))
			{
				ItemsContainer.Children.RemoveAt(0);
			}
			return;
		}
		List<SubscribeTypeItemViewModel> list = new List<SubscribeTypeItemViewModel>();
		List<SubscribeCalendarViewModel> list2 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Site == "google").ToList();
		if (list2 != null && list2.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("Google", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list2.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list2.Count) : list2[0].Title),
				ShowExpired = list2.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list3 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Site == "outlook").ToList();
		if (list3 != null && list3.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("Outlook", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list3.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list3.Count) : list3[0].Title),
				ShowExpired = list3.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list4 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Kind == "exchange").ToList();
		if (list4 != null && list4.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("Exchange", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list4.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list4.Count) : list4[0].Title),
				ShowExpired = list4.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list5 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Kind == "icloud").ToList();
		if (list5 != null && list5.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("iCloud", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list5.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list5.Count) : list5[0].Title),
				ShowExpired = list5.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list6 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Kind == "caldav").ToList();
		if (list6 != null && list6.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("CalDAV", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list6.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list6.Count) : list6[0].Title),
				ShowExpired = list6.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list7 = integratedCals.Where((SubscribeCalendarViewModel x) => x.Kind == "holiday").ToList();
		if (list7 != null && list7.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("Holiday", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list7.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list7.Count) : list7[0].Title),
				ShowExpired = list7.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list8 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Kind == "url").ToList();
		if (list8 != null && list8.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("URL", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list8.Count > 1) ? string.Format(Utils.GetString("NAccounts"), list8.Count) : list8[0].Title),
				ShowExpired = list8.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		List<SubscribeCalendarViewModel> list9 = integratedCals.Where((SubscribeCalendarViewModel c) => c.Site == "notion").ToList();
		if (list9 != null && list9.Count > 0)
		{
			list.Add(new SubscribeTypeItemViewModel("Notion", isSubscribed: true)
			{
				ShowDesc = Visibility.Visible,
				DescWrap = TextWrapping.NoWrap,
				Desc = ((list9.Count > 1) ? string.Format(Utils.GetString("NWorkspaces"), list9.Count) : list9[0].Title),
				ShowExpired = list9.Any((SubscribeCalendarViewModel c) => c.IsExpired)
			});
		}
		if (ItemsContainer.Children.Count > 0 && ItemsContainer.Children[0] is IntegrationItemsControl integrationItemsControl2 && integrationItemsControl2.Title.Text == Utils.GetString("Integrated1"))
		{
			integrationItemsControl2.SetItems(list);
		}
		else
		{
			ItemsContainer.Children.Insert(0, new IntegrationItemsControl(Utils.GetString("Integrated1"), list));
		}
	}

	private void OnBindCalendarClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: SubscribeTypeItemViewModel dataContext })
		{
			this.OnItemSelected?.Invoke(sender, dataContext);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/config/integrationsandimporttypeselector.xaml", UriKind.Relative);
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
			Root = (IntegrationsAndImportTypeSelector)target;
			break;
		case 3:
			ItemsContainer = (StackPanel)target;
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
		if (connectionId == 2)
		{
			((Border)target).MouseLeftButtonUp += OnBindCalendarClick;
		}
	}
}
