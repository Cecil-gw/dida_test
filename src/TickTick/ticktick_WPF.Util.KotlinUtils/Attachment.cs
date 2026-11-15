namespace ticktick_WPF.Util.KotlinUtils;

public struct Attachment
{
	public long uniqueId;

	public string id;

	public Attachment(int uid, string id)
	{
		uniqueId = uid;
		this.id = id;
	}
}
