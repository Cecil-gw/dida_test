using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Habit;

public class SetHabitReminderControl : UserControl, IComponentConnector, IStyleConnector
{
	public static readonly DependencyProperty HasActualReminderItemsProperty = DependencyProperty.Register("HasActualReminderItems", typeof(bool), typeof(SetHabitReminderControl), new PropertyMetadata(false));

	internal ItemsControl ReminderItems;

	internal EscPopup TimePopup;

	private bool _contentLoaded;

	public ObservableCollection<HabitReminderViewModel> Items => ReminderItems.ItemsSource as ObservableCollection<HabitReminderViewModel>;

	public bool HasActualReminderItems
	{
		get
		{
			return (bool)GetValue(HasActualReminderItemsProperty);
		}
		private set
		{
			SetValue(HasActualReminderItemsProperty, value);
		}
	}

	public event EventHandler<string> Toast;

	public SetHabitReminderControl()
	{
		InitializeComponent();
	}

	public void Init(string habitReminder)
	{
		List<HabitReminderViewModel> list = new List<HabitReminderViewModel>();
		if (!string.IsNullOrEmpty(habitReminder))
		{
			string[] array = habitReminder.Split(',');
			if (array.Any())
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (text.Contains(":"))
					{
						string[] array3 = text.Split(':');
						if (array3.Length == 2 && int.TryParse(array3[0], out var hour) && int.TryParse(array3[1], out var min) && list.All((HabitReminderViewModel habitReminderViewModel) => habitReminderViewModel.Time.Hour != hour || habitReminderViewModel.Time.Minute != min))
						{
							list.Add(new HabitReminderViewModel
							{
								Time = DateTime.Today.AddHours(hour).AddMinutes(min)
							});
						}
					}
				}
			}
		}
		list.Sort((HabitReminderViewModel l, HabitReminderViewModel r) => l.Time.CompareTo(r.Time));
		list.Add(new HabitReminderViewModel
		{
			IsAdd = true
		});
		SortItems(list);
		ObservableCollection<HabitReminderViewModel> observableCollection = new ObservableCollection<HabitReminderViewModel>(list);
		observableCollection.CollectionChanged += OnReminderItemsChanged;
		ReminderItems.ItemsSource = observableCollection;
		UpdateHasActualReminderItems();
	}

	private void OnReminderItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateHasActualReminderItems();
	}

	private void UpdateHasActualReminderItems()
	{
		HasActualReminderItems = Items?.Any((HabitReminderViewModel i) => !i.IsAdd) ?? false;
	}

	private void AddNewReminderClick(object sender, RoutedEventArgs e)
	{
		if (Items.Count > 10)
		{
			this.Toast?.Invoke(this, Utils.GetString("HabitReminderLimite"));
			return;
		}
		if (sender is Button placementTarget)
		{
			TimePopup.PlacementTarget = placementTarget;
		}
		TimePopup.IsOpen = true;
	}

	private void AddReminder(object sender, DateTime time)
	{
		if (Items.Any((HabitReminderViewModel i) => !Utils.IsEmptyDate(i.Time) && DateUtils.IsEqualTime(time, i.Time)))
		{
			this.Toast?.Invoke(this, Utils.GetString("HabitReminderExist"));
		}
		else
		{
			UserActCollectUtils.AddClickEvent("habit", "add_edit_habit", "add_reminder");
			Items.Insert(GetInsertIndex(time), new HabitReminderViewModel
			{
				Time = time
			});
		}
		TimePopup.IsOpen = false;
	}

	private int GetInsertIndex(DateTime time)
	{
		if (Items.Count > 1)
		{
			for (int i = 0; i < Items.Count - 1; i++)
			{
				if (Items[i].Time > time)
				{
					return i;
				}
			}
		}
		return Math.Max(0, Items.Count - 1);
	}

	private static void SortItems(List<HabitReminderViewModel> items)
	{
		items.Sort(delegate(HabitReminderViewModel a, HabitReminderViewModel b)
		{
			if (b.IsAdd && !a.IsAdd)
			{
				return -1;
			}
			if (a.IsAdd && !b.IsAdd)
			{
				return 1;
			}
			return (a.Time.Hour != b.Time.Hour) ? a.Time.Hour.CompareTo(b.Time.Hour) : a.Time.Minute.CompareTo(b.Time.Minute);
		});
	}

	private void OnReminderChanged(object sender, DateTime time)
	{
		if (!(sender is TimeInputControl timeInputControl))
		{
			return;
		}
		Grid grid = Utils.FindParent<Grid>(timeInputControl);
		if (grid != null && grid.DataContext is HabitReminderViewModel habitReminderViewModel)
		{
			if (Items.Any((HabitReminderViewModel i) => DateUtils.IsEqualTime(time, i.Time)))
			{
				timeInputControl.SetTimeText(habitReminderViewModel.Time);
			}
			else
			{
				habitReminderViewModel.Time = time;
			}
		}
	}

	public string GetReminderString()
	{
		List<string> values = (from r in Items.Where((HabitReminderViewModel i) => !i.IsAdd).ToList()
			select r.Time.ToString("HH':'mm")).ToList();
		string text = string.Join(",", values);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return null;
	}

	private void DeleteReminder(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: HabitReminderViewModel dataContext })
		{
			Items.Remove(dataContext);
		}
	}

	private void OnPopupClosed(object sender, EventArgs e)
	{
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/habit/sethabitremindercontrol.xaml", UriKind.Relative);
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
			ReminderItems = (ItemsControl)target;
			break;
		case 4:
			TimePopup = (EscPopup)target;
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
		switch (connectionId)
		{
		case 2:
			((Border)target).MouseLeftButtonUp += DeleteReminder;
			break;
		case 3:
			((Button)target).Click += AddNewReminderClick;
			break;
		}
	}
}
