using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Misc;

public class AnimateGroupTitle : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty TitlesProperty = DependencyProperty.Register("Titles", typeof(string), typeof(AnimateGroupTitle), new PropertyMetadata(null, OnTitlesPorpertyChanged));

	public static readonly DependencyProperty ItemPaddingProperty = DependencyProperty.Register("ItemPadding", typeof(Thickness), typeof(AnimateGroupTitle), new PropertyMetadata(new Thickness(20.0, 0.0, 20.0, 0.0)));

	public static readonly DependencyProperty IsNoBackStyleProperty = DependencyProperty.Register("IsNoBackStyle", typeof(bool), typeof(AnimateGroupTitle), new PropertyMetadata());

	public static readonly DependencyProperty ItemMaxWidthProperty = DependencyProperty.Register("ItemMaxWidth", typeof(double), typeof(AnimateGroupTitle), new PropertyMetadata(1000.0, OnSetItemMaxWidth));

	public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(AnimateGroupTitle), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged));

	internal AnimateGroupTitle Root;

	internal Border SelectedBorder;

	internal TranslateTransform SelectedTransform;

	internal ItemsControl TitlesListView;

	private bool _contentLoaded;

	public string Titles
	{
		get
		{
			return (string)GetValue(TitlesProperty);
		}
		set
		{
			SetValue(TitlesProperty, value);
		}
	}

	public Thickness ItemPadding
	{
		get
		{
			return (Thickness)GetValue(ItemPaddingProperty);
		}
		set
		{
			SetValue(ItemPaddingProperty, value);
		}
	}

	public bool IsNoBackStyle
	{
		get
		{
			return (bool)GetValue(IsNoBackStyleProperty);
		}
		set
		{
			SetValue(IsNoBackStyleProperty, value);
		}
	}

	public double ItemMaxWidth
	{
		get
		{
			return (double)GetValue(ItemMaxWidthProperty);
		}
		set
		{
			SetValue(ItemMaxWidthProperty, value);
		}
	}

	public int SelectedIndex
	{
		get
		{
			return (int)GetValue(SelectedIndexProperty);
		}
		set
		{
			SetValue(SelectedIndexProperty, value);
		}
	}

	public string SelectedTitle => Models.FirstOrDefault((GroupTitleViewModel m) => m.IsSelected)?.Title ?? string.Empty;

	public ObservableCollection<GroupTitleViewModel> Models { get; set; } = new ObservableCollection<GroupTitleViewModel>();

	public event EventHandler<GroupTitleViewModel> SelectedTitleChanged;

	private static void OnTitlesPorpertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		(d as AnimateGroupTitle)?.UpdateTitles();
	}

	private static void OnSetItemMaxWidth(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AnimateGroupTitle obj = (AnimateGroupTitle)d;
		double maxWidth = (double)e.NewValue;
		obj.SetMaxWidth(maxWidth);
	}

	private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AnimateGroupTitle obj = (AnimateGroupTitle)d;
		int selectedIndex = (int)e.NewValue;
		obj.SetSelectedIndex(selectedIndex);
	}

	public AnimateGroupTitle()
	{
		InitializeComponent();
		TitlesListView.ItemsSource = Models;
	}

	public async Task UpdateTitles()
	{
		string[] array = Titles.Split('|');
		if (array.Any())
		{
			Models.Clear();
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				Models.Add(new GroupTitleViewModel
				{
					Content = Utils.GetString(text),
					Title = text,
					Index = i,
					MaxWidth = ItemMaxWidth,
					IsNoBackStyle = IsNoBackStyle
				});
			}
			if (SelectedIndex >= 0 && SelectedIndex < Models.Count)
			{
				Models[SelectedIndex].IsSelected = true;
			}
		}
	}

	private void OnItemClick(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is FrameworkElement { DataContext: GroupTitleViewModel { IsSelected: false } dataContext }))
		{
			return;
		}
		foreach (GroupTitleViewModel model in Models)
		{
			model.IsSelected = false;
		}
		dataContext.IsSelected = true;
		SelectedIndex = dataContext.Index;
		this.SelectedTitleChanged?.Invoke(this, dataContext);
	}

	private void ScrollToItem(FrameworkElement element, bool anim = true)
	{
		if (element != null)
		{
			double num = Math.Max(1.0, element.TranslatePoint(new Point(0.0, 0.0), TitlesListView).X);
			if (anim)
			{
				DoubleAnimation doubleAnimation = AnimationUtils.GetDoubleAnimation(SelectedBorder.Width, element.ActualWidth, 400);
				doubleAnimation.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				DoubleAnimation doubleAnimation2 = AnimationUtils.GetDoubleAnimation(SelectedTransform.X, num, 400);
				doubleAnimation2.EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				SelectedBorder.BeginAnimation(FrameworkElement.WidthProperty, doubleAnimation);
				SelectedTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimation2);
			}
			else
			{
				SelectedBorder.BeginAnimation(FrameworkElement.WidthProperty, null);
				SelectedTransform.BeginAnimation(TranslateTransform.XProperty, null);
				SelectedBorder.Width = element.ActualWidth;
				SelectedTransform.X = num;
			}
		}
	}

	public void SetSelectedIndex(int index)
	{
		if (index < Models.Count && index >= 0)
		{
			for (int i = 0; i < Models.Count; i++)
			{
				Models[i].IsSelected = index == i;
			}
			if (SelectedIndex != index)
			{
				SelectedIndex = index;
			}
			ScrollToSelectedIndex();
		}
	}

	public void SetSelectedTitle(string value)
	{
		GroupTitleViewModel groupTitleViewModel = Models.FirstOrDefault((GroupTitleViewModel m) => m.Title == value);
		if (groupTitleViewModel != null)
		{
			int selectedIndex = Models.IndexOf(groupTitleViewModel);
			SetSelectedIndex(selectedIndex);
		}
		else
		{
			SetSelectedIndex(0);
		}
	}

	public void SetMaxWidth(double width)
	{
		foreach (GroupTitleViewModel model in Models)
		{
			model.MaxWidth = width;
		}
	}

	private void TitlesListView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		ScrollToSelectedIndex(withAnimate: false);
	}

	private void ScrollToSelectedIndex(bool withAnimate = true)
	{
		if (Models.Any())
		{
			FrameworkElement element = TitlesListView.ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as FrameworkElement;
			ScrollToItem(element, withAnimate);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/misc/animategrouptitle.xaml", UriKind.Relative);
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
			Root = (AnimateGroupTitle)target;
			break;
		case 2:
			SelectedBorder = (Border)target;
			break;
		case 3:
			SelectedTransform = (TranslateTransform)target;
			break;
		case 4:
			TitlesListView = (ItemsControl)target;
			TitlesListView.SizeChanged += TitlesListView_SizeChanged;
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
		if (connectionId == 5)
		{
			((Grid)target).MouseLeftButtonUp += OnItemClick;
		}
	}
}
