using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils.Lang;
using ticktick_WPF.Util;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal static class EmbeddedBrowserDiagnostics
{
	private static readonly object SyncRoot = new object();

	private static bool _environmentRecorded;

	private static bool _failureSnapshotRecorded;

	private static string _lastDetection;

	private static int _snapshotRunning;

	private static readonly TimeSpan SnapshotTimeout = TimeSpan.FromSeconds(5.0);

	[ThreadStatic]
	private static CancellationToken _snapshotCancellation;

	public static void RecordEnvironmentOnce()
	{
		try
		{
			lock (SyncRoot)
			{
				if (_environmentRecorded)
				{
					return;
				}
				_environmentRecorded = true;
			}
			string architecture = (Environment.Is64BitProcess ? "x64" : "x86");
			Capture("environment", () => new JObject
			{
				["app_version"] = ReadField("app_version", () => Utils.GetVersion(".")),
				["os"] = Environment.OSVersion.VersionString,
				["process_arch"] = architecture,
				["os_64bit"] = Environment.Is64BitOperatingSystem
			}.ToString(Formatting.None));
		}
		catch (Exception exception)
		{
			RecordException("environment_collection_failed", exception);
		}
	}

	public static void RecordRuntimeDetection(string version, Exception exception)
	{
		try
		{
			string text = ((exception == null) ? ("runtime=" + (string.IsNullOrWhiteSpace(version) ? "<empty>" : version)) : FormatException(exception));
			lock (SyncRoot)
			{
				if (string.Equals(_lastDetection, text, StringComparison.Ordinal))
				{
					return;
				}
				_lastDetection = text;
				Write("runtime_detection", text);
			}
			if (exception != null || string.IsNullOrWhiteSpace(version))
			{
				RecordFailureSnapshotOnce("first_detection_failure");
			}
		}
		catch
		{
		}
	}

	public static void RecordFailureSnapshotOnce(string stage)
	{
		lock (SyncRoot)
		{
			if (_failureSnapshotRecorded)
			{
				return;
			}
			_failureSnapshotRecorded = true;
		}
		RecordRuntimeSnapshot(stage);
	}

	public static void RecordRuntimeSnapshot(string stage)
	{
		QueueSnapshot(stage, delegate
		{
			CollectRuntimeSnapshot(stage);
		});
	}

	private static void QueueSnapshot(string stage, Action collect)
	{
		if (Interlocked.CompareExchange(ref _snapshotRunning, 1, 0) != 0)
		{
			Write(stage, "runtime_snapshot_skipped: another snapshot is still running");
		}
		else
		{
			RunSnapshotAsync(stage, collect);
		}
	}

	private static async Task RunSnapshotAsync(string stage, Action collect)
	{
		try
		{
			CancellationTokenSource cancellation = new CancellationTokenSource();
			try
			{
				Task worker = Task.Run(delegate
				{
					_snapshotCancellation = cancellation.Token;
					try
					{
						collect();
					}
					finally
					{
						_snapshotCancellation = default(CancellationToken);
					}
				});
				if (await Task.WhenAny(worker, Task.Delay(SnapshotTimeout)).ConfigureAwait(continueOnCapturedContext: false) != worker)
				{
					cancellation.Cancel();
					Write(stage, "runtime_snapshot_timeout: remaining probes canceled; waiting for pending IO");
				}
				await worker.ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				if (cancellation != null)
				{
					((IDisposable)cancellation).Dispose();
				}
			}
		}
		catch (Exception exception)
		{
			RecordException(stage + " snapshot_worker_failed", exception);
		}
		finally
		{
			Interlocked.Exchange(ref _snapshotRunning, 0);
		}
	}

	private static void CollectRuntimeSnapshot(string stage)
	{
		try
		{
			Write(stage, "runtime_snapshot_start");
			HashSet<string> versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<Action> list = new List<Action>();
			RegistryHive[] array = new RegistryHive[2]
			{
				RegistryHive.CurrentUser,
				RegistryHive.LocalMachine
			};
			for (int i = 0; i < array.Length; i++)
			{
				RegistryHive hive = array[i];
				RegistryView[] array2 = ((!Environment.Is64BitOperatingSystem) ? new RegistryView[1] { RegistryView.Registry32 } : new RegistryView[2]
				{
					RegistryView.Registry32,
					RegistryView.Registry64
				});
				for (int j = 0; j < array2.Length; j++)
				{
					RegistryView view = array2[j];
					Capture(stage + " registry " + hive.ToString() + " " + view, delegate
					{
						using RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, view);
						using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", writable: false);
						if (registryKey2 == null)
						{
							return "key_missing";
						}
						string text = registryKey2.GetValue("pv") as string;
						if (Version.TryParse(text, out var _))
						{
							versions.Add(text);
						}
						return "pv=" + (text ?? "<missing_or_not_string>");
					});
					RecordInstallationRegistration(stage, hive, view, "SOFTWARE\\Microsoft\\EdgeUpdate\\Clients\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", roots, list);
					RecordInstallationRegistration(stage, hive, view, "SOFTWARE\\Microsoft\\EdgeUpdate\\ClientState\\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", roots, list);
					RecordInstallationRegistration(stage, hive, view, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Microsoft EdgeWebView", roots, list);
				}
			}
			Write(stage, "registration_collection_complete");
			foreach (Action item in list)
			{
				if (!_snapshotCancellation.IsCancellationRequested)
				{
					item();
					continue;
				}
				break;
			}
			Capture(stage + " app_assembly", () => Assembly.GetEntryAssembly()?.GetName().FullName);
			Capture(stage + " loader_candidates", delegate
			{
				string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
				string text = (Environment.Is64BitProcess ? "x64" : "x86");
				string[] array4 = new string[3]
				{
					"WebView2Loader.dll",
					Path.Combine(text, "WebView2Loader.dll"),
					Path.Combine("runtimes", "win-" + text, "native", "WebView2Loader.dll")
				};
				foreach (string path in array4)
				{
					RecordFile(stage + " loader_candidate", Path.Combine(baseDirectory, path));
				}
				return "app_directory=" + baseDirectory;
			});
			Capture(stage + " loaded_webview2_assemblies", delegate
			{
				JArray jArray = new JArray();
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int k = 0; k < assemblies.Length; k++)
				{
					AssemblyName name = assemblies[k].GetName();
					if (name.Name.StartsWith("Microsoft.Web.WebView2", StringComparison.Ordinal))
					{
						jArray.Add(name.FullName);
					}
				}
				return jArray.ToString(Formatting.None);
			});
			Capture(stage + " api_probe", ProbeRuntimeVersion);
			Capture(stage + " loaded_loader_modules", delegate
			{
				JArray jArray = new JArray();
				using (Process process = Process.GetCurrentProcess())
				{
					foreach (ProcessModule module in process.Modules)
					{
						if (string.Equals(module.ModuleName, "WebView2Loader.dll", StringComparison.OrdinalIgnoreCase))
						{
							jArray.Add(module.FileName);
							RecordFile(stage + " loaded_loader", module.FileName);
						}
					}
				}
				return jArray.ToString(Formatting.None);
			});
			Environment.SpecialFolder[] array3 = new Environment.SpecialFolder[3]
			{
				Environment.SpecialFolder.LocalApplicationData,
				Environment.SpecialFolder.ProgramFiles,
				Environment.SpecialFolder.ProgramFilesX86
			};
			for (int i = 0; i < array3.Length; i++)
			{
				Environment.SpecialFolder folder = array3[i];
				Capture(stage + " runtime_root " + folder, delegate
				{
					string folderPath = Environment.GetFolderPath(folder);
					if (!string.IsNullOrEmpty(folderPath))
					{
						roots.Add(Path.Combine(folderPath, "Microsoft", "EdgeWebView", "Application"));
					}
					return (!string.IsNullOrEmpty(folderPath)) ? folderPath : "<unset>";
				});
			}
			Capture(stage + " runtime_root ProgramW6432", delegate
			{
				string environmentVariable = Environment.GetEnvironmentVariable("ProgramW6432");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					roots.Add(Path.Combine(environmentVariable, "Microsoft", "EdgeWebView", "Application"));
				}
				return environmentVariable ?? "<unset>";
			});
			foreach (string root in roots)
			{
				foreach (string item2 in versions)
				{
					RecordFile(stage + " registered_version_candidate", Path.Combine(root, item2, "msedgewebview2.exe"));
				}
				Capture(stage + " runtime_directory " + root, delegate
				{
					Version result;
					string[] array4 = (from path in Directory.GetDirectories(root)
						where Version.TryParse(Path.GetFileName(path), out result)
						orderby new Version(Path.GetFileName(path)) descending
						select path).ToArray();
					foreach (string item3 in array4.Take(10))
					{
						if (!versions.Contains(Path.GetFileName(item3)))
						{
							RecordFile(stage + " unregistered_version_candidate", Path.Combine(item3, "msedgewebview2.exe"));
						}
					}
					return "version_directory_count=" + array4.Length + " inspected_limit=10";
				});
			}
			Capture(stage + " runtime_override", delegate
			{
				string environmentVariable = Environment.GetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER");
				if (!string.IsNullOrWhiteSpace(environmentVariable))
				{
					RecordFile(stage + " override_candidate", Path.Combine(environmentVariable, "msedgewebview2.exe"));
				}
				return environmentVariable ?? "<unset>";
			});
			Write(stage, _snapshotCancellation.IsCancellationRequested ? "runtime_snapshot_stopped" : "runtime_snapshot_end");
		}
		catch (Exception exception)
		{
			RecordException(stage + " snapshot_failed", exception);
		}
	}

	private static void RecordInstallationRegistration(string stage, RegistryHive hive, RegistryView view, string keyPath, ISet<string> roots, ICollection<Action> probes)
	{
		string context = stage + " installation_registration " + hive.ToString() + " " + view.ToString() + " " + keyPath;
		JObject values = new JObject();
		Capture(context, delegate
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, view);
			RegistryKey key = registryKey.OpenSubKey(keyPath, writable: false);
			try
			{
				if (key == null)
				{
					return "key_missing";
				}
				string[] array = new string[8] { "location", "InstallLocation", "DisplayVersion", "UninstallString", "UninstallCmdLine", "UninstallArguments", "ModifyPath", "DisplayIcon" };
				foreach (string name in array)
				{
					string text = ReadField(context + " " + name, () => key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string);
					if (!(text == "<null>"))
					{
						values[name] = text;
					}
				}
				return values.ToString(Formatting.None);
			}
			finally
			{
				if (key != null)
				{
					((IDisposable)key).Dispose();
				}
			}
		});
		foreach (JProperty property in values.Properties())
		{
			if (property.Name == "DisplayVersion" || property.Name == "UninstallArguments")
			{
				continue;
			}
			probes.Add(delegate
			{
				Capture(context + " " + property.Name + " path_check", delegate
				{
					InspectRegistrationPath(context + " " + property.Name, property.Name, (string?)property.Value, roots);
					return "complete";
				});
			});
		}
	}

	private static void InspectRegistrationPath(string stage, string name, string value, ISet<string> roots)
	{
		switch (name)
		{
		case "location":
		case "InstallLocation":
		{
			string directory = Environment.ExpandEnvironmentVariables(value).Trim().Trim('"');
			if (!Path.IsPathRooted(directory))
			{
				Write(stage, "not_an_absolute_path");
				break;
			}
			roots.Add(directory);
			Capture(stage + " directory " + directory, () => "attributes=" + File.GetAttributes(directory));
			RecordFile(stage + " direct_runtime_candidate", Path.Combine(directory, "msedgewebview2.exe"));
			break;
		}
		case "UninstallString":
		case "UninstallCmdLine":
		case "ModifyPath":
		case "DisplayIcon":
		{
			string registeredExecutablePath = GetRegisteredExecutablePath(value);
			if (registeredExecutablePath == null)
			{
				Write(stage, "executable_path_not_recognized");
				break;
			}
			RecordFile(stage + " executable", registeredExecutablePath);
			string directoryName = Path.GetDirectoryName(registeredExecutablePath);
			if (string.Equals(Path.GetFileName(registeredExecutablePath), "setup.exe", StringComparison.OrdinalIgnoreCase) && string.Equals(Path.GetFileName(directoryName), "Installer", StringComparison.OrdinalIgnoreCase))
			{
				string directoryName2 = Path.GetDirectoryName(directoryName);
				if (!string.IsNullOrEmpty(directoryName2))
				{
					RecordFile(stage + " uninstall_runtime_candidate", Path.Combine(directoryName2, "msedgewebview2.exe"));
				}
			}
			break;
		}
		}
	}

	private static string GetRegisteredExecutablePath(string value)
	{
		Match match = Regex.Match(Environment.ExpandEnvironmentVariables(value), "^\\s*(?:\"(?<path>[^\"]+\\.exe)\"|(?<path>(?:[a-z]:\\\\|\\\\\\\\).*?\\.exe))(?=\\s|,|$)", RegexOptions.IgnoreCase);
		string text = (match.Success ? match.Groups["path"].Value : null);
		if (text == null || !Path.IsPathRooted(text))
		{
			return null;
		}
		return text;
	}

	private static string ProbeRuntimeVersion()
	{
		return CoreWebView2Environment.GetAvailableBrowserVersionString() ?? "<null>";
	}

	private static string ReadField(string stage, Func<string> collect)
	{
		if (_snapshotCancellation.IsCancellationRequested)
		{
			return "<snapshot_timeout>";
		}
		try
		{
			return collect() ?? "<null>";
		}
		catch (FileNotFoundException)
		{
			return "missing";
		}
		catch (DirectoryNotFoundException)
		{
			return "missing";
		}
		catch (Exception exception)
		{
			RecordException(stage + " collection_failed", exception);
			return "<collection_failed>";
		}
	}

	private static void Capture(string stage, Func<string> collect)
	{
		if (!_snapshotCancellation.IsCancellationRequested)
		{
			Write(stage, ReadField(stage, collect));
		}
	}

	private static void RecordFile(string stage, string path)
	{
		Capture(stage + " file " + path, delegate
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using BinaryReader binaryReader = new BinaryReader(fileStream);
			if (fileStream.Length < 64 || binaryReader.ReadUInt16() != 23117)
			{
				throw new InvalidDataException("Missing DOS header.");
			}
			fileStream.Position = 60L;
			int num = binaryReader.ReadInt32();
			if (num < 64 || num > fileStream.Length - 6)
			{
				throw new InvalidDataException("Invalid PE header offset.");
			}
			fileStream.Position = num;
			if (binaryReader.ReadUInt32() != 17744)
			{
				throw new InvalidDataException("Missing PE signature.");
			}
			ushort num2 = binaryReader.ReadUInt16();
			string text = num2 switch
			{
				43620 => "arm64", 
				34404 => "x64", 
				332 => "x86", 
				_ => "unknown", 
			};
			return "readable=true bytes=" + fileStream.Length + " pe_machine=0x" + num2.ToString("X4") + " arch=" + text;
		});
		Capture(stage + " file_version " + path, () => FileVersionInfo.GetVersionInfo(path).FileVersion);
	}

	public static void RecordException(string stage, Exception exception)
	{
		try
		{
			Write(stage, FormatException(exception));
		}
		catch
		{
		}
	}

	private static string FormatException(Exception exception)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (Exception ex = exception; ex != null; ex = ex.InnerException)
		{
			stringBuilder.Append(ex.GetType().FullName).Append(" HRESULT=0x").Append(ex.HResult.ToString("X8"))
				.AppendLine();
		}
		return stringBuilder.Append(exception).ToString();
	}

	public static void Write(string stage, string message)
	{
		try
		{
			message = "EmbeddedBrowser diagnostic " + stage + ": " + message;
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (!string.IsNullOrEmpty(folderPath))
			{
				message = Regex.Replace(message, Regex.Escape(folderPath), "<user-profile>", RegexOptions.IgnoreCase);
				message = Regex.Replace(message, Regex.Escape(folderPath.Replace("\\", "\\\\")), "<user-profile>", RegexOptions.IgnoreCase);
			}
			message = Regex.Replace(message, "([a-z]:[\\\\/]+(?:Users|Documents and Settings)[\\\\/]+)[^\\\\/\\r\\n\"<>]+", "$1<user>", RegexOptions.IgnoreCase);
			UtilLog.Info(message);
		}
		catch
		{
		}
	}
}
