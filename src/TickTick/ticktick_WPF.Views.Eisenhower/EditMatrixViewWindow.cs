using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Eisenhower;

public class EditMatrixViewWindow : Window, IOkCancelWindow, IComponentConnector, IStyleConnector
{
	private class EditMatrixViewViewModel
	{
		public ObservableCollection<MatrixQuadrantRowViewModel> Quadrants { get; } = new ObservableCollection<MatrixQuadrantRowViewModel>();
	}

	private class MatrixQuadrantRowViewModel
	{
		public QuadrantModel Quadrant { get; }

		public int Level { get; }

		public string Title { get; }

		public string Summary { get; }

		public Geometry Icon { get; }

		public Brush IconBrush { get; }

		public MatrixQuadrantRowViewModel(QuadrantModel quadrant, int level, bool showSeparator)
		{
			Quadrant = quadrant;
			Level = level;
			Title = (string.IsNullOrEmpty(quadrant.name) ? Utils.GetString("MatrixTitleQuadrant" + level) : quadrant.name);
			Summary = MatrixManager.GetRuleSummary(quadrant.rule);
			Icon = Utils.GetIcon("MatrixIconQuadrant" + level);
			IconBrush = ThemeUtil.GetColor("MatrixColorQuadrant" + level);
		}
	}

	private readonly MatrixProfileModel _profile;

	private readonly bool _isAdd;

	private readonly List<QuadrantModel> _draftQuadrants;

	private readonly EditMatrixViewViewModel _viewModel;

	internal TextBlock TitleText;

	internal TextBox NameTextBox;

	internal TextBlock DeleteText;

	internal Button SaveButton;

	private bool _contentLoaded;

	public EditMatrixViewWindow(MatrixProfileModel profile = null)
	{
		InitializeComponent();
		_viewModel = new EditMatrixViewViewModel();
		base.DataContext = _viewModel;
		_profile = profile;
		_isAdd = profile == null;
		_draftQuadrants = LoadDraftQuadrants(profile);
		TitleText.Text = Utils.GetString(_isAdd ? "AddView" : "EditView");
		NameTextBox.Text = GetInitialName(profile);
		BuildQuadrantRows();
		UpdateSaveState();
		base.Loaded += delegate
		{
			NameTextBox.Focus();
			NameTextBox.Select(NameTextBox.Text.Length, 0);
		};
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdateSaveState();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private async void DeleteText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		await DeleteProfile();
	}

	private static List<QuadrantModel> LoadDraftQuadrants(MatrixProfileModel profile)
	{
		return (from q in ((profile == null) ? MatrixProfileDao.GetActiveQuadrantsSync() : MatrixProfileDao.GetProfileQuadrantsSync(profile.sid)).Select(MatrixProfileDao.CloneQuadrant)
			orderby q.sortOrder
			select q).ToList();
	}

	private static string GetInitialName(MatrixProfileModel profile)
	{
		if (profile == null)
		{
			return string.Empty;
		}
		if (!string.IsNullOrWhiteSpace(profile.name))
		{
			return profile.name;
		}
		return Utils.GetString("Matrix");
	}

	private void BuildQuadrantRows()
	{
		_viewModel.Quadrants.Clear();
		List<QuadrantModel> list = _draftQuadrants.OrderBy((QuadrantModel q) => q.sortOrder).ToList();
		for (int num = 0; num < list.Count; num++)
		{
			QuadrantModel quadrant = list[num];
			_viewModel.Quadrants.Add(new MatrixQuadrantRowViewModel(quadrant, GetLevel(quadrant), num < list.Count - 1));
		}
	}

	private void ReloadQuadrantRows()
	{
		_draftQuadrants.Clear();
		_draftQuadrants.AddRange(LoadDraftQuadrants(_profile));
		BuildQuadrantRows();
	}

