using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Views.CustomControl;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.CustomPopup;

public class SetClosedStatusPopup : EscPopup, IComponentConnector
{
	private CustomMenuList _moreMenu;

	private bool _isCompleted;

	private bool _isAbandoned;

	private bool _progressChangePending;

	private int? _progress;

	private bool _contentLoaded;

	public event EventHandler Abandoned;

	public event EventHandler Completed;

	public event Func<int, Task<bool>> ProgressChangeRequested;

	private SetClosedStatusPopup()
	{
		InitializeComponent();
		base.Closed += delegate
		{
			this.Abandoned = null;
			this.Completed = null;
			this.ProgressChangeRequested = null;
		};
	}

	public static SetClosedStatusPopup GetInstance(UIElement targetElement, bool isCompleted, bool isAbandoned, double verticalOffset = 0.0, double horizontalOffset = 0.0, int? progress = null)
	{
		SetClosedStatusPopup setClosedStatusPopup = new SetClosedStatusPopup
		{
			_isCompleted = isCompleted,
			_isAbandoned = isAbandoned,
			_progress = progress
		};
		if (targetElement != null)
		{
			setClosedStatusPopup.PlacementTarget = targetElement;
		}
		else
		{
			setClosedStatusPopup.Placement = PlacementMode.Mouse;
		}
		setClosedStatusPopup.VerticalOffset = verticalOffset;
		setClosedStatusPopup.HorizontalOffset = horizontalOffset;
		return setClosedStatusPopup;
	}

	public void Show()
	{
		List<CustomMenuItemViewModel> list = new List<CustomMenuItemViewModel>
		{
			new CustomMenuItemViewModel((object)"complete", Utils.GetString("Completed"), (Geometry)null, "")
			{
				Selected = _isCompleted
			},
			new CustomMenuItemViewModel((object)"abandoned", Utils.GetString("Abandoned"), (Geometry)null, "")
			{
				Selected = _isAbandoned
			}
		};
		if (_progress.HasValue)
		{
			int progress = Math.Max(0, Math.Min(100, _progress.Value));
			TaskStatusProgressControl progressContent = new TaskStatusProgressControl(progress);
			progressContent.ProgressChangeRequested += delegate(int pointerProgress)
			{
				ChangeProgressAsync(progressContent, pointerProgress);
			};
			list.Add(new CustomMenuItemViewModel(null));
			list.Add(new CustomMenuItemViewModel((object)"progress", string.Empty, (Geometry)null, "")
			{
				Selectable = false,
				ShowSelected = false,
				IsStandaloneContent = true,
				UIContent = progressContent
			});
		}
		CustomMenuList customMenuList = new CustomMenuList(list, this);
		customMenuList.Operated += OnSelected;
		customMenuList.Show();
	}

	private async Task ChangeProgressAsync(TaskStatusProgressControl progressControl, int pointerProgress)
	{
		int currentProgress = _progress.GetValueOrDefault();
		if (pointerProgress < 0 || pointerProgress == currentProgress || _progressChangePending)
		{
			return;
		}
		Func<int, Task<bool>> func = this.ProgressChangeRequested;
		if (func == null)
		{
			progressControl.SetProgress(currentProgress);
			return;
		}
		_progressChangePending = true;
		progressControl.SetProgressEnabled(enabled: false);
		try
		{
			Delegate[] invocationList = func.GetInvocationList();
			for (int i = 0; i < invocationList.Length; i++)
			{
				if (!(await ((Func<int, Task<bool>>)invocationList[i])(pointerProgress)))
				{
					progressControl.SetProgress(currentProgress);
					return;
				}
			}
			_progress = pointerProgress;
			progressControl.SetProgress(pointerProgress);
		}
		catch (Exception ex)
		{
			progressControl.SetProgress(currentProgress);
			UtilLog.Error("SetClosedStatusPopup.ChangeProgress failed: " + ex);
			Utils.Toast(Utils.GetString("OperateFailedMessage"));
		}
		finally
		{
			progressControl.SetProgressEnabled(enabled: true);
			_progressChangePending = false;
		}
	}

	private void OnSelected(object sender, object e)
	{
		if (e is string text)
		{
			if (text == "complete" && !_isCompleted)
			{
				this.Completed?.Invoke(null, null);
			}
			if (text == "abandoned" && !_isAbandoned)
			{
				this.Abandoned?.Invoke(null, null);
			}
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TickTick;component/views/custompopup/setclosedstatuspopup.xaml", UriKind.Relative);
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
		_contentLoaded = true;
	}
}
