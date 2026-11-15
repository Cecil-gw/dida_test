using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class EpicUpdateModel : INotifyPropertyChanged
{
	private string _imageFilePath;

	public string Title { get; set; }

	public string Description { get; set; }

	public string ImageUrl { get; set; }

	public string DarkImageUrl { get; set; }

	[JsonIgnore]
	public string ImageFilePath
	{
		get
		{
			return _imageFilePath;
		}
		set
		{
			if (!(_imageFilePath == value))
			{
				_imageFilePath = value;
				NotifyPropertyChanged("ImageFilePath");
			}
		}
	}

	public EpicUpdateLinkModel Link { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
