using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace ticktick_WPF.Views.Pomo.MainFocus;

public class CustomUndoToast : UserControl, IComponentConnector
{
	internal Border Container;

	internal TextBlock MainText;

	private bool _contentLoaded;

	private bool hasUndo { get; set; }

	private Action UndoAction { get; set; }

	private Action FinishAction { get; set; }

	public CustomUndoToast()
	{
		InitializeComponent();
	}

	public void SetText(string text)
	{
		MainText.Text = text;
	}

	public async Task SetEvent(Action onFinish, Action onUndo)
	{
		UndoAction = onUndo;
		FinishAction = onFinish;
	}

	private void OnUndoClick(object sender, MouseButtonEventArgs e)
	{
		hasUndo = true;
		base.Visibility = Visibility.Collapsed;
		UndoAction?.Invoke();
	}

	public void TryFinish()
	{
		if (!hasUndo)
		{
			FinishAction?.Invoke();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/pomo/mainfocus/customundotoast.xaml", UriKind.Relative);
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
			Container = (Border)target;
			break;
		case 2:
			((StackPanel)target).MouseLeftButtonUp += OnUndoClick;
			break;
		case 3:
			MainText = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
