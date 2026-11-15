using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Detail;

public class CourseDetailControl : Grid, IComponentConnector
{
	internal Border BackBtn;

	internal ListView CourseItems;

	private bool _contentLoaded;

	public string CourseId { get; set; }

	public event EventHandler HideDetail;

	public CourseDetailControl()
	{
		InitializeComponent();
	}

	public async void NavigateCourse(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			int num = id.IndexOf("_", StringComparison.Ordinal);
			if (num > 0)
			{
				id = id.Substring(0, num);
			}
			CourseId = id;
			CourseDetailViewModel courseDetailViewModel = await CourseDetailViewModel.GetDetailViewModelById(id);
			if (courseDetailViewModel != null)
			{
				base.DataContext = courseDetailViewModel;
			}
			if (courseDetailViewModel != null && courseDetailViewModel.Items?.Count > 0)
			{
				CourseItems.ScrollIntoView(courseDetailViewModel.Items[0]);
			}
		}
	}

	private void OnBackClick(object sender, MouseButtonEventArgs e)
	{
		this.HideDetail?.Invoke(this, null);
	}

	public void Reload()
	{
		NavigateCourse(CourseId);
	}

	public void ClearEvent()
	{
		this.HideDetail = null;
	}

	public void ShowBackMenu()
	{
		BackBtn.Visibility = Visibility.Visible;
	}

	public void HideBackMenu()
	{
		BackBtn.Visibility = Visibility.Collapsed;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/detail/coursedetailcontrol.xaml", UriKind.Relative);
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
			BackBtn = (Border)target;
			BackBtn.MouseLeftButtonUp += OnBackClick;
			break;
		case 2:
			CourseItems = (ListView)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
