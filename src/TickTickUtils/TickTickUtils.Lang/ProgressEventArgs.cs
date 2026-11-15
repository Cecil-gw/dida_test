namespace TickTickUtils.Lang;

public class ProgressEventArgs
{
	public readonly int max;

	public readonly int delay;

	public readonly int period;

	public long Elapsed { get; set; }

	public int Progress { get; set; }

	public ProgressEventArgs(int max, int delay, int period)
	{
		this.max = max;
		this.delay = delay;
		this.period = period;
	}

	public bool IsCompleted()
	{
		return Progress >= 100;
	}

	public override string ToString()
	{
		return "ProgressEventArgs{progress: " + Progress + ", elapsed: " + Elapsed + "ms, max: " + max + ", delay: " + delay + "ms, period: " + period + "}";
	}
}
