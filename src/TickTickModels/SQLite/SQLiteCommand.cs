using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using TickTickUtils.Lang;

namespace SQLite;

public class SQLiteCommand
{
	private class Binding
	{
		public string Name { get; set; }

		public object Value { get; set; }

		public int Index { get; set; }
	}

	private SQLiteConnection _conn;

	private List<Binding> _bindings;

	internal static IntPtr NegativePointer = new IntPtr(-1);

	private static int times = 10;

	public string CommandText { get; set; }

	internal SQLiteCommand(SQLiteConnection conn)
	{
		_conn = conn;
		_bindings = new List<Binding>();
		CommandText = "";
	}

	public int ExecuteNonQuery()
	{
		_ = _conn.Trace;
		SQLite3.Result result = SQLite3.Result.OK;
		IntPtr stmt = Prepare();
		result = SQLite3.Step(stmt);
		Finalize(stmt);
		switch (result)
		{
		case SQLite3.Result.Done:
			return SQLite3.Changes(_conn.Handle);
		case SQLite3.Result.Error:
		{
			string errmsg = SQLite3.GetErrmsg(_conn.Handle);
			throw SQLiteException.New(result, errmsg);
		}
		case SQLite3.Result.Constraint:
			if (SQLite3.ExtendedErrCode(_conn.Handle) == SQLite3.ExtendedResult.ConstraintNotNull)
			{
				throw NotNullConstraintViolationException.New(result, SQLite3.GetErrmsg(_conn.Handle));
			}
			break;
		}
		return -1;
	}

