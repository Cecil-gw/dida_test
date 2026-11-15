using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;

namespace ticktick_WPF.Views.Tag;

public class BatchAddTagWindow : MyWindow, IComponentConnector
{
	private TagSelectData _originalDatas;

	private readonly bool _useInDetail;

	private Window _context;

	private bool _locationSet;

	private readonly System.Windows.Point _position;

	private bool _isClosed;

	private bool _inFilter;

	private Dictionary<string, long> _sortDict;

	private bool _canDelete;

	private readonly List<string> _needAddTags = new List<string>();

	internal BatchSetTagControl SetTagControl;

	private bool _contentLoaded;

	public bool IsClosed => _isClosed;

	public event EventHandler<TagSelectData> TagsSelect;

	public event EventHandler WindowHide;

	public BatchAddTagWindow(Window context, TagSelectData tags, bool useInDetail = true, System.Windows.Point position = default(System.Windows.Point))
	{
		InitializeComponent();
		_context = context;
		_position = position;
		_useInDetail = useInDetail;
		SetTagControl.Init(tags, firstInit: true);
		SetTagControl.Close += delegate
		{
			CloseWindow();
		};
		SetTagControl.TagsSelect += delegate(object o, TagSelectData e)
		{
			this.TagsSelect?.Invoke(o, e);
		};
	}

	private static System.Windows.Point GetMousePosition()
	{
		System.Drawing.Point mousePosition = Control.MousePosition;
		return new System.Windows.Point(mousePosition.X, mousePosition.Y);
	}

	private void SetWindowLocation()
	{
		if (!_useInDetail)
		{
			if (_locationSet)
			{
				return;
			}
			_locationSet = true;
			if (_position == default(System.Windows.Point))
			{
				CompositionTarget compositionTarget = PresentationSource.FromVisual(this)?.CompositionTarget;
				if (compositionTarget != null)
				{
					System.Windows.Point point = compositionTarget.TransformFromDevice.Transform(GetMousePosition());
					base.Left = point.X - base.ActualWidth / 2.0;
					base.Top = point.Y - base.ActualHeight / 2.0 - 40.0;
				}
			}
			else
			{
				base.Left = _position.X;
				base.Top = _position.Y;
			}
			return;
		}
		if (_context == null)
		{
			_context = App.Window;
		}
		double actualWidth = _context.ActualWidth;
		double actualHeight = _context.ActualHeight;
		double num = _context.Left;
		double num2 = _context.Top;
		if (_context.WindowState == WindowState.Maximized)
		{
			num = -10.0;
			num2 = -10.0;
		}
		base.Left = actualWidth + num - base.ActualWidth;
		base.Top = actualHeight + num2 - base.ActualHeight - 40.0;
	}

	private void OnWindowDeactivated(object sender, EventArgs e)
	{
		CloseWindow();
	}

	private void CloseWindow()
	{
		if (!_isClosed)
		{
			_isClosed = true;
			this.WindowHide?.Invoke(this, null);
			this.TagsSelect = null;
			this.WindowHide = null;
			Close();
		}
	}

	private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
	{
		SetWindowLocation();
	}

	public void ClearEvent()
	{
		this.TagsSelect = null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/tag/batchaddtagwindow.xaml", UriKind.Relative);
			System.Windows.Application.LoadComponent(this, resourceLocator);
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
		if (connectionId == 1)
		{
			SetTagControl = (BatchSetTagControl)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
