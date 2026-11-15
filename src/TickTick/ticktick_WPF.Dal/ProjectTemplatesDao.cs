using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Entity.Dto;
using ticktick_WPF.Models;

namespace ticktick_WPF.Dal;

internal class ProjectTemplatesDao : BaseDao<ProjectTemplatesModel>
{
	public static async Task<long> GetLastestTimeStamp()
	{
		return (await App.Connection.Table<ProjectTemplatesModel>().FirstOrDefaultAsync())?.Timestamp ?? 0;
	}

	public static async Task<ProjectTemplatesModel> GetFirst()
	{
		return await App.Connection.Table<ProjectTemplatesModel>().FirstOrDefaultAsync();
	}

	public static async Task AddOrUpdate(ProjectTemplatesDto dto)
	{
		ProjectTemplatesModel projectTemplatesModel = await App.Connection.Table<ProjectTemplatesModel>().FirstOrDefaultAsync();
		if (projectTemplatesModel != null)
		{
			if (projectTemplatesModel.Timestamp != dto.Timestamp)
			{
				projectTemplatesModel.UpdateTime = DateTime.Now;
				projectTemplatesModel.Timestamp = dto.Timestamp;
				if (dto.ProjectTemplates != null)
				{
					projectTemplatesModel.ProjectTemplates = dto.ProjectTemplates;
				}
				await BaseDao<ProjectTemplatesModel>.UpdateAsync(projectTemplatesModel);
			}
		}
		else
		{
			await BaseDao<ProjectTemplatesModel>.InsertAsync(new ProjectTemplatesModel
			{
				UpdateTime = DateTime.Now,
				Timestamp = dto.Timestamp,
				ProjectTemplates = dto.ProjectTemplates
			});
		}
	}

	public static async Task<List<ProjectTemplate>> GetAllTemplates()
	{
		ProjectTemplatesModel projectTemplatesModel = await App.Connection.Table<ProjectTemplatesModel>().FirstOrDefaultAsync();
		if (projectTemplatesModel == null)
		{
			return null;
		}
		if (!projectTemplatesModel.ProjectTemplates.Any())
		{
			return null;
		}
		return projectTemplatesModel.ProjectTemplates.Where((ProjectTemplate x) => x.Deleted == 0).ToList();
	}
}
