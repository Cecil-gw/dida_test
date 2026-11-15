using System;
using Newtonsoft.Json;
using SQLite;

namespace TickTickModels;

[Serializable]
public class BaseModel
{
	[JsonIgnore]
	[PrimaryKey]
	[AutoIncrement]
	public int _Id { get; set; }
}
