using System;
using System.IO.Pipes;
using System.Text;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public class NamePipeService
{
	public static void SendPipeText(string arg, string pipeName)
	{
		NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
		namedPipeClientStream.Connect(30000);
		byte[] bytes = Encoding.Unicode.GetBytes(arg);
		namedPipeClientStream.BeginWrite(bytes, 0, bytes.Length, AsyncSend, namedPipeClientStream);
	}

	private static void AsyncSend(IAsyncResult iar)
	{
		try
		{
			NamedPipeClientStream obj = (NamedPipeClientStream)iar.AsyncState;
			obj.EndWrite(iar);
			obj.Flush();
			obj.Close();
			obj.Dispose();
		}
		catch (Exception)
		{
		}
	}

	public static void InitPipe(string pipeName, Action<string> handlePipeCommand)
	{
		try
		{
			NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			namedPipeServerStream.BeginWaitForConnection(HandleClientConnection, namedPipeServerStream);
		}
		catch (Exception ex)
		{
			UtilLog.Info(ExceptionUtils.BuildExceptionMessage(ex));
		}
		void HandleClientConnection(IAsyncResult ar)
		{
			try
			{
				using (NamedPipeServerStream namedPipeServerStream2 = (NamedPipeServerStream)ar.AsyncState)
				{
					namedPipeServerStream2.EndWaitForConnection(ar);
					string obj = IOUtils.ReadString(namedPipeServerStream2.Read);
					handlePipeCommand(obj);
				}
				NamedPipeServerStream namedPipeServerStream3 = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				namedPipeServerStream3.BeginWaitForConnection(HandleClientConnection, namedPipeServerStream3);
			}
			catch (Exception)
			{
			}
		}
	}
}
