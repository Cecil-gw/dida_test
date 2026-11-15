using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class ClosedProjectGroupViewModel : ProjectGroupViewModel
{
	public ClosedProjectGroupViewModel(ProjectGroupModel itemGroup)
		: base(itemGroup)
	{
		base.Icon = Utils.GetGeometry("Ic.ProjectList.DisableFolder");
		base.CanDrag = false;
		base.IsGroupItem = true;
		base.ShowCount = false;
	}

	public override async Task<IEnumerable<ContextAction>> GetContextActions()
	{
		return null;
	}
}
