using System;
using System.Windows;

namespace ticktick_WPF.Notifier;

public class TasksChangeEventManager : WeakEventManager
{
	private static TasksChangeEventManager CurrentManager
	{
		get
		{
			Type typeFromHandle = typeof(TasksChangeEventManager);
			TasksChangeEventManager tasksChangeEventManager = (TasksChangeEventManager)WeakEventManager.GetCurrentManager(typeFromHandle);
			if (tasksChangeEventManager == null)
			{
				tasksChangeEventManager = new TasksChangeEventManager();
				WeakEventManager.SetCurrentManager(typeFromHandle, tasksChangeEventManager);
			}
			return tasksChangeEventManager;
		}
	}

	private TasksChangeEventManager()
	{
	}

	public static void AddHandler(TaskChangeNotifier source, EventHandler<TasksChangeEventArgs> handler)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		CurrentManager.ProtectedAddHandler(source, handler);
	}

	public static void RemoveHandler(TaskChangeNotifier source, EventHandler<TasksChangeEventArgs> handler)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		CurrentManager.ProtectedRemoveHandler(source, handler);
	}

	protected override ListenerList NewListenerList()
	{
		return new ListenerList<TasksChangeEventArgs>();
	}

	protected override void StartListening(object source)
	{
		((TaskChangeNotifier)source).TasksChanged += OnSomeEvent;
	}

	protected override void StopListening(object source)
	{
		((TaskChangeNotifier)source).TasksChanged -= OnSomeEvent;
	}

	private void OnSomeEvent(object sender, TasksChangeEventArgs e)
	{
		DeliverEvent(sender, e);
	}
}
