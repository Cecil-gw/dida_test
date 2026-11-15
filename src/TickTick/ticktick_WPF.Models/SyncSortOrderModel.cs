using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class SyncSortOrderModel : BaseModel
{
	private string _userId;

	private string _groupId = "all";

	public string UserId
	{
		get
		{
			return _userId;
		}
		set
		{
			_userId = StringPool.GetOrCreate(value);
		}
	}

	public string EntityId { get; set; }

	[NotNull]
	public long SortOrder { get; set; }

	[NotNull]
	public int Type { get; set; }

	[JsonIgnore]
	public int SyncStatus { get; set; }

	[NotNull]
	[Indexed]
	public string SortOrderType { get; set; }

	public int Deleted { get; set; }

	public string ColumnId { get; set; } = "";

	[NotNull]
	public string GroupId
	{
		get
		{
			return _groupId;
		}
		set
		{
			_groupId = StringPool.GetOrCreate(value);
		}
	}

	public SyncSortOrderModel()
	{
		UserId = LocalSettings.Settings.LoginUserId;
	}

	public SyncSortOrderModel(string type)
	{
		SortOrderType = type;
		Type = 1;
		UserId = LocalSettings.Settings.LoginUserId;
	}

	public static void Copy(SyncSortOrderModel dest, SyncSortOrderModel from)
	{
		dest.UserId = from.UserId;
		dest.EntityId = from.EntityId;
		dest.SortOrder = from.SortOrder;
		dest.Type = from.Type;
		dest.SyncStatus = from.SyncStatus;
		dest.SortOrderType = from.SortOrderType;
		dest.Deleted = from.Deleted;
		dest.GroupId = from.GroupId;
	}

	public static SyncSortOrderModel Build(string type, TaskSortOrderInDateModel model)
	{
		return new SyncSortOrderModel(type)
		{
			Type = model.type,
			SyncStatus = 3,
			SortOrder = model.sortOrder,
			EntityId = model.taskid,
			GroupId = model.projectid
		};
	}

	public static SyncSortOrderModel Build(string type, TaskSortOrderInProjectModel model)
	{
		return new SyncSortOrderModel(type)
		{
			Type = EntityType.GetEntityTypeNum(model.EntityType),
			SyncStatus = 3,
			SortOrder = model.SortOrder,
			EntityId = model.EntityId,
			GroupId = model.ProjectId
		};
	}

	public static SyncSortOrderModel Build(string type, TaskSortOrderInPriorityModel model)
	{
		return new SyncSortOrderModel(type)
		{
			Type = EntityType.GetEntityTypeNum(model.EntityType),
			SyncStatus = 3,
			SortOrder = model.SortOrder,
			EntityId = model.EntityId,
			GroupId = model.CatId
		};
	}

	public static SyncSortOrderModel Build(string type, SyncSortOrderModel model)
	{
		return new SyncSortOrderModel(type)
		{
			Type = model.Type,
			SyncStatus = 3,
			SortOrder = model.SortOrder,
			EntityId = model.EntityId,
			GroupId = model.GroupId
		};
	}
}
