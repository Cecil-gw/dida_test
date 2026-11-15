using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class SignInWithApple
{
	private LoginDialog _parentDialog;

	private static readonly object _locker = new object();

	private static SignInWithApple _instance;

	public SignInWithApple()
	{
		App.RegisterURIProtocol();
	}

	public static SignInWithApple GetInstance(LoginDialog dialog)
	{
		if (_instance == null)
		{
			lock (_locker)
			{
				if (_instance == null)
				{
					_instance = new SignInWithApple();
				}
			}
		}
		_instance._parentDialog = dialog;
		return _instance;
	}

	public void Start()
	{
		UriNotifier.Uri -= OnUri;
		UriNotifier.Uri += OnUri;
		Utils.TryProcessStartUrl("https://appleid.apple.com/auth/authorize?client_id=com.ticktick.task.service&redirect_uri=https%3A%2F%2F" + BaseUrl.Domain + "%2Fsign%2Fwin%2Fapple&response_type=code%20id_token&state=Lw%3D%3D&scope=email%20name&response_mode=form_post");
	}

	private void OnUri(object sender, UriModel e)
	{
		if (!e.Path.StartsWith("sign/apple"))
		{
			return;
		}
		UriNotifier.Uri -= OnUri;
		SignInWithAppleParma e2 = default(SignInWithAppleParma);
		if (e.Parmas.ContainsKey("code"))
		{
			e2.accessToken = e.Parmas["code"];
		}
		if (e.Parmas.ContainsKey("user"))
		{
			string text = e.Parmas["user"];
			try
			{
				if (!string.IsNullOrEmpty(text))
				{
					JObject jObject = JObject.Parse(text);
					e2.name = string.Format("{0} {1}", jObject["name"]["firstName"], jObject["name"]["lastName"]);
					e2.email = jObject["email"].ToString();
				}
			}
			catch
			{
			}
		}
		if (e.Parmas.ContainsKey("id_token"))
		{
			string token = e.Parmas["id_token"];
			try
			{
				foreach (Claim claim in new JwtSecurityTokenHandler().ReadJwtToken(token).Claims)
				{
					if (claim.Type == "sub")
					{
						e2.uId = claim.Value;
					}
				}
			}
			catch (Exception ex)
			{
				UtilLog.Error("JWT Decode Exception " + ex.Message);
			}
		}
		TryLoginApple(e2);
	}

	private async void TryLoginApple(SignInWithAppleParma e)
	{
		_parentDialog?.TryLoginApple(e);
	}

	public void ClearLogin()
	{
		_parentDialog = null;
	}
}
