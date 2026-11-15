using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace ticktick_WPF.Views.AI;

internal static class NativeCandidateDecorationWorkaround
{
	private sealed class Attachment
	{
		private readonly TextBox _input;

		private readonly Action<string> _report;

		private DispatcherOperation _pending;

		private int _attempts;

		private string _lastReport;

		internal Attachment(TextBox input, Action<string> report)
		{
			_input = input;
			_report = report;
			input.Loaded += OnLoaded;
			input.GotKeyboardFocus += OnGotKeyboardFocus;
			input.Unloaded += OnUnloaded;
			if (input.IsLoaded)
			{
				Start();
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			Start();
		}

		private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
		{
			Start();
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			_pending?.Abort();
			_pending = null;
			if (!TryRemove(_input, out var status))
			{
				Report(status);
			}
		}

		private void Start()
		{
			_pending?.Abort();
			_pending = null;
			_attempts = 0;
			ApplyWhenReady();
		}

		private void ApplyWhenReady()
		{
			_pending = null;
			if (_input.IsLoaded && !_input.Dispatcher.HasShutdownStarted)
			{
				if (!TryApply(_input, out var status) && ++_attempts < 3)
				{
					_pending = _input.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(ApplyWhenReady));
				}
				else
				{
					Report(status);
				}
			}
		}

