using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Misc;

public class SmartProjectViewModel : SelectableItemViewModel
{
	public SmartProjectViewModel()
	{
		base.Type = "smart";
	}

	public override ProjectIdentity ToIdentity()
	{
		return ProjectIdentity.CreateSmartIdentity(base.Id);
	}
}
