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
using System.Windows.Threading;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Eisenhower;

public class ManageMatrixViewsWindow : Window, IOkCancelWindow, IComponentConnector, IStyleConnector
{
	private class ManageMatrixViewsViewModel
	{
		public ObservableCollection<MatrixProfileRowViewModel> Profiles { get; } = new ObservableCollection<MatrixProfileRowViewModel>();

		public ICommand AddCommand { get; set; }

		public ICommand DeleteCommand { get; set; }
	}

	private class MatrixProfileRowViewModel : INotifyPropertyChanged, IDraggable, ICloneable
	{
		private DragVisualState _visualState;

		public MatrixProfileModel Profile { get; }

		public string Sid => Profile?.sid;

		public string DisplayName => Profile?.DisplayName;

		public bool CanDelete { get; }

		public bool CanDrag { get; }

		public DragVisualState VisualState
		{
			get
			{
				return _visualState;
			}
			set
			{
				if (_visualState != value)
				{
					_visualState = value;
					OnPropertyChanged("VisualState");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public MatrixProfileRowViewModel(MatrixProfileModel profile, bool canDelete, bool canDrag)
		{
			Profile = profile;
			CanDelete = canDelete;
			CanDrag = canDrag;
		}

		public object Clone()
		{
			return new MatrixProfileRowViewModel(Profile, CanDelete, CanDrag);
		}

		private void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	private List<MatrixProfileModel> _profiles = new List<MatrixProfileModel>();

	private readonly ManageMatrixViewsViewModel _viewModel;

	internal ScrollViewer RootScroll;

	internal ItemsControl ProfilesItems;

	internal Border AddViewBorder;

	private bool _contentLoaded;

	public ManageMatrixViewsWindow()
	{
		InitializeComponent();
		DragSortBehavior.SetSortCompleted(ProfilesItems, OnSortCompleted);
		_viewModel = new ManageMatrixViewsViewModel
		{
			AddCommand = new RelayCommand(delegate
			{
				AddProfile();
			}),
			DeleteCommand = new RelayCommand(async delegate(object profile)
			{
				await DeleteProfile(profile as MatrixProfileModel);
			})
		};
		base.DataContext = _viewModel;
		base.Loaded += delegate
		{
			Reload();
		};
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void Reload()
	{
		_profiles = MatrixProfileDao.GetProfilesSync();
		_viewModel.Profiles.Clear();
		bool canDelete = _profiles.Count > 1;
		bool canDrag = UserDao.IsPro() && _profiles.Count > 1;
		bool flag = !UserDao.IsPro() || _profiles.Count < MatrixProfileDao.GetProfileLimit();
		AddViewBorder.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		foreach (MatrixProfileModel profile in _profiles)
		{
			_viewModel.Profiles.Add(new MatrixProfileRowViewModel(profile, canDelete, canDrag));
		}
	}

	private void ProfileRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs args)
	{
		DependencyObject dependencyObject = args.OriginalSource as DependencyObject;
		if (!(dependencyObject is Button) && (dependencyObject == null || Utils.FindParent<Button>(dependencyObject) == null) && sender is FrameworkElement { DataContext: MatrixProfileRowViewModel dataContext })
		{
			OpenEdit(dataContext.Profile);
		}
	}

	private async void OnSortCompleted(object dragItem, int oldIndex, int newIndex)
	{
		if (oldIndex == newIndex)
		{
			return;
		}
		if (!UserDao.IsPro())
		{
			ProChecker.CheckPro(ProType.Matrix, this);
			Reload();
			return;
		}
		List<string> list = (from profile in _viewModel.Profiles
			select profile.Sid into sid
			where !string.IsNullOrEmpty(sid)
			select sid).ToList();
		if (list.Count != _profiles.Count || list.SequenceEqual(_profiles.Select((MatrixProfileModel profile) => profile.sid)))
		{
			return;
		}
		try
		{
			await MatrixProfileDao.ReorderProfilesAsync(list);
			Reload();
		}
		catch (Exception)
		{
			ShowToast(Utils.GetString("OperateFailed"));
			Reload();
		}
	}

	private void OpenEdit(MatrixProfileModel profile)
	{
		if (new EditMatrixViewWindow(profile)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			Reload();
		}
	}

	private void AddRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		e.Handled = true;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			_viewModel.AddCommand.Execute(null);
		}, DispatcherPriority.Background);
	}

	private void AddProfile()
	{
		if (ProChecker.CheckPro(ProType.Matrix, this))
		{
			int profileLimit = MatrixProfileDao.GetProfileLimit();
			if (_profiles.Count >= profileLimit)
			{
				ShowToast(Utils.GetString("LimitTips"));
			}
			else if (new EditMatrixViewWindow
			{
				Owner = this
			}.ShowDialog() == true)
			{
				Reload();
			}
		}
	}

	private async Task DeleteProfile(MatrixProfileModel profile)
	{
		if (profile != null && _profiles.Count > 1 && new CustomerDialog(Utils.GetString("DeleteView"), Utils.GetString("MatrixDeleteViewMessage"), Utils.GetString("Delete"), Utils.GetString("Cancel"), this).ShowDialog() == true)
		{
			await MatrixProfileDao.DeleteProfileAsync(profile.sid);
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
			Reload();
		}
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
		Close();
	}

	public void Ok()
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/eisenhower/managematrixviewswindow.xaml", UriKind.Relative);
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
			RootScroll = (ScrollViewer)target;
			break;
		case 2:
			ProfilesItems = (ItemsControl)target;
			break;
		case 4:
			AddViewBorder = (Border)target;
			AddViewBorder.MouseLeftButtonUp += AddRow_MouseLeftButtonUp;
			break;
		case 5:
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
			((Border)target).MouseLeftButtonUp += ProfileRow_MouseLeftButtonUp;
		}
	}
}
