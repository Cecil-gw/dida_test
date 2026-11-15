using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc.ColorSelector;

namespace ticktick_WPF.Views;

public class EditInboxDialog : Window, IOkCancelWindow, IComponentConnector
{
	internal ColorSelector ColorItems;

	internal Button SaveButton;

	private bool _contentLoaded;

	public EditInboxDialog()
	{
		InitializeComponent();
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.EscCommand, new KeyGesture(Key.Escape))
		{
			CommandParameter = this
		});
		base.InputBindings.Add(new KeyBinding(OkCancelWindowCommands.OkCommand, new KeyGesture(Key.Return))
		{
			CommandParameter = this
		});
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.Topmost = false;
		base.OnDeactivated(e);
	}

	protected override async void OnActivated(EventArgs e)
	{
		base.Topmost = false;
		await Task.Delay(10);
		base.Topmost = true;
		base.OnActivated(e);
	}

	private void OnSaveClick(object sender, RoutedEventArgs e)
	{
		Save();
	}

	private async void Save()
	{
		SaveButton.Content = Utils.GetString("Saving");
		SaveButton.IsEnabled = false;
		string selectedColor = ColorItems.GetSelectedColor();
		if (string.IsNullOrEmpty(selectedColor))
		{
			selectedColor = "transparent";
		}
		string text = selectedColor;
		if (text != (await ProjectDao.GetProjectById(Utils.GetInboxId()))?.color)
		{
			ColorSelector.TryAddClickEvent(selectedColor);
		}
		await ProjectDao.SaveInboxColor(selectedColor);
		SettingsHelper.PushLocalSettings();
		Close();
	}

	private void OnCancelClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.Isinbox);
		if (projectModel != null)
		{
			ColorItems.SetSelectedColor(projectModel.color);
		}
	}

	public void OnCancel()
	{
		Close();
	}

	public void Ok()
	{
		Save();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/editinboxdialog.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((EditInboxDialog)target).Loaded += OnWindowLoaded;
			break;
		case 2:
			ColorItems = (ColorSelector)target;
			break;
		case 3:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 4:
			((Button)target).Click += OnCancelClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
