using System.ServiceModel;

namespace ticktick_WPF.Util;

[ServiceContract(SessionMode = SessionMode.Allowed)]
public interface IS2CMessages
{
	[OperationContract(IsOneWay = true)]
	void CommandInClient(string text);
}
