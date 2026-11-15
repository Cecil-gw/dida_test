using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ticktick_WPF.Util;

internal sealed class ExceptionReportRateLimiter
{
	private sealed class ReportState
	{
		internal TimeSpan LastReport;

		internal long SuppressedCount;
	}

	private readonly object _locker = new object();

	private readonly Dictionary<string, ReportState> _lastReports = new Dictionary<string, ReportState>();

	private readonly TimeSpan _interval;

	private readonly Func<TimeSpan> _getElapsed;

	private readonly Action<string, long> _logSuppressed;

	internal ExceptionReportRateLimiter(TimeSpan interval, Func<TimeSpan> getElapsed = null, Action<string, long> logSuppressed = null)
	{
		if (interval <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException("interval");
		}
		_interval = interval;
		Stopwatch clock = Stopwatch.StartNew();
		_getElapsed = getElapsed ?? ((Func<TimeSpan>)(() => clock.Elapsed));
		_logSuppressed = logSuppressed;
	}

	internal bool ShouldReport(Exception exception)
	{
		if (exception == null)
		{
			return false;
		}
		string fingerprint = GetFingerprint(exception);
		List<KeyValuePair<string, ReportState>> list;
		bool result;
		lock (_locker)
		{
			TimeSpan now = _getElapsed();
			list = _lastReports.Where((KeyValuePair<string, ReportState> pair) => now - pair.Value.LastReport >= _interval).ToList();
			foreach (KeyValuePair<string, ReportState> item in list)
			{
				_lastReports.Remove(item.Key);
			}
			if (_lastReports.TryGetValue(fingerprint, out var value))
			{
				value.SuppressedCount++;
				result = false;
			}
			else
			{
				_lastReports[fingerprint] = new ReportState
				{
					LastReport = now
				};
				result = true;
			}
		}
		foreach (KeyValuePair<string, ReportState> item2 in list)
		{
			if (item2.Value.SuppressedCount > 0)
			{
				LogSuppressed(item2.Key, item2.Value.SuppressedCount);
			}
		}
		return result;
	}

	private void LogSuppressed(string fingerprint, long count)
	{
		try
		{
			_logSuppressed?.Invoke(fingerprint, count);
		}
		catch (Exception)
		{
		}
	}

	private static string GetFingerprint(Exception exception)
	{
		if (exception is AggregateException ex)
		{
			string[] array = ex.Flatten().InnerExceptions.Select(GetFingerprint).Distinct().OrderBy((string value) => value, StringComparer.Ordinal)
				.ToArray();
			if (array.Length == 1)
			{
				return array[0];
			}
			if (array.Length > 1)
			{
				return Hash(string.Join("\n", array));
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		AppendPart(stringBuilder, exception.GetType().FullName);
		AppendPart(stringBuilder, exception.Message);
		AppendPart(stringBuilder, exception.StackTrace);
		if (exception.InnerException != null)
		{
			AppendPart(stringBuilder, GetFingerprint(exception.InnerException));
		}
		return Hash(stringBuilder.ToString());
	}

	private static void AppendPart(StringBuilder signature, string value)
	{
		value = value ?? string.Empty;
		signature.Append(value.Length).Append(':').Append(value);
	}

	private static string Hash(string value)
	{
		using SHA256 sHA = SHA256.Create();
		return Convert.ToBase64String(sHA.ComputeHash(Encoding.UTF8.GetBytes(value)));
	}
}
