using System.Windows.Media;

namespace ticktick_WPF.Util.Attachment;

internal class AttachmentResourceHelper
{
	public static string GetFileTypeImage(string fileType)
	{
		return fileType switch
		{
			"AUDIO" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_audio.png", 
			"VIDEO" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_video.png", 
			"PDF" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_pdf.png", 
			"DOC" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_doc.png", 
			"TEXT" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_txt.png", 
			"XLS" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_xls.png", 
			"PPT" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_ppt.png", 
			"CSV" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_csv.png", 
			"ZIP" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_zip.png", 
			"OTHER" => "pack://application:,,,/Assets/Attachment_Icon/Attachment_unknow.png", 
			_ => "pack://application:,,,/Assets/Attachment_Icon/Attachment_unknow.png", 
		};
	}

	public static Brush GetFileTypeBorderColor(string fileType)
	{
		return fileType switch
		{
			"AUDIO" => ThemeUtil.GetColor("AudioBorderColor"), 
			"VIDEO" => ThemeUtil.GetColor("VideoBorderColor"), 
			"PDF" => ThemeUtil.GetColor("PdfBorderColor"), 
			"DOC" => ThemeUtil.GetColor("DocBorderColor"), 
			"TEXT" => ThemeUtil.GetColor("TxtBorderColor"), 
			"XLS" => ThemeUtil.GetColor("XlsBorderColor"), 
			"PPT" => ThemeUtil.GetColor("PptBorderColor"), 
			"CSV" => ThemeUtil.GetColor("CsvBorderColor"), 
			"ZIP" => ThemeUtil.GetColor("ZipBorderColor"), 
			"OTHER" => ThemeUtil.GetColor("OtherBorderColor"), 
			_ => ThemeUtil.GetColor("OtherBorderColor"), 
		};
	}

	public static SolidColorBrush GetFileTypeColor(string fileType)
	{
		return fileType switch
		{
			"AUDIO" => ThemeUtil.GetColor("AudioBackgroundColor"), 
			"VIDEO" => ThemeUtil.GetColor("VideoBackgroundColor"), 
			"PDF" => ThemeUtil.GetColor("PdfBackgroundColor"), 
			"DOC" => ThemeUtil.GetColor("DocBackgroundColor"), 
			"TEXT" => ThemeUtil.GetColor("TxtBackgroundColor"), 
			"XLS" => ThemeUtil.GetColor("XlsBackgroundColor"), 
			"PPT" => ThemeUtil.GetColor("PptBackgroundColor"), 
			"CSV" => ThemeUtil.GetColor("CsvBackgroundColor"), 
			"ZIP" => ThemeUtil.GetColor("ZipBackgroundColor"), 
			_ => ThemeUtil.GetColor("OtherBackgroundColor"), 
		};
	}
}
