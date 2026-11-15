using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views;

public class LoginBindThirdViewModel : BaseViewModel
{
	private string _password;

	private string _passwordComfirm;

	private bool _passwordInvaild;

	private bool _passwordNotSame;

	private bool _networkBroken;

	private bool _notUploading;

	public string Password
	{
		get
		{
			return _password;
		}
		set
		{
			_password = value;
			OnPropertyChanged("Password");
		}
	}

	public string PasswordComfirm
	{
		get
		{
			return _passwordComfirm;
		}
		set
		{
			_passwordComfirm = value;
			OnPropertyChanged("PasswordComfirm");
		}
	}

	public string Email { get; set; }

	public string ComfirmTitle => Utils.GetString("LoginThirdRegisteredTitle");

	public string ComfirmInfo => string.Format(Utils.GetString("LoginThirdRegisteredInfo"), Email);

	public bool PasswordInvaild
	{
		get
		{
			return _passwordInvaild;
		}
		set
		{
			_passwordInvaild = value;
			OnPropertyChanged("PasswordInvaild");
		}
	}

	public bool PasswordNotSame
	{
		get
		{
			return _passwordNotSame;
		}
		set
		{
			_passwordNotSame = value;
			OnPropertyChanged("PasswordNotSame");
		}
	}

	public bool NetworkBroken
	{
		get
		{
			return _networkBroken;
		}
		set
		{
			_networkBroken = value;
			OnPropertyChanged("NetworkBroken");
		}
	}

	public bool NotUploading
	{
		get
		{
			return _notUploading;
		}
		set
		{
			_notUploading = value;
			OnPropertyChanged("NotUploading");
		}
	}
}