		private void Report(string result)
		{
			if (!(result == _lastReport))
			{
				_lastReport = result;
				_report?.Invoke(result);
			}
		}
	}

	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.None)]
	private sealed class EditSink : ITextEditSink
	{
		private readonly WeakReference<TextBox> _input;

		private readonly WeakReference<object> _store;

		private readonly MethodInfo _onEndEdit;

		internal int Cookie;

		internal int CallbackCount;

		private bool _isUpdating;

		internal EditSink(TextBox input, object store)
		{
			_input = new WeakReference<TextBox>(input);
			_store = new WeakReference<object>(store);
			_onEndEdit = store.GetType().GetInterfaces().Single((Type type) => type.Name == "ITfTextEditSink")
				.GetMethod("OnEndEdit");
		}

		internal bool TryGetStore(out object store)
		{
			return _store.TryGetTarget(out store);
		}

		public void OnEndEdit(object context, int readCookie, object editRecord)
		{
			CallbackCount++;
			if (!_store.TryGetTarget(out var target))
			{
				if (Marshal.IsComObject(editRecord))
				{
					Marshal.ReleaseComObject(editRecord);
				}
				return;
			}
			object obj = null;
			object obj2 = null;
			FieldInfo fieldInfo = null;
			try
			{
				_isUpdating = true;
				if (_input.TryGetTarget(out var target2))
				{
					obj = GetEditor(target2);
					fieldInfo = obj.GetType().GetField("_textView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					obj2 = fieldInfo.GetValue(obj);
					fieldInfo.SetValue(obj, new ProgressTextView(obj2).GetTransparentProxy());
				}
				_onEndEdit.Invoke(target, new object[3] { context, readCookie, editRecord });
				RemoveEmptyLinesFromStore(target);
			}
			finally
			{
				if (obj != null && obj2 != null)
				{
					fieldInfo.SetValue(obj, obj2);
				}
				_isUpdating = false;
			}
		}

		internal void OnLayoutUpdated(object sender, EventArgs args)
		{
			if (!_isUpdating && _store.TryGetTarget(out var target))
			{
				RemoveEmptyLinesFromStore(target);
			}
		}
	}

	private sealed class ProgressTextView : RealProxy
	{
		private readonly object _view;

		private readonly Type _pointerType;

		private readonly PropertyInfo _offset;

		private readonly MethodInfo _advance;

		private readonly MethodInfo _readText;

		private readonly Type _segmentType;

		private readonly PropertyInfo _segmentStart;

		private readonly PropertyInfo _segmentEnd;

		private readonly PropertyInfo _segmentIsNull;

		internal ProgressTextView(object view)
			: base(typeof(TextBox).Assembly.GetType("System.Windows.Documents.ITextView"))
		{
			_view = view;
			_pointerType = typeof(TextBox).Assembly.GetType("System.Windows.Documents.ITextPointer");
			_offset = _pointerType.GetProperty("Offset");
			_advance = _pointerType.GetMethod("CreatePointer", new Type[2]
			{
				typeof(int),
				typeof(LogicalDirection)
			});
			_readText = _pointerType.GetMethod("GetTextInRun", new Type[4]
			{
				typeof(LogicalDirection),
				typeof(char[]),
				typeof(int),
				typeof(int)
			});
			_segmentType = typeof(TextBox).Assembly.GetType("System.Windows.Documents.TextSegment");
			_segmentStart = _segmentType.GetProperty("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_segmentEnd = _segmentType.GetProperty("End", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			_segmentIsNull = _segmentType.GetProperty("IsNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public override IMessage Invoke(IMessage message)
		{
			IMethodCallMessage methodCallMessage = (IMethodCallMessage)message;
			try
			{
				MethodInfo methodInfo = (MethodInfo)methodCallMessage.MethodBase;
				object[] args = methodCallMessage.Args;
				object obj = methodInfo.Invoke(_view, args);
				if (methodInfo.Name == "GetLineRange")
				{
					obj = EnsureProgress(methodInfo, args[0], obj);
				}
				return new ReturnMessage(obj, args, args.Length, methodCallMessage.LogicalCallContext, methodCallMessage);
			}
			catch (Exception ex)
			{
				return new ReturnMessage(ex.InnerException ?? ex, methodCallMessage);
			}
		}

		private object EnsureProgress(MethodInfo getLineRange, object pointer, object segment)
		{
			int num = (int)_offset.GetValue(pointer, null);
			if (!(bool)_segmentIsNull.GetValue(segment, null) && (int)_offset.GetValue(_segmentEnd.GetValue(segment, null), null) > num)
			{
				return segment;
			}
			object value = typeof(TextBox).Assembly.GetType("System.Windows.Documents.ITextView").GetProperty("TextContainer").GetValue(_view, null);
			object value2 = typeof(TextBox).Assembly.GetType("System.Windows.Documents.ITextContainer").GetProperty("End").GetValue(value, null);
			int num2 = (int)_offset.GetValue(value2, null);
			if (num >= num2)
			{
				return segment;
			}
			char[] array = new char[1];
			if ((int)_readText.Invoke(pointer, new object[4]
			{
				LogicalDirection.Forward,
				array,
				0,
				1
			}) > 0 && (array[0] == '\r' || array[0] == '\n' || array[0] == '\u2028' || array[0] == '\u2029'))
			{
				bool flag = array[0] == '\r';
				object obj = _advance.Invoke(pointer, new object[2]
				{
					1,
					LogicalDirection.Forward
				});
				if (flag && num + 1 < num2 && (int)_readText.Invoke(obj, new object[4]
				{
					LogicalDirection.Forward,
					array,
					0,
					1
				}) > 0 && array[0] == '\n')
				{
					obj = _advance.Invoke(obj, new object[2]
					{
						1,
						LogicalDirection.Forward
					});
				}
				object obj2 = _pointerType.GetMethod("CreatePointer", new Type[1] { typeof(LogicalDirection) }).Invoke(obj, new object[1] { LogicalDirection.Backward });
				return Activator.CreateInstance(_segmentType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[3] { obj2, obj2, true }, null);
			}
			object obj3 = _advance.Invoke(pointer, new object[2]
			{
				1,
				LogicalDirection.Forward
			});
			object obj4 = getLineRange.Invoke(_view, new object[1] { obj3 });
			if (!(bool)_segmentIsNull.GetValue(obj4, null) && (int)_offset.GetValue(_segmentEnd.GetValue(obj4, null), null) > num && (int)_offset.GetValue(_segmentEnd.GetValue(obj4, null), null) > (int)_offset.GetValue(_segmentStart.GetValue(obj4, null), null))
			{
				return obj4;
			}
			object obj5 = _pointerType.GetMethod("CreatePointer", new Type[1] { typeof(LogicalDirection) }).Invoke(obj3, new object[1] { LogicalDirection.Backward });
			return Activator.CreateInstance(_segmentType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[3] { pointer, obj5, true }, null);
		}
	}

	[ComImport]
	[ComVisible(true)]
	[Guid("8127d409-ccd3-4683-967a-b43d5b482bf7")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITextEditSink
	{
		void OnEndEdit([MarshalAs(UnmanagedType.Interface)] object context, int readCookie, [MarshalAs(UnmanagedType.Interface)] object editRecord);
	}

	[ComImport]
	[Guid("4ea48a35-60ae-446f-8fd6-e6a8d82459f7")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface ITfSource
	{
		void AdviseSink(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] object sink, out int cookie);

		void UnadviseSink(int cookie);
	}

	private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly ConditionalWeakTable<object, EditSink> Sinks = new ConditionalWeakTable<object, EditSink>();

	private static readonly ConditionalWeakTable<TextBox, EditSink> InputSinks = new ConditionalWeakTable<TextBox, EditSink>();

	private static readonly DependencyProperty AttachmentProperty = DependencyProperty.RegisterAttached("Attachment", typeof(Attachment), typeof(NativeCandidateDecorationWorkaround));

	internal static void Attach(TextBox input, Action<string> report = null)
	{
		input.Dispatcher.VerifyAccess();
		if (input.GetValue(AttachmentProperty) == null)
		{
			input.SetValue(AttachmentProperty, new Attachment(input, report));
		}
	}

	internal static bool TryApply(TextBox input, out string status)
	{
		input.Dispatcher.VerifyAccess();
		object obj = null;
		try
		{
			if (!InputMethod.GetIsInputMethodEnabled(input) || InputMethod.GetIsInputMethodSuspended(input))
			{
				throw new InvalidOperationException("输入法已禁用或挂起，未修改候选通知。");
			}
			object store = GetStore(input);
			if (store == null)
			{
				throw new InvalidOperationException("TSF 尚未初始化。");
			}
			PropertyInfo property = store.GetType().GetProperty("EditSinkCookie", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int num = (int)property.GetValue(store, null);
			if (Sinks.TryGetValue(store, out var value) && value.Cookie == num)
			{
				input.LayoutUpdated -= value.OnLayoutUpdated;
				input.LayoutUpdated += value.OnLayoutUpdated;
				status = "已启用原生候选遍历修复。";
				return true;
			}
			if (num == -1)
			{
				throw new InvalidOperationException("原生候选通知未注册，不能叠加候选通知禁用方案。");
			}
			object editor = GetEditor(input);
			FieldInfo field = editor.GetType().GetField("_textView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object value2 = field.GetValue(editor);
			object transparentProxy = new ProgressTextView(value2).GetTransparentProxy();
			try
			{
				field.SetValue(editor, transparentProxy);
				if (transparentProxy != store.GetType().GetProperty("TextView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(store, null))
				{
					throw new NotSupportedException("当前 WPF 不通过 TextEditor 访问 TextView。");
				}
			}
			finally
			{
				field.SetValue(editor, value2);
			}
			PropertyInfo property2 = store.GetType().GetProperty("DocumentManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			object value3 = property2.GetValue(store, null);
			object[] array = new object[1];
			property2.PropertyType.GetMethod("GetBase").Invoke(value3, array);
			obj = array[0];
			ITfSource tfSource = (ITfSource)obj;
			EditSink editSink = new EditSink(input, store);
			Guid id = typeof(ITextEditSink).GUID;
			tfSource.AdviseSink(ref id, editSink, out var cookie);
			try
			{
				tfSource.UnadviseSink(num);
			}
			catch
			{
				tfSource.UnadviseSink(cookie);
				throw;
			}
			property.SetValue(store, cookie, null);
			editSink.Cookie = cookie;
			Sinks.Remove(store);
			Sinks.Add(store, editSink);
			if (InputSinks.TryGetValue(input, out var value4))
			{
				input.LayoutUpdated -= value4.OnLayoutUpdated;
			}
			InputSinks.Remove(input);
			InputSinks.Add(input, editSink);
			input.LayoutUpdated += editSink.OnLayoutUpdated;
			status = "已启用原生候选遍历修复。";
			return true;
		}
		catch (Exception ex)
		{
			status = "候选遍历修复未启用：" + (ex.InnerException ?? ex).Message;
			return false;
		}
		finally
		{
			if (obj != null && Marshal.IsComObject(obj))
			{
				Marshal.ReleaseComObject(obj);
			}
		}
	}

	internal static bool TryRemove(TextBox input, out string status)
	{
		input.Dispatcher.VerifyAccess();
		if (!InputSinks.TryGetValue(input, out var value))
		{
			status = "候选遍历修复未注册。";
			return true;
		}
		object obj = null;
		try
		{
			if (value.TryGetStore(out var store))
			{
				PropertyInfo property = store.GetType().GetProperty("EditSinkCookie", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				PropertyInfo property2 = store.GetType().GetProperty("DocumentManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				object value2 = property2.GetValue(store, null);
				if (value2 != null && (int)property.GetValue(store, null) == value.Cookie)
				{
					object[] array = new object[1];
					property2.PropertyType.GetMethod("GetBase").Invoke(value2, array);
					obj = array[0];
					ITfSource tfSource = (ITfSource)obj;
					Guid id = typeof(ITextEditSink).GUID;
					tfSource.AdviseSink(ref id, store, out var cookie);
					try
					{
						tfSource.UnadviseSink(value.Cookie);
					}
					catch
					{
						tfSource.UnadviseSink(cookie);
						throw;
					}
					property.SetValue(store, cookie, null);
				}
				Sinks.Remove(store);
			}
			input.LayoutUpdated -= value.OnLayoutUpdated;
			InputSinks.Remove(input);
			status = "已恢复原生候选回调。";
			return true;
		}
		catch (Exception ex)
		{
			status = "候选遍历修复清理失败：" + (ex.InnerException ?? ex).Message;
			return false;
		}
		finally
		{
			if (obj != null && Marshal.IsComObject(obj))
			{
				Marshal.ReleaseComObject(obj);
			}
		}
	}

	private static object GetEditor(TextBox input)
	{
		return typeof(TextBoxBase).GetProperty("TextEditor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(input, null);
	}

	private static object GetStore(TextBox input)
	{
		object editor = GetEditor(input);
		return editor.GetType().GetProperty("TextStore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(editor, null);
	}

	internal static int CallbackCount(TextBox input)
	{
		if (!Sinks.TryGetValue(GetStore(input), out var value))
		{
			return 0;
		}
		return value.CallbackCount;
	}

	private static void RemoveEmptyLines(object adorner)
	{
		if (adorner == null)
		{
			return;
		}
		IList obj = (IList)adorner.GetType().GetField("_attributeRanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(adorner);
		bool flag = false;
		foreach (object item in obj)
		{
			IList list = (IList)item.GetType().GetField("_compositionLines", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(item);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				object obj2 = list[num];
				Rect rect = (Rect)obj2.GetType().GetField("_startRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj2);
				Rect rect2 = (Rect)obj2.GetType().GetField("_endRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj2);
				if (Math.Abs(rect.X - rect2.X) < 0.01 && Math.Abs(rect.Bottom - rect2.Bottom) < 0.01)
				{
					list.RemoveAt(num);
					flag = true;
				}
			}
		}
		if (flag)
		{
			((Adorner)adorner).InvalidateVisual();
		}
	}

	private static void RemoveEmptyLinesFromStore(object store)
	{
		object obj = store.GetType().GetField("_textservicesproperty", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(store);
		object obj2 = obj?.GetType().GetField("_propertyRanges", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);
		RemoveEmptyLines(obj2?.GetType().GetField("_compositionAdorner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj2));
	}
}
