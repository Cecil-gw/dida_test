using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Converter;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class AddTaskAttachmentInfo : BaseViewModel
{
	public string Path { get; set; }

	public ImageSource Image { get; set; }

	public bool Uploading { get; set; }

	public AddTaskAttachmentInfo(string path)
	{
		if (path != null)
		{
			try
			{
				Path = path;
				string text = AttachmentProvider.GetFileType(System.IO.Path.GetFileName(path)).ToString();
				Image = ((text == "IMAGE") ? ImageUtils.GetImageByUrl(path, 64, BitmapCacheOption.OnLoad) : new BitmapImage(new Uri((string)(new AttachmentIconConverter().Convert(text, null, new AttachmentConvertParam
				{
					IsDark = ThemeUtil.IsDark(),
					IsFailed = false
				}, null) ?? string.Empty))));
			}
			catch (Exception ex)
			{
				UtilLog.Error("AddTaskAttachmentInfo error:  " + ex.Message);
			}
		}
	}

	public void OnAttachmentClick()
	{
		if (!FileUtils.FileEmptyOrNotExists(Path))
		{
			try
			{
				Process.Start(Path);
			}
			catch (Exception)
			{
			}
		}
	}
}
