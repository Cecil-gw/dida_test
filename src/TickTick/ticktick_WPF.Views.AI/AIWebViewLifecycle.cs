using System;

namespace ticktick_WPF.Views.AI;

internal sealed class AIWebViewLifecycle
{
	public AIWebViewLifecycleState State { get; private set; }

	public bool CanRetry => State == AIWebViewLifecycleState.Failed;

	public event EventHandler Changed;

	public bool TryBeginInitialization()
	{
		if (State != AIWebViewLifecycleState.NotInitialized && State != AIWebViewLifecycleState.Failed)
		{
			return false;
		}
		return SetState(AIWebViewLifecycleState.Initializing);
	}

	public bool TryMarkReady()
	{
		if (State != AIWebViewLifecycleState.Initializing)
		{
			return false;
		}
		return SetState(AIWebViewLifecycleState.Ready);
	}

	public bool TryMarkFailed()
	{
		if (State != AIWebViewLifecycleState.Initializing)
		{
			return false;
		}
		return SetState(AIWebViewLifecycleState.Failed);
	}

	public bool TryMarkRuntimeFailed()
	{
		if (State == AIWebViewLifecycleState.Ready)
		{
			return SetState(AIWebViewLifecycleState.Failed);
		}
		return false;
	}

	public bool Dispose()
	{
		return SetState(AIWebViewLifecycleState.Disposed);
	}

	private bool SetState(AIWebViewLifecycleState state)
	{
		if (State == AIWebViewLifecycleState.Disposed || State == state)
		{
			return false;
		}
		State = state;
		this.Changed?.Invoke(this, EventArgs.Empty);
		return true;
	}
}
