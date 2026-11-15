using System;
using System.Collections.Generic;

namespace ticktick_WPF.Util.DateParser;

internal class RecognizeDate
{
	private int recognizeStringStartPosition;

	private DateTime cal;

	private string reminder;

	private List<string> recogStrings = new List<string>();

	private TickTickDuration duration;

	public RecognizeDate()
	{
	}

	public RecognizeDate(string recogString, int recognizeStringStartPosition, DateTime cal)
	{
		recogStrings.Add(recogString);
		this.recognizeStringStartPosition = recognizeStringStartPosition;
		this.cal = cal;
	}

	public RecognizeDate(string recogString, int recognizeStringStartPosition, TickTickDuration duration)
	{
		recogStrings.Add(recogString);
		this.recognizeStringStartPosition = recognizeStringStartPosition;
		this.duration = duration;
	}

	public int getRecognizeStringStartPosition()
	{
		return recognizeStringStartPosition;
	}

	public DateTime getCal()
	{
		return cal;
	}

	public void SetCal(DateTime cal)
	{
		this.cal = cal;
	}

	public void addRecogString(string str)
	{
		recogStrings.Add(str);
	}

	public string getReminder()
	{
		return reminder;
	}

	public void setReminder(string reminder)
	{
		this.reminder = reminder;
	}

	public List<string> getRecogString()
	{
		return recogStrings;
	}

	public TickTickDuration getDuration()
	{
		return duration;
	}
}
