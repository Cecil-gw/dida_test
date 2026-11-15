using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Print;

public class PaginatorHeaderFooter : DocumentPaginator
{
	private readonly DocumentPaginator _mPaginator;

	private double _designWidth;

	public override bool IsPageCountValid => _mPaginator.IsPageCountValid;

	public override int PageCount => _mPaginator.PageCount;

	public override Size PageSize
	{
		get
		{
			return _mPaginator.PageSize;
		}
		set
		{
			_mPaginator.PageSize = value;
		}
	}

	public override IDocumentPaginatorSource Source => _mPaginator.Source;

	public PaginatorHeaderFooter(DocumentPaginator paginator, double designWidth = 793.0)
	{
		_designWidth = designWidth;
		_mPaginator = paginator;
	}

	public override DocumentPage GetPage(int pageNumber)
	{
		DocumentPage page = _mPaginator.GetPage(pageNumber);
		ContainerVisual containerVisual = new ContainerVisual();
		double num = 793.0 / _designWidth;
		DrawingVisual drawingVisual = new DrawingVisual();
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			BitmapImage image = Utils.GetImage("pack://application:,,,/Assets/printDida.png");
			drawingContext.DrawImage(image, new Rect(page.ContentBox.Left, page.ContentBox.Bottom + 6.5 / num, 122.0 / num, 24.0 / num));
		}
		DrawingVisual drawingVisual2 = new DrawingVisual();
		using (DrawingContext drawingContext2 = drawingVisual2.RenderOpen())
		{
			FormattedText formattedText = new FormattedText($"page {pageNumber + 1}/{PageCount}", CultureInfo.CurrentCulture, FlowDirection.RightToLeft, new Typeface("Courier New"), 10.0 / num, ThemeUtil.GetAlphaColor("#191919", 56));
			drawingContext2.DrawText(formattedText, new Point(page.ContentBox.Right, page.ContentBox.Bottom + 13.0 / num));
		}
		ContainerVisual containerVisual2 = new ContainerVisual();
		containerVisual2.Children.Add(page.Visual);
		containerVisual.Children.Add(containerVisual2);
		containerVisual.Children.Add(drawingVisual);
		containerVisual.Children.Add(drawingVisual2);
		containerVisual.Transform = new ScaleTransform(num, num, 0.0, 0.0);
		return new DocumentPage(containerVisual, (page.Size == Size.Empty) ? page.Size : new Size(page.Size.Width * num, page.Size.Height * num), page.BleedBox, page.ContentBox);
	}
}
