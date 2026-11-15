using System;
using System.ServiceModel;

namespace ticktick_WPF.Util;

[ServiceContract(SessionMode = SessionMode.Allowed)]
public interface IC2SMessages
{
	[OperationContract(IsOneWay = true)]
	void Register(Guid clientId);

	[OperationContract(IsOneWay = true)]
	void DisplayCommandOnServer(string text);
}
