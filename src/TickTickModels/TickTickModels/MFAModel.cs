namespace TickTickModels;

public class MFAModel : ErrorModel
{
	public string mfaEnabled { get; set; }

	public MFAItemModel mailOption { get; set; }

	public MFAItemModel phoneOption { get; set; }

	public MFAItemModel wechatOption { get; set; }

	public MFAItemModel appOption { get; set; }

	public MFAItemModel backupOption { get; set; }
}
