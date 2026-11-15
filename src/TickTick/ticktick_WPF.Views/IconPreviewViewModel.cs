using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Shapes;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views;

public class IconPreviewViewModel
{
	public ObservableCollection<GeometryItem> Items { get; set; } = new ObservableCollection<GeometryItem>();

	public void Load()
	{
		LoadDict("Resource\\SVGResource.xaml");
		LoadDict("Resource\\PomoIcons.xaml");
		LoadDict("Resource\\Sidebar.xaml");
		LoadDict("Resource\\Tasklist.xaml");
	}

	private void LoadDict(string path)
	{
		ResourceDictionary resourceDictionary = new ResourceDictionary
		{
			Source = new Uri(path, UriKind.Relative)
		};
		foreach (string key in resourceDictionary.Keys)
		{
			if (resourceDictionary[key] is Path)
			{
				GeometryItem geometryItem = new GeometryItem();
				geometryItem.Data = Utils.GetIcon(key);
				geometryItem.Name = key;
				Items.Add(geometryItem);
			}
		}
	}
}
