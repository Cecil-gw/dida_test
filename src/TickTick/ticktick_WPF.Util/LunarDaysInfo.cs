namespace ticktick_WPF.Util;

public class LunarDaysInfo
{
	public bool IsLeapMonth30Days { get; set; }

	public bool[] MonthDays { get; set; } = new bool[12];

	public int LeapMonth { get; set; }

	public LunarDaysInfo()
	{
	}

	public LunarDaysInfo(int data)
		: this((uint)data)
	{
	}

	public LunarDaysInfo(uint data)
	{
		Init(data);
	}

	private void Init(uint data)
	{
		for (int i = 0; i < 12; i++)
		{
			uint num = (uint)(1 << 15 - i);
			MonthDays[i] = (data & num) != 0;
		}
		uint num2 = data & 0xF;
		LeapMonth = (int)((num2 != 0) ? num2 : 0);
		IsLeapMonth30Days = ((data >> 16) & 1) != 0;
	}

	public uint ToInt()
	{
		uint num = 0u;
		for (int i = 0; i < 12; i++)
		{
			if (MonthDays[i])
			{
				num |= (uint)(1 << 15 - i);
			}
		}
		if (LeapMonth > 0)
		{
			num |= (uint)(LeapMonth & 0xF);
		}
		if (IsLeapMonth30Days)
		{
			num |= 0x10000;
		}
		return num;
	}
}
