using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.MarkDown;

namespace ticktick_WPF.Views.Print;

public class SummaryPrint : UserControl, IComponentConnector
{
	private double _height;

	private MarkDownEditor _markDown;

	private double _width;

	internal FlowDocumentPageViewer PageViewer;

	internal FlowDocument Doc;

	internal ScrollViewer MarkDownGrid;

	private bool _contentLoaded;

	public SummaryPrint(string text, double width, double height)
	{
		ThemeUtil.SetTheme("light", this);
		InitializeComponent();
		Doc.PageHeight = height;
		Doc.PageWidth = width;
		_height = height - 150.0;
		_width = width - 120.0;
		PageViewer.Height = height;
		Doc.FontFamily = ((LocalSettings.Settings.ExtraSettings.AppFontFamily == "SourceHansansSC_CN") ? new FontFamily("Microsoft YaHei UI") : LocalSettings.Settings.FontFamily);
		_markDown = new MarkDownEditor(inPrint: true);
		_markDown.Width = width - 120.0;
		_markDown.Text = text;
		MarkDownGrid.Content = _markDown;
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_markDown.SetTheme();
		_markDown.UpdateLayout();
		GetBlocks();
	}

	private void GetBlocks()
	{
		if (_markDown.ActualHeight > _height)
		{
			string persistedText = _markDown.GetPersistedText();
			int pageLastIndex = _markDown.GetPageLastIndex(_height);
			int persistedOffset = _markDown.GetPersistedOffset(pageLastIndex);
			persistedOffset = Math.Max(0, Math.Min(persistedOffset, persistedText.Length));
			if (persistedOffset == 0 && persistedText.Length > 0)
			{
				persistedOffset = Math.Min(Math.Max(1, pageLastIndex), persistedText.Length);
			}
			string text = persistedText.Substring(0, persistedOffset);
			MarkDownEditor uiElement = new MarkDownEditor(inPrint: true)
			{
				Text = text,
				Width = _width,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(-20.0, 0.0, 0.0, 0.0),
				EnableSpellCheck = false
			};
			Doc.Blocks.Add(new BlockUIContainer(uiElement));
			_markDown.Text = persistedText.Substring(persistedOffset);
			_markDown.UpdateLayout();
			GetBlocks();
		}
		else
		{
			Doc.Blocks.Add(new BlockUIContainer(new MarkDownEditor(inPrint: true)
			{
				Text = _markDown.GetPersistedText(),
				Width = _width,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(-20.0, 0.0, 0.0, 0.0),
				EnableSpellCheck = false
			}));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/print/summaryprint.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			PageViewer = (FlowDocumentPageViewer)target;
			break;
		case 2:
			Doc = (FlowDocument)target;
			break;
		case 3:
			MarkDownGrid = (ScrollViewer)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
