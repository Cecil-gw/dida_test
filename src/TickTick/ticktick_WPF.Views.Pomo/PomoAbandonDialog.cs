using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo;

public class PomoAbandonDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	public bool? Saved;

	internal TextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal Button SaveButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	public PomoAbandonDialog(bool taskSelected, bool canSave)
	{
		InitializeComponent();
		SaveButton.Visibility = ((!canSave) ? Visibility.Collapsed : Visibility.Visible);
		TitleTextBlock.Text = Utils.GetString(canSave ? "EndPomoTitle" : "AbandonPomoTitle");
		ContentTextBlock.Text = Utils.GetString(canSave ? "EndPomoContent" : ((Constants.MinFocusDuration == 60) ? "AbandonPomoContentLess1Min" : "AbandonPomoContent"));
		SaveButton.Content = Utils.GetString("EndAndSave");
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.OkCommand, new KeyGesture(Key.Return))
		{
			CommandParameter = this
		});
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnDropClick(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Saved = false;
		Close();
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
		Saved = true;
		Close();
	}

	public async void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/pomoabandondialog.xaml", UriKind.Relative);
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
			TitleTextBlock = (TextBlock)target;
			break;
		case 2:
			ContentTextBlock = (TextBlock)target;
			break;
		case 3:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 4:
			((Button)target).Click += OnDropClick;
			break;
		case 5:
			((Button)target).Click += OnCloseClick;
			break;
		case 6:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
