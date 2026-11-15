using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Service;

namespace ticktick_WPF.ViewModels;

public class CourseDetailViewModel : BaseViewModel
{
	public string Title { get; set; }

	public string Color { get; set; }

	public List<CourseItemViewModel> Items { get; set; }

	public string ScheduleName { get; set; }

	public static async Task<CourseDetailViewModel> Build(CourseDisplayModel model)
	{
		CourseDetailViewModel result = new CourseDetailViewModel
		{
			Title = model.Title,
			Color = (string.IsNullOrEmpty(model.Color) ? "#FF4772FA" : model.Color)
		};
		CourseModel courseModel = await ScheduleDao.GetCoursedByIdAsync(model.Id);
		if (!string.IsNullOrEmpty(courseModel?.ItemsStr))
		{
			List<CourseDetailModel> items = ScheduleService.GetCourseDetailModelsFromJson(courseModel.ItemsStr);
			if (items == null || items.Count == 0)
			{
				return result;
			}
			result.Items = items.Select((CourseDetailModel item) => new CourseItemViewModel(item, (items.Count > 1) ? (items.IndexOf(item) + 1) : 0)).ToList();
		}
		return result;
	}

	public static async Task<CourseDetailViewModel> GetDetailViewModelById(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		int num = id.IndexOf("_", StringComparison.Ordinal);
		if (num > 0)
		{
			id = id.Substring(0, num);
		}
		CourseModel courseModel = await ScheduleDao.GetCoursedByIdAsync(id);
		if (courseModel != null)
		{
			CourseDetailViewModel result = new CourseDetailViewModel
			{
				Title = courseModel.Name,
				Color = (string.IsNullOrEmpty(courseModel.Color) ? "#FF4772FA" : courseModel.Color)
			};
			if (!string.IsNullOrEmpty(courseModel?.ItemsStr))
			{
				List<CourseDetailModel> items = ScheduleService.GetCourseDetailModelsFromJson(courseModel.ItemsStr);
				if (items == null || items.Count == 0)
				{
					return result;
				}
				result.Items = items.Select((CourseDetailModel item) => new CourseItemViewModel(item, (items.Count > 1) ? (items.IndexOf(item) + 1) : 0)).ToList();
			}
			result.ScheduleName = (await ScheduleDao.GetScheduleByIdAsync(courseModel.ScheduleId))?.Name;
			return result;
		}
		return null;
	}
}
