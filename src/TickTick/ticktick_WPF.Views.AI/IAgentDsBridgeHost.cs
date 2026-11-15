using System.Runtime.InteropServices;

namespace ticktick_WPF.Views.AI;

[ComVisible(true)]
[Guid("B3561D95-7EB4-4D31-AC8C-F3EECE3A2EC5")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IAgentDsBridgeHost
{
	[DispId(1)]
	string invoke(string methodName, string argumentsJson);
}
