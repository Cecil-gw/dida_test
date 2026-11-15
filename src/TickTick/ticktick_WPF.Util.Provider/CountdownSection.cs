namespace ticktick_WPF.Util.Provider;

public class CountdownSection : Section
{
	public CountdownSection()
	{
		base.Name = Utils.GetString("CountdownTitle");
		base.Ordinal = -1L;
		base.SectionId = "countdowns";
	}
}
