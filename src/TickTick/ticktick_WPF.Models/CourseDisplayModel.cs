using System;
using System.Collections.Generic;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class CourseDisplayModel
{
	public string Title { get; set; }

	public string Id { get; set; }

	public string ScheduleId { get; set; }

	public string Color { get; set; }

	public DateTime CourseStart { get; set; }

	public DateTime CourseEnd { get; set; }

	public string Room { get; set; }

	public string Teacher { get; set; }

	public int StartLesson { get; set; }

	public int EndLesson { get; set; }

	public List<string> Reminders { get; set; }

	public string ScheduleName { get; set; }

	public bool Archived { get; set; }

	public int Index { get; set; }

	public string UniqueId => Id + "_" + DateUtils.GetDateNum(CourseStart) + "_" + StartLesson + "_" + EndLesson;

	public CourseDisplayModel(CourseModel course, DateTime start, DateTime end)
	{
		Title = course.Name;
		Id = course.Id;
		ScheduleId = course.ScheduleId;
		Color = (string.IsNullOrEmpty(course.Color) ? "#FF4772FA" : course.Color);
		CourseStart = start;
		CourseEnd = end;
	}

	public static CourseDisplayModel Build(CourseDisplayModel model)
	{
		return model;
	}
}
