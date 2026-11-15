using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class TaskNode : Node<TaskBaseViewModel>
{
	public TaskNode(TaskBaseViewModel task)
	{
		Value = task;
		NodeId = task.Id;
		ParentId = task.ParentId;
		ProjectId = task.ProjectId;
		Children = new List<Node<TaskBaseViewModel>>();
	}

	public override Node<DisplayItemModel> ToDisplayItemNode(string parentId)
	{
		DisplayItemModel model = new DisplayItemModel(Value);
		DisplayItemNode node = new DisplayItemNode(model);
		node.Children = Children?.Where((Node<TaskBaseViewModel> n) => n.NodeId != parentId).Select(delegate(Node<TaskBaseViewModel> c)
		{
			Node<DisplayItemModel> node2 = c.ToDisplayItemNode(parentId);
			node2.Parent = node;
			return node2;
		}).ToList();
		return node;
	}

	public override bool ExistParent(string parentId)
	{
		if (base.HasParent)
		{
			if (!(ParentId == parentId))
			{
				return Parent.ExistParent(parentId);
			}
			return true;
		}
		return false;
	}
}
