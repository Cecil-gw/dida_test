using System;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class QuickSetModel
{
	public QuickSetType Type;

	public string Title;

	public string Tag;

	public string CalId;

	public ProjectModel Project;

	public DateTime? Date;

	public AvatarViewModel Avatar;

	public int Priority;

	public TaskBaseViewModel task;

	public string SplashCommand;

	public string ColumnId { get; set; }

	public string ColumnName { get; set; }
}
