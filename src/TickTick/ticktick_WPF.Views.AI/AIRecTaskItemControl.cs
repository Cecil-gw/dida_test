using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ticktick_WPF.Entity.Binding;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Views.AI;

public class AIRecTaskItemControl : UserControl, IComponentConnector
{
	public static readonly DependencyProperty CheckCommandProperty = DependencyProperty.Register("CheckCommand", typeof(ICommand), typeof(AIRecTaskItemControl), new PropertyMetadata(null));

	public static readonly DependencyProperty AddCommandProperty = DependencyProperty.Register("AddCommand", typeof(ICommand), typeof(AIRecTaskItemControl), new PropertyMetadata(null));

	public static readonly RoutedEvent CheckClickedEvent = EventManager.RegisterRoutedEvent("CheckClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AIRecTaskItemControl));

	public static readonly RoutedEvent AddClickedEvent = EventManager.RegisterRoutedEvent("AddClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AIRecTaskItemControl));

	internal AIRecTaskItemControl ControlRoot;

	internal TaskTitleBox TitleBox;

	internal Path AddPath;

	private bool _contentLoaded;

	public ICommand CheckCommand
	{
		get
		{
			return (ICommand)GetValue(CheckCommandProperty);
		}
		set
		{
			SetValue(CheckCommandProperty, value);
		}
	}

	public ICommand AddCommand
	{
		get
		{
			return (ICommand)GetValue(AddCommandProperty);
		}
		set
		{
			SetValue(AddCommandProperty, value);
		}
	}

	private Storyboard storyboard { get; set; }

	public event RoutedEventHandler CheckClicked
	{
		add
		{
			AddHandler(CheckClickedEvent, value);
		}
		remove
		{
			RemoveHandler(CheckClickedEvent, value);
		}
	}

	public event RoutedEventHandler AddClicked
	{
		add
		{
			AddHandler(AddClickedEvent, value);
		}
		remove
		{
			RemoveHandler(AddClickedEvent, value);
		}
	}

	public AIRecTaskItemControl()
	{
		InitializeComponent();
	}

	private void ControlRoot_Loaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is AIRecommendItem aIRecommendItem)
		{
			aIRecommendItem.OnRemoveAnimate = (Action<bool>)Delegate.Combine(aIRecommendItem.OnRemoveAnimate, new Action<bool>(OnRemoveAnimate));
		}
	}

	private void ControlRoot_Unloaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is AIRecommendItem aIRecommendItem)
		{
			aIRecommendItem.OnRemoveAnimate = (Action<bool>)Delegate.Remove(aIRecommendItem.OnRemoveAnimate, new Action<bool>(OnRemoveAnimate));
		}
	}

	private void OnRemoveAnimate(bool play)
	{
		if (play)
		{
			storyboard = new Storyboard();
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				From = base.ActualHeight,
				To = 0.0,
				Duration = TimeSpan.FromSeconds(0.3)
			};
			Storyboard.SetTarget(doubleAnimation, this);
			Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("Height"));
			storyboard.Children.Add(doubleAnimation);
			DoubleAnimation doubleAnimation2 = new DoubleAnimation
			{
				To = 0.0,
				Duration = TimeSpan.FromSeconds(0.3)
			};
			Storyboard.SetTarget(doubleAnimation2, this);
			Storyboard.SetTargetProperty(doubleAnimation2, new PropertyPath("Opacity"));
			storyboard.Children.Add(doubleAnimation2);
			EventHandler completedHandler = null;
			completedHandler = delegate
			{
				if (base.DataContext is AIRecommendItem aIRecommendItem)
				{
					aIRecommendItem.OnRemoveCompleted?.Invoke();
				}
				storyboard.Completed -= completedHandler;
			};
			storyboard.Completed += completedHandler;
			storyboard.Begin();
		}
		else
		{
			storyboard?.Stop(this);
			BeginAnimation(FrameworkElement.HeightProperty, null);
			BeginAnimation(UIElement.OpacityProperty, null);
			base.Height = double.NaN;
			base.Opacity = 1.0;
		}
	}

	private void CheckButton_Click(object sender, RoutedEventArgs e)
	{
		RoutedEventArgs e2 = new RoutedEventArgs(CheckClickedEvent, this);
		RaiseEvent(e2);
	}

	private void AddButton_Click(object sender, RoutedEventArgs e)
	{
		RoutedEventArgs e2 = new RoutedEventArgs(AddClickedEvent, this);
		RaiseEvent(e2);
	}

	private void TitleBox_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is AIRecommendItem aIRecommendItem)
		{
			aIRecommendItem.PropertyChanged -= Data_PropertyChanged;
		}
		AIRecommendItem aIRecommendItem2 = e.NewValue as AIRecommendItem;
		if (aIRecommendItem2 != null)
		{
			aIRecommendItem2.PropertyChanged += Data_PropertyChanged;
		}
		TitleBox.SetTextOffset(aIRecommendItem2?.Name, restoreOffset: true, clearUndo: true);
	}

	private void Data_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		AIRecommendItem aIRecommendItem = base.DataContext as AIRecommendItem;
		TitleBox.SetTextOffset(aIRecommendItem?.Name, restoreOffset: true, clearUndo: true);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/ai/airectaskitemcontrol.xaml", UriKind.Relative);
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
			ControlRoot = (AIRecTaskItemControl)target;
			ControlRoot.Loaded += ControlRoot_Loaded;
			ControlRoot.Unloaded += ControlRoot_Unloaded;
			break;
		case 2:
			((Button)target).Click += CheckButton_Click;
			break;
		case 3:
			TitleBox = (TaskTitleBox)target;
			break;
		case 4:
			((Button)target).Click += AddButton_Click;
			break;
		case 5:
			AddPath = (Path)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
