using System;
using System.Globalization;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class CheckInLogViewModel : BaseHidableViewModel
{
	private string _content;

	private int _score;

	private bool _showEdit;

	public string Color;

	public string IconRes;

	public int Score
	{
		get
		{
			return _score;
		}
		set
		{
			_score = value;
			OnPropertyChanged("Score");
		}
	}

	public string Id { get; set; }

	public DateTime Date { get; set; }

	public string DisplayDate { get; set; }

	public string HabitId { get; set; }

	public string HabitName { get; set; }

	public bool IsBoolHabit { get; set; }

	public Visibility LineVisibility { get; set; }

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
		}
	}

	public bool Completed { get; set; }

	public bool UnCompleted { get; set; }

	public int Status { get; set; }

	public bool ShowEdit
	{
		get
		{
			return _showEdit;
		}
		set
		{
			_showEdit = value;
			OnPropertyChanged("ShowEdit");
		}
	}

	public CheckInLogViewModel()
	{
	}

	public CheckInLogViewModel(HabitRecordModel record, HabitModel habit, bool completed, bool unCompleted, bool showEdit)
	{
		Id = record.Id;
		Content = record.Content;
		Completed = completed;
		UnCompleted = unCompleted;
		Status = ((Completed || UnCompleted) ? 2 : 0);
		IsBoolHabit = habit.IsBoolHabit();
		HabitName = habit.Name;
		HabitId = habit.Id;
		DateTime date = DateTime.ParseExact(record.Stamp.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
		string text = date.ToString("ddd", App.Ci);
		Date = date;
		DisplayDate = (Utils.IsEn() ? (text + ", " + DateUtils.FormatMonthDay(date, DateFormatLen.Middle)) : (DateUtils.FormatMonthDay(date, DateFormatLen.Middle) + " " + text));
		Score = record.Emoji;
		IconRes = habit.IconRes;
		Color = habit.Color;
		ShowEdit = showEdit;
	}

	public CheckInLogViewModel Copy()
	{
		return (CheckInLogViewModel)MemberwiseClone();
	}
}
