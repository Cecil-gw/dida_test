using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Models;

public class Node<T> where T : INode
{
	public string NodeId;

	public string ProjectId;

	public List<Node<T>> Children;

	public Node<T> Parent;

	public int Level;

	public T Value;

	public string ParentId;

	public bool HasParent => Parent != null;

	public List<T> GetAllChildrenValue()
	{
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		ClearValueChildren();
		List<Node<T>> children = Children;
		if (children != null && children.Count > 0)
		{
			foreach (Node<T> child in Children)
			{
				SetValueChildren(child);
				GetChildrenValue(child, dictionary);
			}
		}
		return dictionary.Values.ToList();
	}

	protected virtual void ClearValueChildren()
	{
	}

	public virtual bool ExistParent(string parentId)
	{
		return false;
	}

	protected Node<T> FindValidParent(Node<T> node)
	{
		if (!node.HasParent || node.Parent == null)
		{
			return node;
		}
		if (node.Level >= 4)
		{
			return FindValidParent(node.Parent);
		}
		return node;
	}

	private void GetChildrenValue(Node<T> node, Dictionary<string, T> dict)
	{
		if (dict.ContainsKey(node.NodeId))
		{
			TaskDao.UpdateParent(node.NodeId, "", needSync: false);
			return;
		}
		dict.Add(node.NodeId, node.Value);
		node.GetLevel();
		node.ClearValueChildren();
		List<Node<T>> children = node.Children;
		if (children == null || children.Count <= 0)
		{
			return;
		}
		node.ClearValueChildren();
		foreach (Node<T> child in node.Children)
		{
			node.SetValueChildren(child);
			GetChildrenValue(child, dict);
		}
	}

	protected virtual void SetValueChildren(Node<T> child)
	{
	}

	public List<Node<T>> GetAllChildrenNode()
	{
		Dictionary<string, Node<T>> dictionary = new Dictionary<string, Node<T>>();
		List<Node<T>> children = Children;
		if (children != null && children.Count > 0)
		{
			foreach (Node<T> child in Children)
			{
				GetChildrenNode(child, dictionary);
			}
		}
		return dictionary.Values.ToList();
	}

	private void GetChildrenNode(Node<T> node, Dictionary<string, Node<T>> dict)
	{
		if (dict.ContainsKey(node.NodeId))
		{
			return;
		}
		dict.Add(node.NodeId, node);
		List<Node<T>> children = node.Children;
		if (children == null || children.Count <= 0)
		{
			return;
		}
		foreach (Node<T> child in node.Children)
		{
			GetChildrenNode(child, dict);
		}
	}

	protected virtual void GetLevel()
	{
		Level = GetLevel(0);
	}

	public virtual int GetLevel(int i)
	{
		if (i > 4)
		{
			return 4;
		}
		if (Parent != null)
		{
			i++;
			return Parent.GetLevel(i);
		}
		return i;
	}

	public virtual Node<DisplayItemModel> ToDisplayItemNode(string parentId)
	{
		return null;
	}
}
