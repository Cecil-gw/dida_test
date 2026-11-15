using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, InstanceContextMode = InstanceContextMode.Single)]
internal class PipelineService : IC2SMessages
{
	private ServiceHost _serverHost;

	private static PipelineService _instance;

	private readonly List<Guid> _registeredClients = new List<Guid>();

	static PipelineService()
	{
		_instance = new PipelineService();
	}

	private PipelineService()
	{
	}

	public static PipelineService GetInstance()
	{
		return _instance;
	}

	public void RegisterServiceHost()
	{
		try
		{
			_serverHost = new ServiceHost(this);
			_serverHost.AddServiceEndpoint(typeof(IC2SMessages), new NetNamedPipeBinding(), "net.pipe://localhost/TickTick");
			_serverHost.Open();
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public void CloseServiceHost()
	{
		try
		{
			_serverHost.Close();
		}
		catch (Exception)
		{
		}
	}

	public void Register(Guid clientId)
	{
		Guid guid = clientId;
		UtilLog.Info("Pipeline.Register " + guid.ToString());
		if (!_registeredClients.Contains(clientId))
		{
			_registeredClients.Add(clientId);
		}
	}

	public void DisplayCommandOnServer(string text)
	{
		UtilLog.Info("Pipeline.DisplayCommandOnServer " + text);
		if (text.StartsWith("ticktick://"))
		{
			TickTickUri(text);
		}
		else
		{
			App.Window?.HandlePipelineMsg(text);
		}
	}

	private void TickTickUri(string uri)
	{
		uri = uri.Substring(uri.IndexOf("//", StringComparison.Ordinal) + 2);
		uri = HttpUtility.UrlDecode(uri);
		if (uri.Contains('?'))
		{
			string path = uri.Substring(0, uri.IndexOf('?'));
			string param = uri.Substring(uri.IndexOf('?') + 1);
			UriNotifier.NotifyUri(path, param);
		}
	}
}
