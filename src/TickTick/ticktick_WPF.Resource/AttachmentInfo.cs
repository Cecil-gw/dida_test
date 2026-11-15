namespace ticktick_WPF.Resource;

public class AttachmentInfo
{
	public readonly string Value;

	public readonly string Kind;

	public readonly string Url;

	public readonly int Offset;

	public bool Duplicate;

	public AttachmentInfo(string value, string kind, string url, int offset)
	{
		Value = value;
		Kind = kind;
		Url = url;
		Offset = offset;
	}
}
