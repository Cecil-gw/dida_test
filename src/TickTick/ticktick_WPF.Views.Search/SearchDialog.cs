using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Views.Search;

public class SearchDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	private bool _restore;

	private readonly string _from;

	internal SearchInputControl SearchInput;

	private bool _contentLoaded;

	public bool Showing { get; set; } = true;

	public SearchDialog(bool restore, SearchKind kind, List<string> linkTaskIds, string from = "list")
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		base.Closed += OnClosed;
		base.Activated += OnWindowActivated;
		_restore = restore;
		SearchProjectHelper.UpdateSortOrder();
		SearchInput.InitSearch(restore, kind, linkTaskIds);
		_from = from;
	}

	private void OnClosed(object sender, EventArgs e)
	{
		SearchInput.Dispose();
		PopupStateManager.Reset();
	}

	private void OnWindowActivated(object sender, EventArgs e)
	{
		DelayFocus();
	}

	private async Task DelayFocus()
	{
		await Task.Delay(200);
		SearchInput.FocusEnd();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SearchInput.Search -= OnSearch;
		SearchInput.Search += OnSearch;
		SearchInput.Close -= OnInputClose;
		SearchInput.Close += OnInputClose;
		WindowNotifier.NotifySubWindowOpen(SubWindowType.Search);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (base.Owner != null && base.Owner.IsLoaded)
		{
			base.Owner.Activate();
		}
		Showing = false;
		base.OnClosing(e);
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		if (GetTemplateChild("TitleGrid") is Grid grid)
		{
			grid.Visibility = Visibility.Collapsed;
		}
		base.OnApplyTemplate();
	}

	private void OnInputClose(object sender, EventArgs e)
	{
		if (!LocalSettings.Settings.InSearch)
		{
			App.Window.ResetLeftSelected();
		}
		Close();
	}

	private void OnSearch(object sender, SearchExtra e)
	{
		e.from = _from;
		App.Window.OnSearch(e, _restore);
		Close();
	}

	public void OnCancel()
	{
		App.Window.ResetLeftSelected();
		Close();
	}

	public async void Ok()
	{
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed && SearchInput.CanDragMove())
		{
			DragMove();
		}
	}

	public void TryClose()
	{
		try
		{
			App.Window.ResetLeftSelected();
			Close();
		}
		catch (Exception)
		{
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/search/searchdialog.xaml", UriKind.Relative);
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
		if (connectionId == 1)
		{
			SearchInput = (SearchInputControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
