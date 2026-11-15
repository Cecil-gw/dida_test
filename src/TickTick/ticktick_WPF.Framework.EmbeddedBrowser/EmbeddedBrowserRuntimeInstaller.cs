using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using TickTickUtils.Lang;
using ticktick_WPF.Util;
using ticktick_WPF.Views;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class EmbeddedBrowserRuntimeInstaller
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WinTrustFileInfo
	{
		public uint StructSize;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string FilePath;

		public IntPtr FileHandle;

		public IntPtr KnownSubject;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct WinTrustData
	{
		public uint StructSize;

		public IntPtr PolicyCallbackData;

		public IntPtr SIPClientData;

		public uint UIChoice;

		public uint RevocationChecks;

		public uint UnionChoice;

		public IntPtr FileInfo;

		public uint StateAction;

		public IntPtr StateData;

		public IntPtr URLReference;

		public uint ProviderFlags;

		public uint UIContext;
	}

	private static readonly SemaphoreSlim InstallLock = new SemaphoreSlim(1, 1);

	private static volatile bool _restartRequiredAfterRepair;

	private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

	public static async Task<EmbeddedBrowserPreparationResult> PrepareAndInitializeAsync(IEmbeddedBrowser browser, EmbeddedBrowserOptions options, Window owner, Action<EmbeddedBrowserRuntimeInstallProgress> progress)
	{
		if (browser == null)
		{
			throw new ArgumentNullException("browser");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if (browser.Engine == EmbeddedBrowserEngine.WebView2 && _restartRequiredAfterRepair)
		{
			ShowRepairRestartRequired(owner);
			return EmbeddedBrowserPreparationResult.RestartRequired;
		}
		bool flag = browser.Engine == EmbeddedBrowserEngine.WebView2;
		if (flag)
		{
			flag = !(await EnsureInstalledAsync(owner, options.UseCase, progress, forceRepair: false));
		}
		if (flag)
		{
			UtilLog.Info("EmbeddedBrowser preparation unavailable: runtime installation did not complete, use_case=" + options.UseCase);
			return EmbeddedBrowserPreparationResult.Unavailable;
		}
		EmbeddedBrowserPreparationResult result = default(EmbeddedBrowserPreparationResult);
		Exception ex2 = default(Exception);
		int num;
		try
		{
			await browser.InitializeAsync(options);
			result = EmbeddedBrowserPreparationResult.Ready;
			return result;
		}
		catch (Exception ex) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			ex2 = ex;
			return browser.Engine == EmbeddedBrowserEngine.WebView2;
		}).Invoke())
		{
			num = 1;
		}
		if (num != 1)
		{
			return result;
		}
		EmbeddedBrowserDiagnostics.RecordException("initialization_failed use_case=" + options.UseCase, ex2);
		EmbeddedBrowserDiagnostics.RecordFailureSnapshotOnce("first_initialization_failure");
		EmbeddedBrowserTelemetry.Record(options.UseCase, browser.Engine, browser.RuntimeVersion, "init_repair_prompt", null, null, ex2.GetType().Name);
		if (_restartRequiredAfterRepair)
		{
			EmbeddedBrowserTelemetry.Record(options.UseCase, browser.Engine, browser.RuntimeVersion, "init_restart_required", null, null, ex2.GetType().Name);
			ShowRepairRestartRequired(owner);
			return EmbeddedBrowserPreparationResult.RestartRequired;
		}
		if (!ConfirmInitializationRepair(owner))
		{
			EmbeddedBrowserTelemetry.Record(options.UseCase, browser.Engine, browser.RuntimeVersion, "init_repair_canceled", null, null, ex2.GetType().Name);
			return EmbeddedBrowserPreparationResult.Unavailable;
		}
		if (!(await EnsureInstalledAsync(owner, options.UseCase, progress, forceRepair: true)))
		{
			return EmbeddedBrowserPreparationResult.Unavailable;
		}
		ShowRepairRestartRequired(owner);
		return EmbeddedBrowserPreparationResult.RestartRequired;
	}

	private static async Task<bool> EnsureInstalledAsync(Window owner, string useCase, Action<EmbeddedBrowserRuntimeInstallProgress> progress, bool forceRepair)
	{
		if (forceRepair && _restartRequiredAfterRepair)
		{
			return true;
		}
		if (!forceRepair && EmbeddedBrowserFactory.IsWebView2RuntimeAvailable())
		{
			return true;
		}
		await InstallLock.WaitAsync();
		try
		{
			if (forceRepair && _restartRequiredAfterRepair)
			{
				return true;
			}
			if (!forceRepair && EmbeddedBrowserFactory.IsWebView2RuntimeAvailable())
			{
				return true;
			}
			if (!CanShowPrompt(owner))
			{
				EmbeddedBrowserTelemetry.Record(useCase, EmbeddedBrowserEngine.WebView2, null, "runtime_install_owner_closed", null, null);
				return false;
			}
			if (!forceRepair && !ConfirmInstall(owner))
			{
				EmbeddedBrowserTelemetry.Record(useCase, EmbeddedBrowserEngine.WebView2, null, "runtime_install_canceled", null, null);
				return false;
			}
			while (true)
			{
				try
				{
					EmbeddedBrowserDiagnostics.Write("runtime_install_start", "use_case=" + useCase + " force_repair=" + forceRepair);
					await DownloadAndInstallAsync(progress);
					if (await WaitUntilRuntimeAvailableAsync())
					{
						if (forceRepair)
						{
							_restartRequiredAfterRepair = true;
						}
						progress?.Invoke(new EmbeddedBrowserRuntimeInstallProgress(forceRepair ? GetText("网页组件修复完成，请重启应用后再试", "The web component was repaired. Restart the app to continue.") : GetText("正在打开网页…", "Opening the page…"), null, isInstalling: false));
						EmbeddedBrowserTelemetry.Record(useCase, EmbeddedBrowserEngine.WebView2, GetRuntimeVersion(), forceRepair ? "runtime_repaired_restart_required" : "runtime_installed", null, null);
						return true;
					}
					throw new InvalidOperationException("WebView2 Runtime is unavailable after installation.");
				}
				catch (Exception ex)
				{
					EmbeddedBrowserDiagnostics.RecordException("runtime_install_failed use_case=" + useCase + " force_repair=" + forceRepair, ex);
					EmbeddedBrowserTelemetry.Record(useCase, EmbeddedBrowserEngine.WebView2, null, "runtime_install_failed", null, null, ex.GetType().Name);
					progress?.Invoke(new EmbeddedBrowserRuntimeInstallProgress(GetText("网页组件安装失败", "The web component couldn't be installed"), null, isInstalling: false));
					if (!ConfirmRetry(owner))
					{
						return false;
					}
				}
			}
		}
		finally
		{
			InstallLock.Release();
		}
	}

	private static async Task DownloadAndInstallAsync(Action<EmbeddedBrowserRuntimeInstallProgress> progress)
	{
		string downloadDirectory = Path.Combine(Path.GetTempPath(), "TickTick-WebView2");
		Directory.CreateDirectory(downloadDirectory);
		string installerPath = Path.Combine(downloadDirectory, "MicrosoftEdgeWebview2Setup-" + Guid.NewGuid().ToString("N") + ".exe");
		try
		{
			progress?.Invoke(new EmbeddedBrowserRuntimeInstallProgress(GetText("正在下载网页组件…", "Downloading the web component…"), 0, isInstalling: false));
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			using (WebClient client = new WebClient())
			{
				client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
				{
					progress?.Invoke(new EmbeddedBrowserRuntimeInstallProgress(GetText("正在下载网页组件…", "Downloading the web component…"), args.ProgressPercentage, isInstalling: false));
				};
				await client.DownloadFileTaskAsync(new Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703"), installerPath);
			}
			EmbeddedBrowserDiagnostics.Write("bootstrapper_downloaded", "Verifying signature.");
			VerifyMicrosoftSignature(installerPath);
			EmbeddedBrowserDiagnostics.Write("bootstrapper_verified", "Starting installer.");
			progress?.Invoke(new EmbeddedBrowserRuntimeInstallProgress(GetText("正在下载并安装网页组件…", "Downloading and installing the web component…"), null, isInstalling: true));
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = installerPath,
				Arguments = "/silent /install",
				WorkingDirectory = downloadDirectory,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			if (process == null)
			{
				throw new InvalidOperationException("The WebView2 Runtime installer couldn't be started.");
			}
			int num = await WaitForExitAsync(process);
			EmbeddedBrowserDiagnostics.Write("installer_exit", "code=" + num + " hex=0x" + num.ToString("X8"));
			EmbeddedBrowserDiagnostics.RecordRuntimeSnapshot("after_installer_exit");
			if (num != 0)
			{
				throw new InvalidOperationException("The WebView2 Runtime installer exited with code " + num + ".");
			}
		}
		finally
		{
			try
			{
				if (File.Exists(installerPath))
				{
					File.Delete(installerPath);
				}
			}
			catch
			{
			}
		}
	}

	private static Task<int> WaitForExitAsync(Process process)
	{
		TaskCompletionSource<int> completion = new TaskCompletionSource<int>();
		process.EnableRaisingEvents = true;
		process.Exited += delegate
		{
			try
			{
				completion.TrySetResult(process.ExitCode);
			}
			catch (Exception exception)
			{
				completion.TrySetException(exception);
			}
		};
		if (process.HasExited)
		{
			completion.TrySetResult(process.ExitCode);
		}
		return completion.Task;
	}

	private static async Task<bool> WaitUntilRuntimeAvailableAsync()
	{
		for (int attempt = 0; attempt < 20; attempt++)
		{
			if (EmbeddedBrowserFactory.IsWebView2RuntimeAvailable())
			{
				return true;
			}
			await Task.Delay(500);
		}
		return false;
	}

	private static string GetRuntimeVersion()
	{
		try
		{
			return CoreWebView2Environment.GetAvailableBrowserVersionString();
		}
		catch
		{
			return null;
		}
	}

	private static bool ConfirmInstall(Window owner)
	{
		return new CustomerDialog(GetText("安装网页组件", "Install web component"), GetText("此功能需要 Microsoft Edge WebView2 网页组件。是否现在联网下载安装？", "This feature requires the Microsoft Edge WebView2 component. Download and install it now?"), GetText("安装", "Install"), GetText("取消", "Cancel"), owner).ShowDialog() == true;
	}

	private static bool ConfirmRetry(Window owner)
	{
		if (!CanShowPrompt(owner))
		{
			return false;
		}
		return new CustomerDialog(GetText("网页组件安装失败", "Web component installation failed"), GetText("请检查网络连接或系统策略后重试。", "Check your network connection or system policy, then try again."), GetText("重试", "Retry"), GetText("取消", "Cancel"), owner).ShowDialog() == true;
	}

	private static bool ConfirmInitializationRepair(Window owner)
	{
		if (!CanShowPrompt(owner))
		{
			return false;
		}
		return new CustomerDialog(GetText("网页组件无法启动", "The web component couldn't start"), GetText("Microsoft Edge WebView2 网页组件无法启动。可以现在重新安装，完成后需要重启应用。", "Microsoft Edge WebView2 couldn't start. You can reinstall it now, then restart the app."), GetText("重新安装", "Reinstall"), GetText("取消", "Cancel"), owner).ShowDialog() == true;
	}

	private static void ShowRepairRestartRequired(Window owner)
	{
		if (CanShowPrompt(owner))
		{
			new CustomerDialog(GetText("需要重启应用", "Restart required"), GetText("Microsoft Edge WebView2 网页组件已修复，请重启应用后重新打开此功能。", "Microsoft Edge WebView2 was repaired. Restart the app, then open this feature again."), MessageBoxButton.OK, owner).ShowDialog();
		}
	}

	private static bool CanShowPrompt(Window owner)
	{
		if (owner != null)
		{
			if (owner.IsLoaded && owner.IsVisible && !owner.Dispatcher.HasShutdownStarted)
			{
				return !owner.Dispatcher.HasShutdownFinished;
			}
			return false;
		}
		return true;
	}

	private static string GetText(string chinese, string english)
	{
		string language = Utils.GetLanguage();
		if (string.IsNullOrEmpty(language) || !language.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return english;
		}
		return chinese;
	}

	private static void VerifyMicrosoftSignature(string filePath)
	{
		WinTrustFileInfo structure = new WinTrustFileInfo
		{
			StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo)),
			FilePath = filePath,
			FileHandle = IntPtr.Zero,
			KnownSubject = IntPtr.Zero
		};
		IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
		try
		{
			Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
			WinTrustData trustData = new WinTrustData
			{
				StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData)),
				UIChoice = 2u,
				RevocationChecks = 0u,
				UnionChoice = 1u,
				FileInfo = intPtr,
				StateAction = 0u,
				ProviderFlags = 0u,
				UIContext = 0u
			};
			Guid actionId = WinTrustActionGenericVerifyV2;
			uint num = WinVerifyTrust(new IntPtr(-1), ref actionId, ref trustData);
			EmbeddedBrowserDiagnostics.Write("bootstrapper_signature", "win_verify_trust=0x" + num.ToString("X8"));
			if (num != 0)
			{
				throw new InvalidDataException("The WebView2 bootstrapper has an invalid signature.");
			}
			using X509Certificate2 x509Certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
			if (!string.Equals(x509Certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false), "Microsoft Corporation", StringComparison.Ordinal))
			{
				throw new InvalidDataException("The WebView2 bootstrapper isn't signed by Microsoft.");
			}
		}
		finally
		{
			Marshal.DestroyStructure(intPtr, typeof(WinTrustFileInfo));
			Marshal.FreeCoTaskMem(intPtr);
		}
	}

	[DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
	private static extern uint WinVerifyTrust(IntPtr windowHandle, ref Guid actionId, ref WinTrustData trustData);
}
