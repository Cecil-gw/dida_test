using System;
using System.Windows;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Views.Detail;

namespace ticktick_WPF.ViewModels;

public class AttachmentViewModel : BaseViewModel
{
	private object _tag;

	private Thickness _margin;

	private Thickness _statusMargin;

	private CornerRadius _cornerRadius;

	private double _width;

	private double _height;

	private Brush _selectedBrush;

	private int _imageMode;

	private string _path;

	private string _localPath;

	private int _size;

	private string _fileName;

	private string _fileType;

	private DateTime? _createdTime;

	private int _ordinal;

	private OptTypeEnum _fileStatus;

	public AttachmentModel AttachmentModel { get; set; }

	public string Id { get; set; }

	public string RefId { get; set; }

	public string TaskId { get; set; }

	public bool Deleted { get; set; }

	public bool CanDelete { get; set; }

	public string SyncStatus { get; set; }

	public object Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			if (_tag != value)
			{
				_tag = value;
				OnPropertyChanged("Tag");
			}
		}
	}

	public Thickness Margin
	{
		get
		{
			return _margin;
		}
		set
		{
			if (!(_margin == value))
			{
				_margin = value;
				OnPropertyChanged("Margin");
			}
		}
	}

	public Thickness StatusMargin
	{
		get
		{
			return _statusMargin;
		}
		set
		{
			if (!(_statusMargin == value))
			{
				_statusMargin = value;
				OnPropertyChanged("StatusMargin");
			}
		}
	}

	public double RadiusX => _cornerRadius.TopRight;

	public double RadiusY => _cornerRadius.BottomLeft;

	public CornerRadius CornerRadius
	{
		get
		{
			return _cornerRadius;
		}
		set
		{
			_cornerRadius = value;
			OnPropertyChanged("CornerRadius");
			OnPropertyChanged("RadiusX");
			OnPropertyChanged("RadiusY");
		}
	}

	public double Width
	{
		get
		{
			return _width;
		}
		set
		{
			if (!(Math.Abs(_width - value) < 0.1))
			{
				_width = value;
				OnPropertyChanged("Width");
			}
		}
	}

	public double Height
	{
		get
		{
			return _height;
		}
		set
		{
			if (!(Math.Abs(_height - value) < 0.1))
			{
				_height = value;
				OnPropertyChanged("Height");
			}
		}
	}

	public Brush SelectedBrush
	{
		get
		{
			return _selectedBrush;
		}
		set
		{
			if (_selectedBrush != value)
			{
				_selectedBrush = value;
				OnPropertyChanged("SelectedBrush");
			}
		}
	}

	public int ImageMode
	{
		get
		{
			return _imageMode;
		}
		set
		{
			if (_imageMode != value)
			{
				_imageMode = value;
				OnPropertyChanged("ImageMode");
			}
		}
	}

	public string Path
	{
		get
		{
			return _path;
		}
		set
		{
			if (!(_path == value))
			{
				_path = value;
				OnPropertyChanged("Path");
			}
		}
	}

	public string LocalPath
	{
		get
		{
			return _localPath;
		}
		set
		{
			if (!(_localPath == value))
			{
				_localPath = value;
				OnPropertyChanged("LocalPath");
			}
		}
	}

	public int Size
	{
		get
		{
			return _size;
		}
		set
		{
			if (_size != value)
			{
				_size = value;
				OnPropertyChanged("Size");
			}
		}
	}

	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			if (!(_fileName == value))
			{
				_fileName = value;
				OnPropertyChanged("FileName");
			}
		}
	}

	public string FileType
	{
		get
		{
			return _fileType;
		}
		set
		{
			if (!(_fileType == value))
			{
				_fileType = value;
				OnPropertyChanged("FileType");
			}
		}
	}

	public DateTime? CreatedTime
	{
		get
		{
			return _createdTime;
		}
		set
		{
			if (!(_createdTime == value))
			{
				_createdTime = value;
				OnPropertyChanged("CreatedTime");
			}
		}
	}

	public int Ordinal
	{
		get
		{
			return _ordinal;
		}
		set
		{
			if (_ordinal != value)
			{
				_ordinal = value;
				OnPropertyChanged("Ordinal");
			}
		}
	}

	public OptTypeEnum FileStatus
	{
		get
		{
			return _fileStatus;
		}
		set
		{
			if (_fileStatus != value)
			{
				_fileStatus = value;
				OnPropertyChanged("FileStatus");
			}
		}
	}

	public AttachmentViewModel(string filePath, int imageMode = 0)
	{
		LocalPath = filePath;
		ImageMode = imageMode;
	}

	public AttachmentViewModel(AttachmentModel attachment, int imageMode = 0)
	{
		UpdateProperty(attachment, imageMode);
	}

	public void UpdateProperty(AttachmentModel model, int imageMode)
	{
		Id = model.id;
		Path = model.path;
		LocalPath = model.localPath;
		RefId = model.refId;
		TaskId = model.taskId;
		FileName = model.fileName;
		FileType = model.fileType;
		CreatedTime = model.createdTime;
		Size = model.size;
		SyncStatus = model.sync_status;
		Deleted = model.deleted;
		CanDelete = model.canDelete;
		ImageMode = imageMode;
		FileStatus = AttachmentLoadHelper.AttachmentStatus(model);
		AttachmentModel = model;
		try
		{
			Ordinal = (int)(Constants.AttachmentKind)Enum.Parse(typeof(Constants.AttachmentKind), FileType.ToUpper());
		}
		catch (Exception)
		{
			Ordinal = 10;
		}
	}

	public void SetFileStatus(OptTypeEnum status)
	{
		FileStatus = status;
	}
}
