using System;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class TagModifyModel : BaseModel
{
	public string name { get; set; }

	public string newName { get; set; }
}
