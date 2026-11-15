using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Models;

public class MatrixProfileQuadrantModel : BaseModel
{
	[JsonProperty("id")]
	public string id { get; set; }

	[JsonIgnore]
	[Indexed]
	public string userId { get; set; }

	[JsonProperty("matrixId")]
	[Indexed]
	public string matrixSid { get; set; }

	[JsonProperty("quadrant")]
	public int quadrant { get; set; }

	[JsonProperty("name")]
	public string name { get; set; }

	[JsonProperty("rule")]
	public string rule { get; set; }

	[JsonProperty("sortType")]
	public string sortType { get; set; }

	[JsonProperty("groupBy")]
	public string groupBy { get; set; }

	[JsonProperty("orderBy")]
	public string orderBy { get; set; }

	[JsonProperty("order")]
	public string order { get; set; }

	[JsonProperty("sortOrder")]
	public long? sortOrder { get; set; }

	[JsonProperty("createdTime")]
	public DateTime createdTime { get; set; }

	[JsonProperty("modifiedTime")]
	public DateTime modifiedTime { get; set; }

	[JsonProperty("etag")]
	public string etag { get; set; }

	[JsonProperty("deleted")]
	public bool deleted { get; set; }

	[JsonIgnore]
	public string status { get; set; }

	[Ignore]
	[JsonProperty("sortOption")]
	public SortOption SortOption
	{
		get
		{
			if (!string.IsNullOrEmpty(orderBy) || !string.IsNullOrEmpty(groupBy))
			{
				return new SortOption
				{
					groupBy = groupBy,
					orderBy = orderBy,
					order = order
				};
			}
			return null;
		}
		set
		{
			groupBy = value?.groupBy;
			orderBy = value?.orderBy;
			order = value?.order;
		}
	}

	public QuadrantModel ToQuadrantModel()
	{
		return new QuadrantModel
		{
			id = "quadrant" + quadrant,
			name = name,
			rule = rule,
			sortType = sortType,
			sortOrder = sortOrder,
			SortOption = SortOption
		};
	}

	public void Apply(QuadrantModel model)
	{
		if (model != null)
		{
			name = model.name;
			rule = model.rule;
			sortType = model.sortType;
			sortOrder = model.sortOrder;
			SortOption = model.SortOption;
			modifiedTime = DateTime.Now;
		}
	}

	public static MatrixProfileQuadrantModel Create(string matrixSid, int quadrant, QuadrantModel model, string status)
	{
		DateTime now = DateTime.Now;
		MatrixProfileQuadrantModel matrixProfileQuadrantModel = new MatrixProfileQuadrantModel();
		matrixProfileQuadrantModel.id = matrixSid + "_quadrant" + quadrant;
		matrixProfileQuadrantModel.userId = LocalSettings.Settings.LoginUserId;
		matrixProfileQuadrantModel.matrixSid = matrixSid;
		matrixProfileQuadrantModel.quadrant = quadrant;
		matrixProfileQuadrantModel.createdTime = now;
		matrixProfileQuadrantModel.modifiedTime = now;
		matrixProfileQuadrantModel.status = status;
		matrixProfileQuadrantModel.deleted = false;
		matrixProfileQuadrantModel.Apply(model ?? QuadrantModel.GetDefault(quadrant));
		matrixProfileQuadrantModel.createdTime = now;
		matrixProfileQuadrantModel.status = status;
		return matrixProfileQuadrantModel;
	}
}
