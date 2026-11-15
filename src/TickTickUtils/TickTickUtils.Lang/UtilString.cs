using System.IO;
using System.Text;

namespace TickTickUtils.Lang;

public class UtilString
{
	public static string ToFilenameUtf8(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			return file;
		}
		string fileName = Path.GetFileName(file);
		if (!string.IsNullOrEmpty(fileName))
		{
			return ToUtf8(fileName.Replace("\"", "%22"));
		}
		return fileName;
	}

	public static string ToUtf8(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			return ToUtf8(Encoding.UTF8.GetBytes(text));
		}
		return text;
	}

	public static string ToUtf8(byte[] bytes)
	{
		if (bytes == null)
		{
			return null;
		}
		if (bytes.Length == 0)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(bytes.Length);
		foreach (byte value in bytes)
		{
			stringBuilder.Append((char)value);
		}
		return stringBuilder.ToString();
	}
}
