using System;
using System.Windows;
using System.Windows.Controls;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.MainListView.TaskList;

public class TaskItemTemplateSelector : DataTemplateSelector
{
	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		if (item is DisplayItemModel displayItemModel && container is FrameworkElement frameworkElement)
		{
			try
			{
				if (displayItemModel.IsLoadMore)
				{
					return frameworkElement.FindResource("LoadMoreTemplate") as DataTemplate;
				}
				if (displayItemModel.AddViewModel != null)
				{
					return frameworkElement.FindResource("AddViewTemplate") as DataTemplate;
				}
				if (displayItemModel.IsSection)
				{
					if (displayItemModel.Section.Customized)
					{
						return frameworkElement.FindResource("CustomSectionTemplate") as DataTemplate;
					}
					return frameworkElement.FindResource("SectionTemplate") as DataTemplate;
				}
				if (displayItemModel.IsNormalItem)
				{
					if (displayItemModel.InDetail)
					{
						return frameworkElement.FindResource("SubTaskItemTemplate") as DataTemplate;
					}
					if (displayItemModel.InKanban)
					{
						return frameworkElement.FindResource("KanbanItemTemplate") as DataTemplate;
					}
					if (displayItemModel.InMatrix)
					{
						return frameworkElement.FindResource("MatrixItemTemplate") as DataTemplate;
					}
					if (!displayItemModel.HitVisible)
					{
						return frameworkElement.FindResource("PreviewItem") as DataTemplate;
					}
					return frameworkElement.FindResource("TaskItemTemplate") as DataTemplate;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
		return null;
	}
}
