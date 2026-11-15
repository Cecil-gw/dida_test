using System.Collections.Generic;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Service;

namespace ticktick_WPF.Dal;

public class ArchiveCourseDao : ArchivedDao
{
	public static async Task CheckOldArchiveCourse()
	{
		foreach (ArchivedEventModel arc in await ArchivedDao.GetArchivedModels(ArchiveKind.Course))
		{
			string[] array = arc.Key?.Split('_');
			if (array == null || array.Length != 3)
			{
				continue;
			}
			string id = array[0];
			string date = array[1];
			int result;
			int index = (int.TryParse(array[2], out result) ? result : 0);
			CourseModel courseModel = await ScheduleDao.GetCoursedByIdAsync(id);
			if (courseModel != null)
			{
				List<CourseDetailModel> courseDetailModelsFromJson = ScheduleService.GetCourseDetailModelsFromJson(courseModel.ItemsStr);
				if (courseDetailModelsFromJson == null || courseDetailModelsFromJson.Count <= index)
				{
					await BaseDao<ArchivedEventModel>.DeleteAsync(arc);
					continue;
				}
				CourseDetailModel courseDetailModel = courseDetailModelsFromJson[index];
				arc.Key = id + "_" + date + "_" + courseDetailModel.StartLesson + "_" + courseDetailModel.EndLesson;
				await BaseDao<ArchivedEventModel>.UpdateAsync(arc);
			}
			else
			{
				await BaseDao<ArchivedEventModel>.DeleteAsync(arc);
			}
		}
	}
}
