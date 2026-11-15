using System;
using Newtonsoft.Json.Linq;

namespace ticktick_WPF.Views.AI;

internal sealed class AgentBridgeNativeEventArgs : EventArgs
{
	public string MethodName { get; }

	public JToken Payload { get; }

	public AgentBridgeNativeEventArgs(string methodName, JToken payload)
	{
		MethodName = methodName;
		Payload = payload;
	}
}
