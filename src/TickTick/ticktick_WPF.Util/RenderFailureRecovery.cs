using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Views;

namespace ticktick_WPF.Util;

internal static class RenderFailureRecovery
{
	private const int RenderThreadFailure = -2003303418;

	public static bool IsRenderFailure(Exception exception)
	{
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			if (ex is COMException && ex.HResult == -2003303418)
			{
				return true;
			}
		}
		return false;
	}

	public static Form CreateNativeDialog(Exception exception, bool restart, Action primaryAction, Func<Task> secondaryAction, Action closingAction)
	{
		bool needDownload = UnhandledExceptionWindow.NeedsDownload(exception);
		string text = ExceptionUtils.BuildExceptionMessage(exception);
		UtilLog.Error(text);
		Form dialog = new Form
		{
			Text = Utils.GetString("ProgramUnhandledException"),
			Font = SystemFonts.MessageBoxFont,
			AutoScaleDimensions = new SizeF(96f, 96f),
			AutoScaleMode = AutoScaleMode.Dpi,
			ClientSize = new Size(780, 630),
			MinimumSize = new Size(440, 340),
			StartPosition = FormStartPosition.CenterScreen,
			TopMost = true,
			ShowInTaskbar = true,
			MinimizeBox = false
		};
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			Padding = new Padding(16)
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = Utils.GetString(needDownload ? "RedownloadMessage" : "UnhandledExceptionTitle"),
			AutoSize = true,
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 0, 0, 10)
		}, 0, 0);
		TextBox detailsBox = new TextBox
		{
			Name = "ExceptionDetails",
			Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n"),
			Multiline = true,
			ReadOnly = true,
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.Vertical,
			WordWrap = true,
			Margin = Padding.Empty,
			HideSelection = true,
			TabIndex = 2
		};
		tableLayoutPanel.Controls.Add(detailsBox, 0, 1);
		tableLayoutPanel.Controls.Add(new Label
		{
			Text = Utils.GetString("SendUnhandledExceptionMessage"),
			AutoSize = true,
			Dock = DockStyle.Fill,
			Margin = new Padding(0, 10, 0, 10)
		}, 0, 2);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			Anchor = AnchorStyles.Right,
			FlowDirection = FlowDirection.RightToLeft,
			WrapContents = false,
			Margin = Padding.Empty,
			TabIndex = 0
		};
		Button button = new Button
		{
			Name = "SecondaryAction",
			Text = Utils.GetString(needDownload ? "SendLog" : "Exit"),
			AutoSize = true,
			MinimumSize = new Size(100, 34),
			Margin = new Padding(8, 0, 0, 0),
			TabIndex = 1
		};
		Button primary = new Button
		{
			Name = "PrimaryAction",
			Text = Utils.GetString(needDownload ? "GoToDownload" : "Restart"),
			AutoSize = true,
			MinimumSize = new Size(100, 34),
			Margin = Padding.Empty,
			TabIndex = 0
		};
		primary.Click += delegate
		{
			primaryAction();
		};
		button.Click += async delegate
		{
			await secondaryAction();
			if (!needDownload && !dialog.IsDisposed)
			{
				dialog.Close();
			}
		};
		dialog.FormClosing += delegate
		{
			closingAction();
		};
		dialog.Shown += delegate
		{
			primary.Focus();
			detailsBox.Select(0, 0);
			UtilLog.Info($"NativeExceptionDialog shown hwnd=0x{dialog.Handle.ToInt64():X}, bounds={dialog.Bounds}");
		};
		dialog.FormClosed += delegate
		{
			UtilLog.Info("NativeExceptionDialog closed");
		};
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(primary);
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 3);
		dialog.Controls.Add(tableLayoutPanel);
		return dialog;
	}
}