	public IEnumerable<T> ExecuteDeferredQuery<T>()
	{
		return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T)));
	}

	public List<T> ExecuteQuery<T>()
	{
		return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof(T))).ToList();
	}

	public List<T> ExecuteQuery<T>(TableMapping map)
	{
		return ExecuteDeferredQuery<T>(map).ToList();
	}

	protected virtual void OnInstanceCreated(object obj)
	{
	}

	public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
	{
		_ = _conn.Trace;
		Stopwatch sw = new Stopwatch();
		sw.Start();
		IntPtr stmt = Prepare();
		long prepareTime = sw.ElapsedMilliseconds;
		int resultCount = 0;
		try
		{
			TableMapping.Column[] cols = new TableMapping.Column[SQLite3.ColumnCount(stmt)];
			for (int i = 0; i < cols.Length; i++)
			{
				string columnName = SQLite3.ColumnName16(stmt, i);
				cols[i] = map.FindColumn(columnName);
			}
			while (SQLite3.Step(stmt) == SQLite3.Result.Row)
			{
				object obj = Activator.CreateInstance(map.MappedType);
				for (int j = 0; j < cols.Length; j++)
				{
					if (cols[j] != null)
					{
						SQLite3.ColType type = SQLite3.ColumnType(stmt, j);
						object val = ReadCol(stmt, j, type, cols[j].ColumnType);
						cols[j].SetValue(obj, val);
					}
				}
				OnInstanceCreated(obj);
				resultCount++;
				yield return (T)obj;
			}
		}
		finally
		{
			SQLiteCommand sQLiteCommand = this;
			SQLite3.Finalize(stmt);
			sw.Stop();
			if (sw.ElapsedMilliseconds > 30)
			{
				UtilLog.Info($"SlowSql Cost:{sw.ElapsedMilliseconds} ms, prepare: {prepareTime} ms, resultCount {resultCount}, Query: " + sQLiteCommand);
			}
		}
	}

	public T ExecuteScalar<T>()
	{
		_ = _conn.Trace;
		T result = default(T);
		IntPtr stmt = Prepare();
		try
		{
			SQLite3.Result result2 = SQLite3.Step(stmt);
			switch (result2)
			{
			case SQLite3.Result.Row:
			{
				SQLite3.ColType type = SQLite3.ColumnType(stmt, 0);
				result = (T)ReadCol(stmt, 0, type, typeof(T));
				return result;
			}
			case SQLite3.Result.Done:
				break;
			default:
				throw SQLiteException.New(result2, SQLite3.GetErrmsg(_conn.Handle));
			}
		}
		finally
		{
			Finalize(stmt);
		}
		return result;
	}

	public void Bind(string name, object val)
	{
		_bindings.Add(new Binding
		{
			Name = name,
			Value = val
		});
	}

	public void Bind(object val)
	{
		Bind(null, val);
	}

	public override string ToString()
	{
		string[] array = new string[1 + _bindings.Count];
		array[0] = CommandText;
		int num = 1;
		foreach (Binding binding in _bindings)
		{
			array[num] = $"  {num - 1}: {binding.Value}";
			num++;
		}
		return string.Join(Environment.NewLine, array);
	}

	private IntPtr Prepare()
	{
		IntPtr intPtr = SQLite3.Prepare2(_conn.Handle, CommandText);
		BindAll(intPtr);
		return intPtr;
	}

	private void Finalize(IntPtr stmt)
	{
		SQLite3.Finalize(stmt);
	}

	private void BindAll(IntPtr stmt)
	{
		int num = 1;
		foreach (Binding binding in _bindings)
		{
			if (binding.Name != null)
			{
				binding.Index = SQLite3.BindParameterIndex(stmt, binding.Name);
			}
			else
			{
				binding.Index = num++;
			}
			BindParameter(stmt, binding.Index, binding.Value, _conn.StoreDateTimeAsTicks);
		}
	}

	internal static void BindParameter(IntPtr stmt, int index, object value, bool storeDateTimeAsTicks)
	{
		if (value == null)
		{
			SQLite3.BindNull(stmt, index);
		}
		else if (value is int)
		{
			SQLite3.BindInt(stmt, index, (int)value);
		}
		else if (value is string)
		{
			SQLite3.BindText(stmt, index, (string)value, -1, NegativePointer);
		}
		else if (value is byte || value is ushort || value is sbyte || value is short)
		{
			SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
		}
		else if (value is bool)
		{
			SQLite3.BindInt(stmt, index, ((bool)value) ? 1 : 0);
		}
		else if (value is uint || value is long)
		{
			SQLite3.BindInt64(stmt, index, Convert.ToInt64(value));
		}
		else if (value is float || value is double || value is decimal)
		{
			SQLite3.BindDouble(stmt, index, Convert.ToDouble(value));
		}
		else if (value is TimeSpan)
		{
			SQLite3.BindInt64(stmt, index, ((TimeSpan)value).Ticks);
		}
		else if (value is DateTime dateTime)
		{
			if (storeDateTimeAsTicks)
			{
				SQLite3.BindInt64(stmt, index, dateTime.Ticks);
				return;
			}
			string val;
			try
			{
				val = dateTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss", CultureInfo.InvariantCulture);
			}
			catch
			{
				val = dateTime.ToString(CultureInfo.InvariantCulture);
				if (times-- > 0)
				{
					UtilLog.Info($"SQLite.ReadCol BindParameter {dateTime},{DateTime.Today}");
				}
			}
			SQLite3.BindText(stmt, index, val, -1, NegativePointer);
		}
		else if (value is DateTimeOffset)
		{
			SQLite3.BindInt64(stmt, index, ((DateTimeOffset)value).UtcTicks);
		}
		else if (value.GetType().IsEnum)
		{
			SQLite3.BindInt(stmt, index, Convert.ToInt32(value));
		}
		else if (value is byte[])
		{
			SQLite3.BindBlob(stmt, index, (byte[])value, ((byte[])value).Length, NegativePointer);
		}
		else
		{
			if (!(value is Guid))
			{
				throw new NotSupportedException("Cannot store type: " + value.GetType());
			}
			SQLite3.BindText(stmt, index, ((Guid)value/*cast due to .constrained prefix*/).ToString(), 72, NegativePointer);
		}
	}

	private object ReadCol(IntPtr stmt, int index, SQLite3.ColType type, Type clrType)
	{
		if (type == SQLite3.ColType.Null)
		{
			return null;
		}
		if (clrType == typeof(string))
		{
			return SQLite3.ColumnString(stmt, index);
		}
		if (clrType == typeof(int))
		{
			return SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(bool))
		{
			return SQLite3.ColumnInt(stmt, index) == 1;
		}
		if (clrType == typeof(double))
		{
			return SQLite3.ColumnDouble(stmt, index);
		}
		if (clrType == typeof(float))
		{
			return (float)SQLite3.ColumnDouble(stmt, index);
		}
		if (clrType == typeof(TimeSpan))
		{
			return new TimeSpan(SQLite3.ColumnInt64(stmt, index));
		}
		if (clrType == typeof(DateTime))
		{
			if (_conn.StoreDateTimeAsTicks)
			{
				return new DateTime(SQLite3.ColumnInt64(stmt, index));
			}
			string text = SQLite3.ColumnString(stmt, index);
			if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			{
				return result;
			}
			if (!string.IsNullOrEmpty(text))
			{
				if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
				{
					return result2;
				}
				if (DateTime.TryParseExact(text, "yyyy-MM-dd HH'.'mm'.'ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result3))
				{
					return result3;
				}
				if (times-- > 0)
				{
					UtilLog.Info($"SQLite.ReadCol FormatDateError {text},{DateTime.Today}");
				}
			}
			return null;
		}
		if (clrType == typeof(DateTimeOffset))
		{
			return new DateTimeOffset(SQLite3.ColumnInt64(stmt, index), TimeSpan.Zero);
		}
		if (clrType.IsEnum)
		{
			return SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(long))
		{
			return SQLite3.ColumnInt64(stmt, index);
		}
		if (clrType == typeof(uint))
		{
			return (uint)SQLite3.ColumnInt64(stmt, index);
		}
		if (clrType == typeof(decimal))
		{
			return (decimal)SQLite3.ColumnDouble(stmt, index);
		}
		if (clrType == typeof(byte))
		{
			return (byte)SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(ushort))
		{
			return (ushort)SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(short))
		{
			return (short)SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(sbyte))
		{
			return (sbyte)SQLite3.ColumnInt(stmt, index);
		}
		if (clrType == typeof(byte[]))
		{
			return SQLite3.ColumnByteArray(stmt, index);
		}
		if (clrType == typeof(Guid))
		{
			return new Guid(SQLite3.ColumnString(stmt, index));
		}
		throw new NotSupportedException("Don't know how to read " + clrType);
	}
}
