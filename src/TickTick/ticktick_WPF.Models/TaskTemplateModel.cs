using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Models;

public class TaskTemplateModel : BaseModel
{
	private List<string> _items;

	private List<string> _tags;

	private List<TaskTemplateModel> _children;

	[NotNull]
	[DefaultValue("\"Task\"")]
	[JsonIgnore]
	public string Kind { get; set; }

	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("title")]
	public string Title { get; set; }

	[JsonProperty("content")]
	public string Content { get; set; }

	[JsonProperty("desc")]
	public string Desc { get; set; }

	[JsonProperty("createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime CreatedTime { get; set; }

	[JsonProperty("etag")]
	public string Etag { get; set; }

	[JsonProperty("order")]
	public long? Order { get; set; }

	[Ignore]
	[JsonProperty("children")]
	public List<TaskTemplateModel> Chidlren
	{
		get
		{
			return _children;
		}
		set
		{
			_children = value;
			ChidlrenToString = ((value != null) ? JsonConvert.SerializeObject(value) : "[]");
		}
	}

	[JsonIgnore]
	public string ChidlrenToString { get; set; }

	[Ignore]
	[JsonProperty("items")]
	public List<string> Items
	{
		get
		{
			return _items;
		}
		set
		{
			_items = value;
			ItemsToString = ((value != null) ? JsonConvert.SerializeObject(value) : "[]");
		}
	}

	[JsonIgnore]
	public string ItemsToString { get; set; }

	[Ignore]
	[JsonProperty("tags")]
	public List<string> Tags
	{
		get
		{
			return _tags;
		}
		set
		{
			_tags = TagSerializer.NormalizeLabels(value);
			TagsToString = ((_tags != null) ? JsonConvert.SerializeObject(_tags) : "[]");
		}
	}

	[JsonIgnore]
	public string TagsToString { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	public bool IsNote => Kind == TemplateKind.Note.ToString();

	public TaskTemplateModel()
	{
	}

	public TaskTemplateModel(TaskModel task, List<TaskDetailItemModel> checkItems)
	{
		Id = Utils.GetGuid();
		Title = task.title?.Trim();
		if (task.kind == "TEXT" || task.kind == "NOTE")
		{
			string text = TaskUtils.ReplaceAttachmentTextInString(task.content?.Trim(), withTypeName: false);
			Content = ((text == null) ? null : MarkdownUtils.RemoveFoldedMetadataComments(text));
			Kind = ((task.kind == "NOTE") ? TemplateKind.Note.ToString() : TemplateKind.Task.ToString());
		}
		else
		{
			Desc = ((task.desc == null) ? null : MarkdownUtils.RemoveFoldedMetadataComments(task.desc.Trim()));
			if (checkItems != null)
			{
				List<TaskDetailItemModel> list = checkItems.ToList();
				list.Sort((TaskDetailItemModel a, TaskDetailItemModel b) => a.sortOrder.CompareTo(b.sortOrder));
				Items = list.Select((TaskDetailItemModel i) => i.title).ToList();
			}
			else
			{
				Items = new List<string> { "" };
			}
			Kind = TemplateKind.Task.ToString();
		}
		Tags = ((task.tag == null) ? null : JsonConvert.DeserializeObject<List<string>>(task.tag));
		CreatedTime = DateTime.Now;
		UserId = LocalSettings.Settings.LoginUserId;
	}

	public static List<TaskTemplateModel> GetDefaultNoteTemp()
	{
		return new List<TaskTemplateModel>
		{
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("ReviewTemplateTitle"),
				Content = Utils.GetString("ReviewTemplateContent"),
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-6.0),
				Items = null,
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Note.ToString()
			},
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("ReadingTemplateTitle"),
				Content = Utils.GetString("ReadingTemplateContent"),
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-5.0),
				Items = null,
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Note.ToString()
			},
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("MeetingTemplateTitle"),
				Content = Utils.GetString("MeetingTemplateContent"),
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-4.0),
				Items = null,
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Note.ToString()
			}
		};
	}

	public static List<TaskTemplateModel> GetDefaultTaskTemp()
	{
		return new List<TaskTemplateModel>
		{
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("TripTemplateTitle"),
				Content = null,
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-3.0),
				Items = new List<string>
				{
					Utils.GetString("TripTemplateItem01"),
					Utils.GetString("TripTemplateItem02"),
					Utils.GetString("TripTemplateItem03"),
					Utils.GetString("TripTemplateItem04"),
					Utils.GetString("TripTemplateItem05"),
					Utils.GetString("TripTemplateItem06"),
					Utils.GetString("TripTemplateItem07"),
					Utils.GetString("TripTemplateItem08"),
					Utils.GetString("TripTemplateItem09"),
					Utils.GetString("TripTemplateItem10"),
					Utils.GetString("TripTemplateItem11"),
					Utils.GetString("TripTemplateItem12"),
					Utils.GetString("TripTemplateItem13"),
					Utils.GetString("TripTemplateItem14"),
					Utils.GetString("TripTemplateItem15"),
					Utils.GetString("TripTemplateItem16"),
					Utils.GetString("TripTemplateItem17"),
					Utils.GetString("TripTemplateItem18")
				},
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Task.ToString()
			},
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("DayTemplateTitle"),
				Content = Utils.GetString("DayTemplateContent"),
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-2.0),
				Items = null,
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Task.ToString()
			},
			new TaskTemplateModel
			{
				Id = Utils.GetGuid(),
				Title = Utils.GetString("WorkTemplateTitle"),
				Content = null,
				Desc = null,
				CreatedTime = DateTime.Now.AddMinutes(-1.0),
				Items = new List<string>
				{
					Utils.GetString("WorkTemplateItem01"),
					Utils.GetString("WorkTemplateItem02"),
					Utils.GetString("WorkTemplateItem03"),
					Utils.GetString("WorkTemplateItem04"),
					Utils.GetString("WorkTemplateItem05"),
					Utils.GetString("WorkTemplateItem06"),
					Utils.GetString("WorkTemplateItem07")
				},
				Tags = null,
				UserId = LocalSettings.Settings.LoginUserId,
				Kind = TemplateKind.Task.ToString()
			}
		};
	}

	public static async Task<TaskTemplateModel> Build(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		List<TaskDetailItemModel> list = await TaskDetailItemDao.GetCheckItemsByTaskId(taskId);
		if (task == null || (string.IsNullOrEmpty(task.title) && string.IsNullOrEmpty(task.content) && string.IsNullOrEmpty(task.desc) && list.All((TaskDetailItemModel t) => string.IsNullOrEmpty(t.title))))
		{
			return null;
		}
		TaskTemplateModel template = new TaskTemplateModel(task, list);
		Node<TaskBaseViewModel> taskNode = TaskCache.GetTaskNode(task.id, task.projectId);
		TaskTemplateModel taskTemplateModel = template;
		taskTemplateModel.Chidlren = await GetTemplateChildren(template, taskNode);
		return template;
	}

	private static async Task<List<TaskTemplateModel>> GetTemplateChildren(TaskTemplateModel templateModel, Node<TaskBaseViewModel> node)
	{
		if (node != null && node.Children?.Count > 0)
		{
			IOrderedEnumerable<Node<TaskBaseViewModel>> orderedEnumerable = node.Children.OrderBy((Node<TaskBaseViewModel> n) => n.Value?.SortOrder);
			List<TaskTemplateModel> templateChildren = new List<TaskTemplateModel>();
			foreach (Node<TaskBaseViewModel> child in orderedEnumerable)
			{
				TaskModel task = await TaskDao.GetTaskById(child.Value?.Id);
				if (task != null && (task.status == 0 || string.IsNullOrEmpty(task.repeatTaskId)))
				{
					TaskTemplateModel template = new TaskTemplateModel(task, await TaskDetailItemDao.GetCheckItemsByTaskId(child.NodeId));
					TaskTemplateModel taskTemplateModel = template;
					taskTemplateModel._children = await GetTemplateChildren(template, child);
					templateChildren.Add(template);
				}
			}
			return templateChildren;
		}
		return null;
	}
}
