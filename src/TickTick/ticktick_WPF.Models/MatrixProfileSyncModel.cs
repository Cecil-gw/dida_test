using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class MatrixProfileSyncModel
{
	[JsonProperty("id")]
	public string id { get; set; }

	[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
	public string name { get; set; }

	[JsonProperty("sortOrder")]
	public long? sortOrder { get; set; }

	[JsonProperty("createdTime", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }

	[JsonProperty("modifiedTime", NullValueHandling = NullValueHandling.Ignore)]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? modifiedTime { get; set; }

	[JsonProperty("etag", NullValueHandling = NullValueHandling.Ignore)]
	public string etag { get; set; }

	[JsonProperty("deleted", NullValueHandling = NullValueHandling.Ignore)]
	public int? deleted { get; set; }

	[JsonProperty("matrixs")]
	public List<MatrixProfileQuadrantSyncModel> matrixs { get; set; } = new List<MatrixProfileQuadrantSyncModel>();
}
