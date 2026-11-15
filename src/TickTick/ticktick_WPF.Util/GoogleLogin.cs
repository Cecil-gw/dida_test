using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Views;

namespace ticktick_WPF.Util;

public class GoogleLogin
{
	private LoginDialog _dialog;

	private const string clientID = "366263775281-fbdi26qafnn9k6aoli4893k87srqun06.apps.googleusercontent.com";

	private const string clientSecret = "z4A-okmmX8bQB3iYBc4rkHqM";

	private const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

	private const string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

	private const string userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

	public GoogleLogin(LoginDialog dialog)
	{
		_dialog = dialog;
	}

	public static int GetRandomUnusedPort()
	{
		TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	public async Task PerformLogin()
	{
		string state = randomDataBase64url(32u);
		string code_verifier = randomDataBase64url(32u);
		string text = base64urlencodeNoPadding(sha256(code_verifier));
		string redirectURI = $"http://{IPAddress.Loopback}:{GetRandomUnusedPort()}/";
		output("redirect URI: " + redirectURI);
		HttpListener http = new HttpListener();
		http.Prefixes.Add(redirectURI);
		output("Listening..");
		try
		{
			http.Start();
		}
		catch (HttpListenerException)
		{
			new CustomerDialog(Utils.GetString("AccessDenied"), Utils.GetString("AccessDeniedMessage"), MessageBoxButton.OK).ShowDialog();
			return;
		}
		string fileName = string.Format("{0}?response_type=code&scope=https://www.googleapis.com/auth/userinfo.profile+https://www.googleapis.com/auth/userinfo.email&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}", "https://accounts.google.com/o/oauth2/v2/auth", Uri.EscapeDataString(redirectURI), "366263775281-fbdi26qafnn9k6aoli4893k87srqun06.apps.googleusercontent.com", state, text, "S256");
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = true
		};
		try
		{
			Process.Start(startInfo);
		}
		catch (Win32Exception ex2)
		{
			http.Stop();
			UtilLog.Error(ex2);
			output(ex2.Message);
			new CustomerDialog(string.Empty, "No Default Browser!", MessageBoxButton.OK).ShowDialog();
			return;
		}
		HttpListenerContext httpListenerContext = await http.GetContextAsync();
		HttpListenerResponse response = httpListenerContext.Response;
		string s = $"<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Login successfully, please wait...</body></html>";
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		response.ContentLength64 = bytes.Length;
		Stream responseOutput = response.OutputStream;
		responseOutput.WriteAsync(bytes, 0, bytes.Length).ContinueWith(delegate
		{
			responseOutput.Close();
			http.Stop();
			Console.WriteLine("HTTP server stopped.");
		});
		if (httpListenerContext.Request.QueryString.Get("error") != null)
		{
			output(string.Format("OAuth authorization error: {0}.", httpListenerContext.Request.QueryString.Get("error")));
			return;
		}
		if (httpListenerContext.Request.QueryString.Get("code") == null || httpListenerContext.Request.QueryString.Get("state") == null)
		{
			output("Malformed authorization response. " + httpListenerContext.Request.QueryString);
			return;
		}
		string text2 = httpListenerContext.Request.QueryString.Get("code");
		string text3 = httpListenerContext.Request.QueryString.Get("state");
		if (text3 != state)
		{
			output($"Received request with invalid state ({text3})");
			return;
		}
		output("Authorization code: " + text2);
		await performCodeExchange(text2, code_verifier, redirectURI);
	}

	private async Task performCodeExchange(string code, string code_verifier, string redirectURI)
	{
		if (_dialog == null)
		{
			return;
		}
		output("Exchanging code for tokens...");
		string requestUriString = "https://www.googleapis.com/oauth2/v4/token";
		string s = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code", code, Uri.EscapeDataString(redirectURI), "366263775281-fbdi26qafnn9k6aoli4893k87srqun06.apps.googleusercontent.com", code_verifier, "z4A-okmmX8bQB3iYBc4rkHqM");
		HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
		tokenRequest.Method = "POST";
		tokenRequest.ContentType = "application/x-www-form-urlencoded";
		tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		tokenRequest.ContentLength = bytes.Length;
		try
		{
			Stream stream = tokenRequest.GetRequestStream();
			await stream.WriteAsync(bytes, 0, bytes.Length);
			stream.Close();
			using StreamReader reader = new StreamReader((await tokenRequest.GetResponseAsync()).GetResponseStream());
			string value = await reader.ReadToEndAsync();
			output(value);
			string accessToken = JsonConvert.DeserializeObject<Dictionary<string, string>>(value)["access_token"];
			await userinfoCall(code, accessToken);
		}
		catch (WebException ex)
		{
			if (ex.Status == WebExceptionStatus.ProtocolError)
			{
				if (ex.Response is HttpWebResponse response)
				{
					using StreamReader reader = new StreamReader(response.GetResponseStream());
					string arg = await reader.ReadToEndAsync();
					System.Windows.Forms.MessageBox.Show($"GoogleLoginErrorCode: {response.StatusCode} \n {arg}");
				}
			}
			else
			{
				System.Windows.Forms.MessageBox.Show(ex.Message);
			}
		}
	}

	private async Task userinfoCall(string code, string accessToken)
	{
		LoginDialog dialog = _dialog;
		if (dialog != null)
		{
			await dialog.PerformGoogleByAccessToken(code, accessToken);
		}
	}

	public void output(string output)
	{
		Console.WriteLine(output);
	}

	public static string randomDataBase64url(uint length)
	{
		RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider();
		byte[] array = new byte[length];
		rNGCryptoServiceProvider.GetBytes(array);
		return base64urlencodeNoPadding(array);
	}

	public static byte[] sha256(string inputStirng)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
		return new SHA256Managed().ComputeHash(bytes);
	}

	public static string base64urlencodeNoPadding(byte[] buffer)
	{
		return Convert.ToBase64String(buffer).Replace("+", "-").Replace("/", "_")
			.Replace("=", "");
	}

	public void ClearLogin()
	{
		_dialog = null;
	}
}
