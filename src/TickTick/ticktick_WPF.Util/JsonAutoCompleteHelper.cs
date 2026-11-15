using System;
using System.Text;

namespace ticktick_WPF.Util;

internal class JsonAutoCompleteHelper
{
	private enum PendingKeyState
	{
		None,
		Started,
		Ended
	}

	private enum PotentialLiteral
	{
		None,
		N,
		Nu,
		Null,
		T,
		Tr,
		Tru,
		True,
		F,
		Fa,
		Fal,
		Fals,
		False
	}

	public static string CompleteJson(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "{}";
		}
		StringBuilder stringBuilder = new StringBuilder(input);
		bool flag = false;
		bool flag2 = false;
		PendingKeyState pendingKeyState = PendingKeyState.None;
		PotentialLiteral potentialLiteral = PotentialLiteral.None;
		char c = '\0';
		bool flag3 = false;
		int num = 0;
		int num2 = 0;
		foreach (char c2 in input)
		{
			if (flag2)
			{
				flag2 = false;
				continue;
			}
			switch (c2)
			{
			case '\\':
				if (flag)
				{
					flag2 = true;
				}
				continue;
			case '"':
				if (flag)
				{
					flag = false;
					pendingKeyState = ((pendingKeyState == PendingKeyState.Started && num > num2) ? PendingKeyState.Ended : PendingKeyState.None);
					potentialLiteral = PotentialLiteral.None;
					if (flag3 && pendingKeyState != PendingKeyState.Ended)
					{
						pendingKeyState = PendingKeyState.None;
					}
					continue;
				}
				flag = true;
				if (num > num2 && (c == '{' || c == ','))
				{
					pendingKeyState = PendingKeyState.Started;
					continue;
				}
				pendingKeyState = PendingKeyState.None;
				if (flag3)
				{
					potentialLiteral = PotentialLiteral.None;
				}
				continue;
			case ':':
				pendingKeyState = PendingKeyState.None;
				flag3 = true;
				potentialLiteral = PotentialLiteral.None;
				c = ':';
				continue;
			case '{':
				num++;
				pendingKeyState = PendingKeyState.None;
				flag3 = false;
				potentialLiteral = PotentialLiteral.None;
				c = '{';
				continue;
			case '[':
				num2++;
				pendingKeyState = PendingKeyState.None;
				flag3 = false;
				potentialLiteral = PotentialLiteral.None;
				c = '[';
				continue;
			case '}':
				if (num > 0)
				{
					num--;
				}
				pendingKeyState = PendingKeyState.None;
				flag3 = false;
				potentialLiteral = PotentialLiteral.None;
				c = '}';
				continue;
			case ']':
				if (num2 > 0)
				{
					num2--;
				}
				pendingKeyState = PendingKeyState.None;
				flag3 = false;
				potentialLiteral = PotentialLiteral.None;
				c = ']';
				continue;
			case ',':
				pendingKeyState = PendingKeyState.None;
				flag3 = false;
				potentialLiteral = PotentialLiteral.None;
				c = ',';
				continue;
			}
			if (flag3 && !char.IsWhiteSpace(c2) && !flag)
			{
				switch (potentialLiteral)
				{
				case PotentialLiteral.None:
					switch (c2)
					{
					case 'n':
						potentialLiteral = PotentialLiteral.N;
						break;
					case 't':
						potentialLiteral = PotentialLiteral.T;
						break;
					case 'f':
						potentialLiteral = PotentialLiteral.F;
						break;
					}
					break;
				case PotentialLiteral.N:
					if (c2 == 'u')
					{
						potentialLiteral = PotentialLiteral.Nu;
					}
					break;
				case PotentialLiteral.Nu:
					if (c2 == 'l')
					{
						potentialLiteral = PotentialLiteral.Null;
					}
					break;
				case PotentialLiteral.Null:
					potentialLiteral = PotentialLiteral.None;
					break;
				case PotentialLiteral.T:
					if (c2 == 'r')
					{
						potentialLiteral = PotentialLiteral.Tr;
					}
					break;
				case PotentialLiteral.Tr:
					if (c2 == 'u')
					{
						potentialLiteral = PotentialLiteral.Tru;
					}
					break;
				case PotentialLiteral.Tru:
					if (c2 == 'e')
					{
						potentialLiteral = PotentialLiteral.True;
					}
					break;
				case PotentialLiteral.True:
					potentialLiteral = PotentialLiteral.None;
					break;
				case PotentialLiteral.F:
					if (c2 == 'a')
					{
						potentialLiteral = PotentialLiteral.Fa;
					}
					break;
				case PotentialLiteral.Fa:
					if (c2 == 'l')
					{
						potentialLiteral = PotentialLiteral.Fal;
					}
					break;
				case PotentialLiteral.Fal:
					if (c2 == 's')
					{
						potentialLiteral = PotentialLiteral.Fals;
					}
					break;
				case PotentialLiteral.Fals:
					if (c2 == 'e')
					{
						potentialLiteral = PotentialLiteral.False;
					}
					break;
				case PotentialLiteral.False:
					potentialLiteral = PotentialLiteral.None;
					break;
				}
				if (potentialLiteral == PotentialLiteral.None && char.IsWhiteSpace(c2))
				{
				}
			}
			else if (!flag3 || !char.IsWhiteSpace(c2))
			{
				potentialLiteral = PotentialLiteral.None;
			}
		}
		bool flag4 = flag;
		PendingKeyState pendingKeyState2 = pendingKeyState;
		bool flag5 = flag3;
		PotentialLiteral potentialLiteral2 = potentialLiteral;
		int num3 = num;
		int num4 = num2;
		if (flag4)
		{
			int num5 = 0;
			int num6 = stringBuilder.Length - 1;
			while (num6 >= 0 && stringBuilder[num6] == '\\')
			{
				num5++;
				num6--;
			}
			if (num5 % 2 == 1)
			{
				stringBuilder.Append("\\");
			}
			stringBuilder.Append("\"");
			pendingKeyState2 = ((pendingKeyState2 == PendingKeyState.Started && num3 > num4) ? PendingKeyState.Ended : ((!flag5) ? PendingKeyState.None : PendingKeyState.None));
		}
		if (pendingKeyState2 == PendingKeyState.Ended && num3 > num4 && !flag5)
		{
			stringBuilder.Append(": null");
		}
		else if (flag5)
		{
			string value = potentialLiteral2 switch
			{
				PotentialLiteral.N => "ull", 
				PotentialLiteral.Nu => "ll", 
				PotentialLiteral.Null => "", 
				PotentialLiteral.T => "rue", 
				PotentialLiteral.Tr => "ue", 
				PotentialLiteral.Tru => "e", 
				PotentialLiteral.True => "", 
				PotentialLiteral.F => "alse", 
				PotentialLiteral.Fa => "lse", 
				PotentialLiteral.Fal => "se", 
				PotentialLiteral.Fals => "e", 
				PotentialLiteral.False => "", 
				_ => "", 
			};
			if (!string.IsNullOrEmpty(value))
			{
				stringBuilder.Append(value);
			}
			else if (potentialLiteral2 == PotentialLiteral.None && !flag4)
			{
				string text = stringBuilder.ToString().TrimEnd();
				if (!text.EndsWith("\"", StringComparison.Ordinal) && !text.EndsWith("null", StringComparison.Ordinal) && !text.EndsWith("true", StringComparison.Ordinal) && !text.EndsWith("false", StringComparison.Ordinal) && !text.EndsWith("}", StringComparison.Ordinal) && !text.EndsWith("]", StringComparison.Ordinal))
				{
					stringBuilder.Append(" null");
				}
			}
		}
		num = 0;
		num2 = 0;
		bool flag6 = false;
		bool flag7 = false;
		for (int j = 0; j < stringBuilder.Length; j++)
		{
			char c3 = stringBuilder[j];
			if (flag7)
			{
				flag7 = false;
				continue;
			}
			switch (c3)
			{
			case '\\':
				if (flag6)
				{
					flag7 = true;
				}
				break;
			case '"':
				flag6 = !flag6;
				break;
			case '{':
				if (!flag6)
				{
					num++;
				}
				break;
			case '[':
				if (!flag6)
				{
					num2++;
				}
				break;
			case '}':
				if (!flag6 && num > 0)
				{
					num--;
				}
				break;
			case ']':
				if (!flag6 && num2 > 0)
				{
					num2--;
				}
				break;
			}
		}
		for (int k = 0; k < num2; k++)
		{
			stringBuilder.Append(']');
		}
		for (int l = 0; l < num; l++)
		{
			stringBuilder.Append('}');
		}
		return stringBuilder.ToString();
	}
}
