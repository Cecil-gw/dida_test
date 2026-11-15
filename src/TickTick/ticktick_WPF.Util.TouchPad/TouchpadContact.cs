using System;

namespace ticktick_WPF.Util.TouchPad;

public struct TouchpadContact : IEquatable<TouchpadContact>
{
	public int ContactId { get; }

	public int X { get; }

	public int Y { get; }

	public TouchpadContact(int contactId, int x, int y)
	{
		ContactId = contactId;
		X = x;
		Y = y;
	}

	public override bool Equals(object obj)
	{
		if (obj is TouchpadContact other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(TouchpadContact other)
	{
		if (ContactId == other.ContactId && X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public static bool operator ==(TouchpadContact a, TouchpadContact b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(TouchpadContact a, TouchpadContact b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return (ContactId, X, Y).GetHashCode();
	}

	public override string ToString()
	{
		return $"Contact ID:{ContactId} Point:{X},{Y}";
	}
}
