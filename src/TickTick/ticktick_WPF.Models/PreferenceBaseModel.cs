namespace ticktick_WPF.Models;

public abstract class PreferenceBaseModel
{
	public long mtime { get; set; }

	public abstract bool SetRemoteValue(PreferenceBaseModel remote);
}
