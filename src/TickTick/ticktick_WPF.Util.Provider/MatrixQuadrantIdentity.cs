using System;
using System.Collections.Generic;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util.Provider;

public class MatrixQuadrantIdentity : ProjectIdentity
{
	public QuadrantModel Quadrant { get; }

	public string MatrixProfileId { get; }

	public override string CatId => Quadrant?.id + "_" + MatrixProfileId;

	public override string SortProjectId => CatId;

	public override string Id => Quadrant?.id;

	public override string QueryId => Quadrant?.id;

	public override bool LoadAll => false;

	public override bool IsNote => TaskDefault?.IsNote ?? false;

	private FilterTaskDefault TaskDefault { get; }

	public MatrixQuadrantIdentity(QuadrantModel quadrant, string matrixProfileId)
	{
		if (string.IsNullOrEmpty(matrixProfileId))
		{
			throw new ArgumentException("Matrix profile ID is required.", "matrixProfileId");
		}
		Quadrant = quadrant;
		MatrixProfileId = matrixProfileId;
		base.SortOption = Quadrant.GetSortOption();
		TaskDefault = FilterViewModel.CalculateTaskDefault(quadrant.rule);
		IsNote = TaskDefault.IsNote;
	}

	public override string GetProjectId()
	{
		return TaskDefault.ProjectModel.id;
	}

	public override string GetProjectName()
	{
		return TaskDefault.ProjectModel.name;
	}

	public override bool GetForceShowProjectName()
	{
		return true;
	}

	public override bool GetIsNote()
	{
		return TaskDefault.IsNote;
	}

	public override TimeData GetTimeData()
	{
		if (!GetIsNote())
		{
			return new TimeData
			{
				StartDate = TaskDefault.DefaultDate,
				Reminders = TimeData.GetDefaultAllDayReminders(),
				IsDefault = true,
				IsAllDay = true
			};
		}
		return new TimeData();
	}

	public int? GetDefaultPriority()
	{
		return TaskDefault.Priority;
	}

	public override int GetPriority()
	{
		return TaskDefault.Priority ?? TaskDefaultDao.GetDefaultSafely().Priority;
	}

	public override List<string> GetTags()
	{
		return TaskDefault.DefaultTags;
	}

	public override string GetAssignee()
	{
		return TaskDefault.DefaultAssignee;
	}

	public override bool UseDefaultTags()
	{
		return false;
	}

	public override ProjectIdentity Copy(ProjectIdentity project)
	{
		MatrixQuadrantIdentity matrixQuadrantIdentity = (MatrixQuadrantIdentity)project;
		return new MatrixQuadrantIdentity(matrixQuadrantIdentity.Quadrant, matrixQuadrantIdentity.MatrixProfileId);
	}
}
