using System;
using System.Globalization;

namespace ticktick_WPF.Util;

public class MathUtil
{
	public static bool DoubleGe(double left, double right, double precision)
	{
		if (!(Math.Abs(left - right) < precision))
		{
			return left > right;
		}
		return true;
	}

	public static bool DoubleGt(double left, double right, double precision)
	{
		if (Math.Abs(left - right) >= precision)
		{
			return left > right;
		}
		return false;
	}

	public static bool DoubleLe(double left, double right, double precision)
	{
		if (!(Math.Abs(left - right) < precision))
		{
			return left < right;
		}
		return true;
	}

	public static bool DoubleLt(double left, double right, double precision)
	{
		if (Math.Abs(left - right) >= precision)
		{
			return left < right;
		}
		return false;
	}

	public static bool DoubleEq(double left, double right, double precision)
	{
		return Math.Abs(left - right) < precision;
	}

	public static bool TryParseString2DoubleWithoutFormat(string str, out double db)
	{
		return double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out db);
	}

	public static bool TryParseString2Double(string str, out double db)
	{
		return double.TryParse(str, out db);
	}

	public static long LongAvg(long left, long right)
	{
		return left / 2 + right / 2;
	}

	public static bool Between(double val, double d1, double d2, bool checkEqual = true)
	{
		double num = Math.Max(d1, d2);
		double num2 = Math.Min(d1, d2);
		if (checkEqual)
		{
			if (val >= num2)
			{
				return val <= num;
			}
			return false;
		}
		if (val > num2)
		{
			return val < num;
		}
		return false;
	}

	public static bool Between(int val, int d1, int d2, bool checkEqual = true)
	{
		int num = Math.Max(d1, d2);
		int num2 = Math.Min(d1, d2);
		if (checkEqual)
		{
			if (val >= num2)
			{
				return val <= num;
			}
			return false;
		}
		if (val > num2)
		{
			return val < num;
		}
		return false;
	}

	public static int Ceiling(double d)
	{
		int num = (int)d;
		return num + ((d - (double)num > 0.1) ? 1 : 0);
	}
}
