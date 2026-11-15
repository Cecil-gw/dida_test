using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.CustomControl;

namespace ticktick_WPF.Views;

public class DeleteColumnWindow : Window, IOkCancelWindow, IComponentConnector
{
	internal DeleteColumnWindow Root;

	internal RadioButton DeleteRtb;

	internal RadioButton DeleteAllRtb;

	internal RadioButton MoveRtb;

	internal CustomComboBox ColumnSelectComboBox;

	private bool _contentLoaded;

	private string _projectId { get; set; }

	private string _columnId { get; set; }

	private bool _fromList { get; set; }

	public string TargetColumnId { get; set; }

	public bool? removeAll { get; set; }

	public DeleteColumnWindow(string projectId, string columnId, bool fromList = false)
	{
		InitializeComponent();
		_projectId = projectId;
		_columnId = columnId;
		_fromList = fromList;
		LoadData();
	}

	private async void LoadData()
	{
		List<TaskBaseViewModel> source = await TaskCache.GetModelsInProjectColumn(_projectId, _columnId);
		bool flag = source.Any((TaskBaseViewModel x) => x.IsTask && x.Status == 0);
		bool flag2 = source.Any((TaskBaseViewModel x) => x.IsTask && x.Status != 0);
		bool flag3 = source.Any((TaskBaseViewModel x) => x.IsTask);
		bool flag4 = source.Any((TaskBaseViewModel x) => x.IsNote);
		if (_fromList)
		{
			string text = string.Empty;
			string arg = string.Empty;
			if (flag4)
			{
				if (!flag3)
				{
					arg = Utils.GetString("TaskTypeAllNotes");
				}
				else
				{
					if (flag && flag2)
					{
						text = Utils.GetString("TaskTypeUncompletedTasksNotes");
					}
					else if (flag2)
					{
						text = Utils.GetString("task_type_note");
					}
					arg = Utils.GetString("TaskTypeAllTasksNotes");
				}
			}
			else if (flag3)
			{
				if (flag && flag2)
				{
					text = Utils.GetString("task_type_uncompleted_task");
				}
				arg = Utils.GetString("TaskTypeAllTasks");
			}
			if (!string.IsNullOrEmpty(text))
			{
				DeleteRtb.Content = string.Format(Utils.GetString("delete_column_delete_task"), text);
				DeleteRtb.IsChecked = true;
			}
			else
			{
				DeleteRtb.Visibility = Visibility.Collapsed;
				DeleteAllRtb.IsChecked = true;
			}
			DeleteAllRtb.Content = string.Format(Utils.GetString("delete_column_delete_task"), arg);
			MoveRtb.Content = string.Format(Utils.GetString("DeleteOnlySectionAndMove"), arg);
		}
		else
		{
			string text2 = Utils.GetString("task_type_task");
			if (flag3)
			{
				if (flag4)
				{
					text2 = text2 + (Utils.IsZhCn() ? "、" : ",") + Utils.GetString("task_type_note");
				}
			}
			else if (flag4)
			{
				text2 = Utils.GetString("task_type_note");
			}
			RadioButton deleteRtb = DeleteRtb;
			object content = (DeleteAllRtb.Content = string.Format(Utils.GetString("delete_column_delete_task"), text2));
			deleteRtb.Content = content;
			DeleteAllRtb.Visibility = Visibility.Collapsed;
			MoveRtb.Content = string.Format(Utils.GetString("delete_column_move_task"), text2);
			DeleteRtb.IsChecked = true;
		}
		List<ColumnModel> source2 = await ColumnDao.GetColumnsByProjectId(_projectId);
		ObservableCollection<ComboBoxViewModel> observableCollection = new ObservableCollection<ComboBoxViewModel>();
		foreach (ColumnModel item in source2.Where((ColumnModel column) => column.id != _columnId))
		{
			observableCollection.Add(new ComboBoxViewModel(item, item.name));
		}
		ColumnSelectComboBox.ItemsSource = observableCollection;
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		DoDelete();
		Close();
	}

	private async Task DoDelete()
	{
		if (MoveRtb.IsChecked == true)
		{
			if (ColumnSelectComboBox.SelectedItem?.Value is ColumnModel columnModel)
			{
				TargetColumnId = columnModel.id;
			}
			base.DialogResult = true;
		}
		else if (DeleteAllRtb.IsChecked == true)
		{
			TargetColumnId = null;
			removeAll = true;
			base.DialogResult = true;
		}
		else if (DeleteRtb.IsChecked == true)
		{
			TargetColumnId = null;
			removeAll = false;
			base.DialogResult = true;
		}
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		DoDelete();
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/deletecolumnwindow.xaml", UriKind.Relative);
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
			Root = (DeleteColumnWindow)target;
			break;
		case 2:
			DeleteRtb = (RadioButton)target;
			break;
		case 3:
			DeleteAllRtb = (RadioButton)target;
			break;
		case 4:
			MoveRtb = (RadioButton)target;
			break;
		case 5:
			ColumnSelectComboBox = (CustomComboBox)target;
			break;
		case 6:
			((Button)target).Click += OnSaveClick;
			break;
		case 7:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
