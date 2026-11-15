using System.Collections.Generic;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class DisplayItemNode : Node<DisplayItemModel>
{
	public DisplayItemNode(DisplayItemModel model)
	{
		Value = model;
		NodeId = model.Id;
		ParentId = model.ParentId;
		ProjectId = model.ProjectId;
		Children = new List<Node<DisplayItemModel>>();
	}

	protected override void GetLevel()
	{
		Level = GetLevel(0);
		Value.Level = Level;
		if (Level >= 5)
		{
			Node<DisplayItemModel> node = FindValidParent(this);
			if (node != null && node != Parent && node != this)
			{
				TaskDao.UpdateParent(NodeId, node.NodeId).ContinueWith(UtilRun.LogTask);
			}
		}
	}

	public override int GetLevel(int i)
	{
		if (Parent != null)
		{
			i++;
			return Parent.GetLevel(i);
		}
		return i;
	}

	protected override void SetValueChildren(Node<DisplayItemModel> child)
	{
		if (Value.Children == null)
		{
			Value.Children = new List<DisplayItemModel> { child.Value };
		}
		else if (!Value.Children.Contains(child.Value))
		{
			Value.Children.Add(child.Value);
		}
		child.Value.Parent = Value;
	}

	protected override void ClearValueChildren()
	{
		Value.Children?.Clear();
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
