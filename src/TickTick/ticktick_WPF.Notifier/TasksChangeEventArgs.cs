using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.Notifier;

public class TasksChangeEventArgs : EventArgs
{
	public BlockingSet<string> DeletedChangedIds = new BlockingSet<string>();

	public BlockingSet<string> UndoDeletedIds = new BlockingSet<string>();

	public BlockingSet<string> StatusChangedIds = new BlockingSet<string>();

	public BlockingSet<string> KindChangedIds = new BlockingSet<string>();

	public BlockingSet<string> PinChangedIds = new BlockingSet<string>();

	public BlockingSet<string> ProjectChangedIds = new BlockingSet<string>();

	public BlockingSet<string> PriorityChangedIds = new BlockingSet<string>();

	public BlockingSet<string> TagChangedIds = new BlockingSet<string>();

	public BlockingSet<string> PomoChangedIds = new BlockingSet<string>();

	public BlockingSet<string> SortOrderChangedIds = new BlockingSet<string>();

	public BlockingSet<string> DateChangedIds = new BlockingSet<string>();

	public BlockingSet<string> AssignChangedIds = new BlockingSet<string>();

	public BlockingSet<string> AttachmentChangedIds = new BlockingSet<string>();

	public BlockingSet<string> TasksOpenChangedIds = new BlockingSet<string>();

	public BlockingSet<string> CheckItemChangedIds = new BlockingSet<string>();

	public BlockingSet<string> TaskTextChangedIds = new BlockingSet<string>();

	public BlockingSet<string> AddIds = new BlockingSet<string>();

	public BlockingSet<string> BatchChangedIds = new BlockingSet<string>();

	public BlockingSet<string> ScrollToBottomIds = new BlockingSet<string>();

	public TasksChangeEventArgs Copy()
	{
		TasksChangeEventArgs e = new TasksChangeEventArgs();
		e.DeletedChangedIds.AddRange(DeletedChangedIds);
		e.UndoDeletedIds.AddRange(UndoDeletedIds);
		e.StatusChangedIds.AddRange(StatusChangedIds);
		e.KindChangedIds.AddRange(KindChangedIds);
		e.PinChangedIds.AddRange(PinChangedIds);
		e.ProjectChangedIds.AddRange(ProjectChangedIds);
		e.PriorityChangedIds.AddRange(PriorityChangedIds);
		e.TagChangedIds.AddRange(TagChangedIds);
		e.PomoChangedIds.AddRange(PomoChangedIds);
		e.SortOrderChangedIds.AddRange(SortOrderChangedIds);
		e.DateChangedIds.AddRange(DateChangedIds);
		e.AssignChangedIds.AddRange(AssignChangedIds);
		e.AttachmentChangedIds.AddRange(AttachmentChangedIds);
		e.TasksOpenChangedIds.AddRange(TasksOpenChangedIds);
		e.CheckItemChangedIds.AddRange(CheckItemChangedIds);
		e.TaskTextChangedIds.AddRange(TaskTextChangedIds);
		e.AddIds.AddRange(AddIds);
		e.BatchChangedIds.AddRange(BatchChangedIds);
		e.ScrollToBottomIds.AddRange(ScrollToBottomIds);
		return e;
	}

	public void Clear()
	{
		DeletedChangedIds.Clear();
		UndoDeletedIds.Clear();
		StatusChangedIds.Clear();
		KindChangedIds.Clear();
		PinChangedIds.Clear();
		ProjectChangedIds.Clear();
		PriorityChangedIds.Clear();
		TagChangedIds.Clear();
		PomoChangedIds.Clear();
		SortOrderChangedIds.Clear();
		DateChangedIds.Clear();
		AssignChangedIds.Clear();
		AttachmentChangedIds.Clear();
		TasksOpenChangedIds.Clear();
		CheckItemChangedIds.Clear();
		TaskTextChangedIds.Clear();
		AddIds.Clear();
		BatchChangedIds.Clear();
		ScrollToBottomIds.Clear();
	}

	public void Merge(TasksChangeEventArgs args)
	{
		DeletedChangedIds.AddRange(args.DeletedChangedIds);
		UndoDeletedIds.AddRange(args.UndoDeletedIds);
		StatusChangedIds.AddRange(args.StatusChangedIds);
		KindChangedIds.AddRange(args.KindChangedIds);
		PinChangedIds.AddRange(args.PinChangedIds);
		ProjectChangedIds.AddRange(args.ProjectChangedIds);
		PriorityChangedIds.AddRange(args.PriorityChangedIds);
		TagChangedIds.AddRange(args.TagChangedIds);
		PomoChangedIds.AddRange(args.PomoChangedIds);
		SortOrderChangedIds.AddRange(args.SortOrderChangedIds);
		DateChangedIds.AddRange(args.DateChangedIds);
		AssignChangedIds.AddRange(args.AssignChangedIds);
		AttachmentChangedIds.AddRange(args.AttachmentChangedIds);
		TasksOpenChangedIds.AddRange(args.TasksOpenChangedIds);
		CheckItemChangedIds.AddRange(args.CheckItemChangedIds);
		TaskTextChangedIds.AddRange(args.TaskTextChangedIds);
		AddIds.AddRange(args.AddIds);
		BatchChangedIds.AddRange(args.BatchChangedIds);
		ScrollToBottomIds.AddRange(args.ScrollToBottomIds);
	}
}
