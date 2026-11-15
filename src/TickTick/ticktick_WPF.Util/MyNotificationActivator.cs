using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

[ClassInterface(ClassInterfaceType.None)]
[ComSourceInterfaces(typeof(INotificationActivationCallback))]
[Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3")]
[ComVisible(true)]
public class MyNotificationActivator : NotificationActivator
{
	public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
	{
		UtilLog.Info(invokedArgs + "  " + appUserModelId);
	}
}