	private void QuadrantRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: MatrixQuadrantRowViewModel dataContext })
		{
			EditQuadrant(dataContext.Level);
		}
	}

	private void EditQuadrant(int level)
	{
		QuadrantModel quadrant = _draftQuadrants.FirstOrDefault((QuadrantModel q) => GetLevel(q) == level) ?? QuadrantModel.GetDefault(level);
		if (!_isAdd && _profile != null)
		{
			EditQuadrantWindow editQuadrantWindow = new EditQuadrantWindow(level, _profile.sid, quadrant);
			editQuadrantWindow.Owner = this;
			editQuadrantWindow.ShowDialog();
			ReloadQuadrantRows();
			return;
		}
		EditQuadrantWindow editQuadrantWindow2 = new EditQuadrantWindow(level, quadrant, delegate(QuadrantModel updated)
		{
			int num = _draftQuadrants.FindIndex((QuadrantModel q) => GetLevel(q) == level);
			if (num >= 0)
			{
				_draftQuadrants[num] = updated;
			}
			else
			{
				_draftQuadrants.Add(updated);
			}
			BuildQuadrantRows();
		});
		editQuadrantWindow2.Owner = this;
		editQuadrantWindow2.ShowDialog();
	}

	private async void OnSaveClick(object sender, RoutedEventArgs e)
	{
		if (!UpdateSaveState())
		{
			return;
		}
		try
		{
			string name = NameTextBox.Text.Trim();
			if (_isAdd)
			{
				if (!ProChecker.CheckPro(ProType.Matrix, this))
				{
					return;
				}
				MatrixProfileModel matrixProfileModel = await MatrixProfileDao.AddProfileFromCurrentAsync(name);
				if (matrixProfileModel == null)
				{
					ShowToast(Utils.GetString("LimitTips"));
					return;
				}
				await MatrixProfileDao.SaveProfileAsync(matrixProfileModel.sid, name, _draftQuadrants, makeActive: false);
			}
			else
			{
				bool makeActive = string.Equals(LocalSettings.Settings.MatrixActiveViewId, _profile.sid, StringComparison.Ordinal);
				if (!UserDao.IsPro())
				{
					await MatrixProfileDao.SaveProfileNameAsync(_profile.sid, name, makeActive);
				}
				else
				{
					await MatrixProfileDao.SaveProfileAsync(_profile.sid, name, _draftQuadrants, makeActive);
				}
			}
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
			base.DialogResult = true;
			Close();
		}
		catch (InvalidOperationException ex)
		{
			ShowToast(Utils.GetString(ex.Message));
		}
		catch (Exception ex2)
		{
			UtilLog.Warn("EditMatrixViewWindow.OnSaveClick " + ex2.Message);
			ShowToast(Utils.GetString("OperateFailed"));
		}
	}

	private async Task DeleteProfile()
	{
		if (_profile != null && MatrixProfileDao.GetProfilesSync().Count > 1 && new CustomerDialog(Utils.GetString("DeleteView"), Utils.GetString("MatrixDeleteViewMessage"), Utils.GetString("Delete"), Utils.GetString("Cancel"), this).ShowDialog() == true)
		{
			await MatrixProfileDao.DeleteProfileAsync(_profile.sid);
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
			base.DialogResult = true;
			Close();
		}
	}

	private bool UpdateSaveState()
	{
		bool flag = !string.IsNullOrWhiteSpace(NameTextBox.Text);
		if (SaveButton != null)
		{
			SaveButton.IsEnabled = flag;
			SaveButton.Opacity = (flag ? 1.0 : 0.45);
		}
		return flag;
	}

	private static int GetLevel(QuadrantModel quadrant)
	{
		if (quadrant != null && quadrant.id?.StartsWith("quadrant") == true && int.TryParse(quadrant.id.Substring("quadrant".Length), out var result))
		{
			return result;
		}
		return 1;
	}

	private static void ShowToast(string message)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			(Utils.GetToastWindow() ?? App.Window)?.TryToastString(null, message);
		}
	}

	public override void OnApplyTemplate()
	{
		Utils.InitBaseEvents(this, base.GetTemplateChild);
		base.OnApplyTemplate();
	}

	public void OnCancel()
	{
		CancelButton_Click(null, null);
	}

	public void Ok()
	{
		OnSaveClick(null, null);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/eisenhower/editmatrixviewwindow.xaml", UriKind.Relative);
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
			TitleText = (TextBlock)target;
			break;
		case 2:
			NameTextBox = (TextBox)target;
			NameTextBox.TextChanged += NameTextBox_TextChanged;
			break;
		case 4:
			DeleteText = (TextBlock)target;
			DeleteText.MouseLeftButtonUp += DeleteText_MouseLeftButtonUp;
			break;
		case 5:
			SaveButton = (Button)target;
			SaveButton.Click += OnSaveClick;
			break;
		case 6:
			((Button)target).Click += CancelButton_Click;
			break;
		case 7:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 3)
		{
			((Border)target).MouseLeftButtonUp += QuadrantRow_MouseLeftButtonUp;
		}
	}
}
