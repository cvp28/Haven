using System.Diagnostics;

namespace Haven;

public static class Engine
{
	private static Thread RenderThread;

	public static bool AppActive { get; private set; }
	public static bool RenderActive { get; private set; }

	public static IRenderer Screen;
	private static Stopwatch RenderTimer;
	private static long FPS;
	private static long FrameTime;

	private static Dimensions LastDimensions;

	private static Widget _focused;
	public static Widget FocusedWidget
	{
		get => _focused;
		set
		{
			if (_focused == value)
				return;

			if (_focused != null)
				_focused.Focused = false;

			_focused = value;

			if (_focused != null)
				_focused.Focused = true;
		}
	}
	private static List<Widget> Widgets;

	private static Dictionary<string, Action<State>> UpdateTasks;

	public static void Initialize<T>() where T : IRenderer
	{
		RenderThread = new(RenderLoop);
		RenderThread.IsBackground = false;
		RenderThread.Name = "Haven Render Thread";
		RenderThread.Priority = ThreadPriority.Highest;

		RenderTimer = new();

		LastDimensions = Dimensions.Current;

		Screen = Activator.CreateInstance(typeof(T)) as IRenderer;

		Widgets = new();
		UpdateTasks = new();

		AppActive = false;
		RenderActive = false;
	}

	public static void ApplicationLoop()
	{
		State s = new();

		while (AppActive)
		{
			s.FPS = FPS;
			s.LastFrameTime = FrameTime;

			// Check for updated console dimensions and update current state accordingly
			#region Update Dimensions
			Dimensions CurrentDimensions = Dimensions.Current;

			bool WindowWidthChanged = CurrentDimensions.WindowWidth != LastDimensions.WindowWidth;
			bool WindowHeightChanged = CurrentDimensions.WindowHeight != LastDimensions.WindowHeight;
			bool BufferWidthChanged = CurrentDimensions.BufferWidth != LastDimensions.BufferWidth;
			bool BufferHeightChanged = CurrentDimensions.BufferHeight != LastDimensions.BufferHeight;

			// Dimensions were changed if any of those conditions were true
			s.DimensionsChanged = WindowWidthChanged || WindowHeightChanged || BufferWidthChanged || BufferHeightChanged;

			// Set current dimensions
			s.Dimensions = CurrentDimensions;

			// Update last dimensions
			LastDimensions = CurrentDimensions;
			#endregion

			// Check for user input and update current state accordingly
			#region Update User Input
			if (Console.KeyAvailable)
			{
				var cki = Console.ReadKey(true);
				s.KeyPressed = true;
				s.KeyInfo = cki;

				if (FocusedWidget is not null)
				{
					FocusedWidget.OnConsoleKey(cki);
					s.InputAlreadyHandled = true;
				}
				else
				{
					s.InputAlreadyHandled = false;
				}
			}
			else
			{
				s.KeyPressed = false;
				s.KeyInfo = default;
			}
			#endregion

			if (UpdateTasks.Count > 0)
				foreach (var Task in UpdateTasks)
					Task.Value(s);
		}
	}

	private static void RenderLoop()
	{
		while (RenderActive)
		{
			RenderTimer.Restart();
			Screen.Render(Widgets);
			RenderTimer.Stop();

			// Calculate FPS
			FPS = Stopwatch.Frequency / RenderTimer.ElapsedTicks;
			FrameTime = RenderTimer.ElapsedMilliseconds;
		}
	}

	public static void Run()
	{
		Console.CursorVisible = false;

		AppActive = true;
		RenderActive = true;

		RenderThread.Start();		// Run render thread concurrently
		ApplicationLoop();			// Run application logic in main thread and await its return first
		RenderThread.Join();        // Then, await render thread return

		Console.ResetColor();
		Console.Clear();
	}

	public static void SignalExit()
	{
		AppActive = false;
		RenderActive = false;
	}

	public static bool AddUpdateTask(string TaskID, Action<State> Action)
	{
		if (UpdateTasks.ContainsKey(TaskID)) { return false; }

		UpdateTasks.Add(TaskID, Action);
		return true;
	}

	public static bool RemoveUpdateTask(string TaskID) => UpdateTasks.Remove(TaskID);

	public static void AddWidget(Widget Widget)
	{
		Widgets.Add(Widget);
	}

	public static void AddWidgets(params Widget[] Widgets)
	{
		foreach (Widget Widget in Widgets)
			AddWidget(Widget);
	}

	public static void AddWidgets(WidgetGroup Group)
	{
		foreach (Widget Widget in Group.Widgets)
			AddWidget(Widget);
	}

	public static bool RemoveWidget(Widget Widget)
	{
		return Widgets.Remove(Widget);
	}
}