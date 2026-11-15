using System.Threading.Tasks;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.MarkDown;

public interface ILinkMenuHost
{
	Task TryShowLinkMenuPopup(LinkLineInfoModel linkLineInfo);

	Task HideLinkMenuPopup();
}
