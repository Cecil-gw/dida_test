using TickTickModels;

namespace ticktick_WPF.Models;

public class TempDataModel : BaseModel
{
	public const string NOTIFICIATION = "NOTIFICIATION";

	public const string SHAREUSERLIST = "SHAREUSERLIST";

	public const string USERLIST = "USERLIST";

	public const string ATTACHMENTLIMIT = "ATTACHMENTLIMIT";

	public string User_Id { get; set; }

	public string Data { get; set; }

	public string DataType { get; set; }

	public long ModifyTime { get; set; }

	public string EntityId { get; set; }
}
