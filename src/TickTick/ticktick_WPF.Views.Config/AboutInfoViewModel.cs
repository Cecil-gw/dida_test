using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Config;

public class AboutInfoViewModel : INotifyPropertyChanged
{
	private bool _isUpdateEnable;

	private string _newVersionNum;

	private string _newVersionDescription;

	public string CurrentVersion => Application.ResourceAssembly.GetName().Version.ToString();

	public string CopyRight => "作者 Sikoll 个人修改测试版，禁止商业化运用";

	public bool IsUpdateEnable
	{
		get
		{
			return _isUpdateEnable;
		}
		set
		{
			_isUpdateEnable = value;
			NotifyPropertyChanged("IsUpdateEnable");
		}
	}

	public string NewVersionNum
	{
		get
		{
			return _newVersionNum;
		}
		set
		{
			_newVersionNum = value;
			NotifyPropertyChanged("NewVersionNum");
		}
	}

	public string NewVersionDescription
	{
		get
		{
			return _newVersionDescription;
		}
		set
		{
			_newVersionDescription = value;
			NotifyPropertyChanged("NewVersionDescription");
		}
	}

	public string DownloadUrl { get; set; }

	public PackageSizeModel ArcSize { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
