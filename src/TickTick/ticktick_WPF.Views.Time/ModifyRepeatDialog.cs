using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;
using ticktick_WPF.Util.KotlinUtils;

namespace ticktick_WPF.Views.Time;

public class ModifyRepeatDialog : MyWindow, IOkCancelWindow, IComponentConnector
{
	public EditorType Selected = EditorType.CANCEL;

	public static bool Showing;

	internal TextBlock TitleTextBlock;

	internal TextBlock ContentTextBlock;

	internal StackPanel ModifyRadios;

	internal RadioButton CurrentRadio;

	internal RadioButton FutureRadio;

	internal RadioButton AllRadio;

	internal StackPanel CompleteRadios;

	internal RadioButton CompleteAllRadio;

	internal RadioButton SkipAllRadio;

	internal Button OkButton;

	internal Button CancelButton;

	internal Border CloseButton;

	private bool _contentLoaded;

	public ModifyRepeatDialog(List<string> strings, string taskTitle = null, bool isDelete = false)
	{
		InitializeComponent();
		if (!strings.Contains(EditorType.CURRENT.ToString()))
		{
			CurrentRadio.Visibility = Visibility.Collapsed;
		}
		else
		{
			CurrentRadio.IsChecked = true;
		}
		if (!strings.Contains(EditorType.FROM_CURRENT.ToString()))
		{
			FutureRadio.Visibility = Visibility.Collapsed;
		}
		if (!strings.Contains(EditorType.ALL.ToString()))
		{
			AllRadio.Visibility = Visibility.Collapsed;
		}
		TitleTextBlock.Text = Utils.GetString("EditRecurringTask");
		if (taskTitle == "")
		{
			taskTitle = " ";
		}
		ContentTextBlock.Text = ((!isDelete) ? ((taskTitle == null) ? Utils.GetString("EditingRepeat") : string.Format(Utils.GetString("EditingTaskRepeat"), taskTitle)) : ((taskTitle == null) ? Utils.GetString("DeletingRepeat") : string.Format(Utils.GetString("DeletingTaskRepeat"), taskTitle)));
		Showing = true;
	}

	public ModifyRepeatDialog(List<DateTime> repeatDates)
	{
		InitializeComponent();
		ModifyRadios.Visibility = Visibility.Collapsed;
		CompleteRadios.Visibility = Visibility.Visible;
		CompleteAllRadio.IsChecked = true;
		TitleTextBlock.Text = Utils.GetString("EditRecurringTask");
		ContentTextBlock.Text = string.Format(Utils.GetString((repeatDates.Count > 1) ? "HandleTaskCycles" : "HandleTaskCycle"), repeatDates.Count);
		Showing = true;
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Selected = EditorType.CANCEL;
		Close();
	}

	private void OnOkClick(object sender, RoutedEventArgs e)
	{
		Ok();
	}

	private async void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Selected = EditorType.CANCEL;
		Close();
	}

	public async void OnCancel()
	{
		Selected = EditorType.CANCEL;
		Close();
	}

	public void Ok()
	{
		if (CurrentRadio.IsChecked == true)
		{
			Selected = EditorType.CURRENT;
		}
		else if (FutureRadio.IsChecked == true)
		{
			Selected = EditorType.FROM_CURRENT;
		}
		else if (AllRadio.IsChecked == true)
		{
			Selected = EditorType.ALL;
		}
		else if (CompleteAllRadio.IsChecked == true)
		{
			Selected = EditorType.COMPLETEALL;
		}
		else if (SkipAllRadio.IsChecked == true)
		{
			Selected = EditorType.SKIP;
		}
		else
		{
			Selected = EditorType.CANCEL;
		}
		Close();
	}

	protected override void OnClosed(EventArgs e)
	{
		Showing = false;
		GlobalEventManager.NotifyModifyRecurrenceCompleted();
		base.OnClosed(e);
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
		Activate();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/time/modifyrepeatdialog.xaml", UriKind.Relative);
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
			ModifyRadios = (StackPanel)target;
			break;
		case 4:
			CurrentRadio = (RadioButton)target;
			break;
		case 5:
			FutureRadio = (RadioButton)target;
			break;
		case 6:
			AllRadio = (RadioButton)target;
			break;
		case 7:
			CompleteRadios = (StackPanel)target;
			break;
		case 8:
			CompleteAllRadio = (RadioButton)target;
			break;
		case 9:
			SkipAllRadio = (RadioButton)target;
			break;
		case 10:
			OkButton = (Button)target;
			OkButton.Click += OnOkClick;
			break;
		case 11:
			CancelButton = (Button)target;
			CancelButton.Click += OnCancelClick;
			break;
		case 12:
			CloseButton = (Border)target;
			CloseButton.MouseLeftButtonUp += OnCloseClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
