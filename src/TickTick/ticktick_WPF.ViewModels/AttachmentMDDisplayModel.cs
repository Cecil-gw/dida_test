using System;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

public class AttachmentMDDisplayModel
{
	private AttachmentInfo _info;

	public Func<int?> DisplayOffsetProvider;

	public string LocalPath;

	public AttachmentModel Attachment;

	public AttachmentInfo Info
	{
		get
		{
			if (_info == null || DisplayOffsetProvider == null)
			{
				return _info;
			}
			int? num = DisplayOffsetProvider();
			if (!num.HasValue)
			{
				return null;
			}
			if (num.Value == _info.Offset)
			{
				return _info;
			}
			return new AttachmentInfo(_info.Value, _info.Kind, _info.Url, num.Value)
			{
				Duplicate = _info.Duplicate
			};
		}
		set
		{
			_info = value;
		}
	}
}
