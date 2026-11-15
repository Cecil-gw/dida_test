using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using CommonMark;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

public class HtmlConverter
{
	private static string RemoveInvalidXmlChars(string text)
	{
		return new string(text.Where((char ch) => XmlConvert.IsXmlChar(ch)).ToArray());
	}

	public static string ConvertToPlainText(string html)
	{
		if (string.IsNullOrEmpty(html))
		{
			return "";
		}
		html = RemoveInvalidXmlChars(html);
		html = "<html>" + html + "</html>";
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(html);
		StringWriter stringWriter = new StringWriter();
		foreach (XmlNode childNode in xmlDocument.ChildNodes)
		{
			ConvertTo(childNode, stringWriter);
		}
		stringWriter.Flush();
		return stringWriter.ToString();
	}

	private static void ConvertTo(XmlNode node, TextWriter outText)
	{
		switch (node.NodeType)
		{
		case XmlNodeType.Document:
			ConvertContentTo(node, outText);
			break;
		case XmlNodeType.Text:
		{
			string innerText = node.InnerText;
			if (innerText.Trim().Length > 0)
			{
				bool flag = IsNodeBehindBr(node);
				if (node.ParentNode?.Name == "li" && !flag)
				{
					outText.Write(GetNodeSortLabel(node.ParentNode));
				}
				outText.Write(innerText.Trim());
				if (node.ParentNode?.Name == "li" && InFrontOfLiOrIsTheLastChild(node))
				{
					outText.Write("\r\n");
				}
			}
			break;
		}
		case XmlNodeType.Element:
		{
			string name = node.Name;
			if (!(name == "p"))
			{
				if (name == "br")
				{
					outText.Write("\r\n");
				}
			}
			else
			{
				outText.Write("\r\n");
			}
			if (node.HasChildNodes)
			{
				ConvertContentTo(node, outText);
			}
			break;
		}
		}
	}

	private static bool InFrontOfLiOrIsTheLastChild(XmlNode node)
	{
		if (node.ParentNode != null)
		{
			for (int i = 0; i < node.ParentNode.ChildNodes.Count; i++)
			{
				if (node.ParentNode.ChildNodes[i] == node)
				{
					if (i < node.ParentNode.ChildNodes.Count - 1 && (node.ParentNode.ChildNodes[i + 1].Name == "ul" || node.ParentNode.ChildNodes[i + 1].Name == "ol"))
					{
						return true;
					}
					if (i == node.ParentNode.ChildNodes.Count - 1)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool IsNodeBehindBr(XmlNode node)
	{
		if (node.ParentNode != null)
		{
			for (int num = node.ParentNode.ChildNodes.Count - 1; num >= 0; num--)
			{
				if (node.ParentNode.ChildNodes[num] == node && num > 0 && node.ParentNode.ChildNodes[num - 1].Name == "br")
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void ConvertContentTo(XmlNode node, TextWriter outText)
	{
		foreach (XmlNode childNode in node.ChildNodes)
		{
			ConvertTo(childNode, outText);
		}
	}

	private static string GetNodeSortLabel(XmlNode node)
	{
		string text = "";
		if (node.ParentNode != null)
		{
			int ulOrOlParentCount = GetUlOrOlParentCount(node, 0);
			for (int i = 0; i < ulOrOlParentCount - 1; i++)
			{
				text += "    ";
			}
			if (node.ParentNode.Name == "ul")
			{
				text += "- ";
			}
			if (node.ParentNode.Name == "ol")
			{
				int num = 0;
				foreach (XmlNode childNode in node.ParentNode.ChildNodes)
				{
					if (childNode.Name == "li")
					{
						num++;
					}
					if (childNode == node)
					{
						break;
					}
				}
				text = text + num + ".";
			}
		}
		return text;
	}

	private static int GetUlOrOlParentCount(XmlNode node, int num)
	{
		if (node.ParentNode != null && (node.ParentNode.Name == "ol" || node.ParentNode.Name == "ul"))
		{
			num++;
		}
		if (node.ParentNode == null || !(node.ParentNode.Name != "#document"))
		{
			return num;
		}
		return GetUlOrOlParentCount(node.ParentNode, num);
	}

	public static void Html2Rtf(string html)
	{
		WebBrowser webBrowser = new WebBrowser();
		webBrowser.CreateControl();
		string html2 = GetHtml(html);
		webBrowser.DocumentText = html2;
		webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;
	}

	private static string GetHtml(string html)
	{
		html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"> <title></title><style type=\"text/css\">li{font-size:13px}ul{list-style-type:disc}h1 {font-size:18px}h2 {font-size:16px}h3 {font-size:14px}p {font-size:13px}</style></head><body>" + html + "</body></html>";
		return html;
	}

	private static void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
	{
		try
		{
			if (sender is WebBrowser webBrowser)
			{
				webBrowser.Document?.ExecCommand("SelectAll", showUI: true, null);
				webBrowser.Document?.ExecCommand("Copy", showUI: false, null);
			}
		}
		catch (Exception)
		{
		}
	}

	public static string MdToHtml(string handledText)
	{
		try
		{
			return CommonMarkConverter.Convert(handledText.Replace("<", "\\<").Replace(">", "\\>"), new CommonMarkSettings
			{
				RenderSoftLineBreaksAsLineBreaks = true
			});
		}
		catch (Exception ex)
		{
			UtilLog.Warn(ex.Message);
			return null;
		}
	}
}
