using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class OrAndNotSelector : StackPanel, IComponentConnector
{
	internal Grid OrGrid;

	internal Border OrBorder;

	internal Grid AndGrid;

	internal Border AndBorder;

	internal Grid NotGrid;

	private bool _contentLoaded;

	public event EventHandler<LogicType> OnLogicChanged;

	public OrAndNotSelector()
	{
		InitializeComponent();
	}

	private void SelectOrLogic(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FilterConditionViewModel filterConditionViewModel)
		{
			filterConditionViewModel.Logic = LogicType.Or;
			this.OnLogicChanged?.Invoke(this, LogicType.Or);
		}
	}

	private void SelectAndLogic(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FilterConditionViewModel filterConditionViewModel)
		{
			if (filterConditionViewModel.AllLogicEnabled)
			{
				filterConditionViewModel.Logic = LogicType.And;
				this.OnLogicChanged?.Invoke(this, LogicType.And);
			}
			else
			{
				Utils.Toast(Utils.GetString("CannotSelectNoTagAndLogicAndAtTheSameTime"));
			}
		}
	}

	private void SelectNotLogic(object sender, MouseButtonEventArgs e)
	{
		if (base.DataContext is FilterConditionViewModel filterConditionViewModel)
		{
			if (filterConditionViewModel.WithTagsSelected && filterConditionViewModel.WithoutTagsSelected)
			{
				Utils.Toast(Utils.GetString("CannotSelectWithTagNoTagAndLogicNotAtTheSameTime"));
				return;
			}
			filterConditionViewModel.Logic = LogicType.Not;
			this.OnLogicChanged?.Invoke(this, LogicType.Not);
		}
	}

	private void OnDataBind(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is FilterConditionViewModel filterConditionViewModel)
		{
			bool flag = filterConditionViewModel.SupportedLogic.Contains(LogicType.And);
			bool flag2 = filterConditionViewModel.SupportedLogic.Contains(LogicType.Not);
			int num = ((!(flag || flag2)) ? 4 : 0);
			OrBorder.CornerRadius = new CornerRadius(4.0, num, num, 4.0);
			if (flag)
			{
				int num2 = ((!flag2) ? 4 : 0);
				AndGrid.Visibility = Visibility.Visible;
				AndBorder.CornerRadius = new CornerRadius(0.0, num2, num2, 0.0);
			}
			if (flag2)
			{
				NotGrid.Visibility = Visibility.Visible;
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/filter/orandnotselector.xaml", UriKind.Relative);
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
			((OrAndNotSelector)target).DataContextChanged += OnDataBind;
			break;
		case 2:
			OrGrid = (Grid)target;
			OrGrid.MouseLeftButtonUp += SelectOrLogic;
			break;
		case 3:
			OrBorder = (Border)target;
			break;
		case 4:
			AndGrid = (Grid)target;
			AndGrid.MouseLeftButtonUp += SelectAndLogic;
			break;
		case 5:
			AndBorder = (Border)target;
			break;
		case 6:
			NotGrid = (Grid)target;
			NotGrid.MouseLeftButtonUp += SelectNotLogic;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
