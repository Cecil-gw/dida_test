using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Models;

public class TemplateViewModel : SortableListItemViewModel, ICloneable
{
	private TaskTemplateModel _model;

	private string _title;

	private string _content;

	private bool _openOption;

	private string _desc;

	private List<string> _items;

	public List<string> Tags { get; set; }

	public bool IsList { get; set; }

	public bool WithChild
	{
		get
		{
			List<TemplateChildViewModel> children = Children;
			if (children == null)
			{
				return false;
			}
			return children.Count > 0;
		}
	}

	public DateTime CreatedTime { get; set; }

	public List<TemplateChildViewModel> Children { get; set; }

	public bool HasOrder { get; set; }

	public bool ShowContent
	{
		get
		{
			if (!IsList)
			{
				return !string.IsNullOrEmpty(Content?.Trim());
			}
			return false;
		}
	}

	public bool ShowTag
	{
		get
		{
			List<string> tags = Tags;
			if (tags == null)
			{
				return false;
			}
			return tags.Count > 0;
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public string Desc
	{
		get
		{
			return _desc;
		}
		set
		{
			_desc = value;
			OnPropertyChanged("Desc");
		}
	}

	public string Content
	{
		get
		{
			return _content;
		}
		set
		{
			_content = value;
			OnPropertyChanged("Content");
		}
	}

	public List<string> Items
	{
		get
		{
			return _items;
		}
		set
		{
			_items = value;
			OnPropertyChanged("Items");
		}
	}

	public bool OpenOption
	{
		get
		{
			return _openOption;
		}
		set
		{
			_openOption = value;
			OnPropertyChanged("OpenOption");
		}
	}

	public TemplateViewModel()
	{
	}

	private TemplateViewModel(TaskTemplateModel model)
	{
		_model = model;
		base.Id = model.Id;
		Title = model.Title;
		Content = model.Content;
		Desc = model.Desc;
		Items = model.Items?.ToList();
		Tags = model.Tags?.ToList();
		IsList = Items != null || !string.IsNullOrEmpty(Desc);
		CreatedTime = model.CreatedTime;
		Children = BuildChildren(model.Chidlren);
		base.SortOrder = model.Order.GetValueOrDefault();
		HasOrder = model.Order.HasValue;
	}

	public override void SaveSortOrder()
	{
		if (_model != null)
		{
			_model.Order = base.SortOrder;
			HasOrder = true;
			TemplateDao.UpdateTemplate(_model);
		}
	}

	public static void SaveSortOrders(IEnumerable<TemplateViewModel> templates)
	{
		if (templates == null)
		{
			return;
		}
		List<TaskTemplateModel> list = new List<TaskTemplateModel>();
		foreach (TemplateViewModel template in templates)
		{
			if (template?._model != null)
			{
				template._model.Order = template.SortOrder;
				template.HasOrder = true;
				list.Add(template._model);
			}
		}
		if (list.Count > 0)
		{
			TemplateDao.UpdateTemplates(list);
		}
	}

	public object Clone()
	{
		return new TemplateViewModel
		{
			Id = base.Id,
			Title = Title,
			Content = Content,
			Desc = Desc,
			Items = Items?.ToList(),
			Tags = Tags?.ToList(),
			IsList = IsList,
			CreatedTime = CreatedTime,
			Children = Children?.Select((TemplateChildViewModel c) => new TemplateChildViewModel
			{
				Id = c.Id,
				Title = c.Title,
				Level = c.Level,
				IsTextMode = c.IsTextMode
			}).ToList(),
			SortOrder = base.SortOrder,
			HasOrder = HasOrder,
			_model = _model
		};
	}

	private static List<TemplateChildViewModel> BuildChildren(List<TaskTemplateModel> templateChildren)
	{
		List<TemplateChildViewModel> children;
		if (templateChildren != null)
		{
			children = new List<TemplateChildViewModel>();
			templateChildren.ForEach(delegate(TaskTemplateModel child)
			{
				GetChildren(child, children, -1);
			});
			return children;
		}
		return null;
		void GetChildren(TaskTemplateModel templateChild, List<TemplateChildViewModel> childViewModels, int parentLevel)
		{
			TemplateChildViewModel obj = new TemplateChildViewModel
			{
				Id = templateChild.Id,
				Title = templateChild.Title,
				Level = parentLevel + 1
			};
			List<string> items = templateChild.Items;
			obj.IsTextMode = items == null || items.Count <= 0;
			TemplateChildViewModel item = obj;
			childViewModels.Add(item);
			templateChild.Chidlren?.ForEach(delegate(TaskTemplateModel child)
			{
				GetChildren(child, children, Math.Min(2, parentLevel + 1));
			});
		}
	}

	public static List<TemplateViewModel> Build(List<TaskTemplateModel> models)
	{
		return models.Select((TaskTemplateModel m) => new TemplateViewModel(m)).ToList();
	}
}
