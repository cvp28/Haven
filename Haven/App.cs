using System.Diagnostics;
using System.Collections.Concurrent;
using System.Timers;
using System.Text;
using System.Runtime.InteropServices;

namespace Haven;

public class App
{
	public bool AppActive { get; private set; }
	private bool AppPaused;
	private bool RequestPause;

	public Renderer Screen { get; private set; }
	private Stopwatch FrameTimer;
	private System.Timers.Timer FPSIntervalTimer;
	
	private int CurrentFPS = 0;
	private long LastFPS = 0;
	private long LastFrameTime = 0;

	private Dimensions LastDimensions;

	private Widget _focused;
	public Widget FocusedWidget
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

	private Dictionary<string, Layer> Layers;
	private Layer[] ActiveLayers;

	public int TopLayer => ActiveLayers.Length - 1;

	private ConcurrentDictionary<string, Action<State>> UpdateTasks;

	private static App _instance;

	public static App Instance => _instance;

	private App(Renderer Renderer, int LayerCount)
	{
		FrameTimer = new();
		FPSIntervalTimer = new();

		FPSIntervalTimer.Interval = 1000;
		FPSIntervalTimer.Elapsed += delegate (object sender, ElapsedEventArgs e)
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
		};

		LastDimensions = Dimensions.Current;

		Screen = Renderer;

		Layers = new();
		ActiveLayers = new Layer[LayerCount];
		UpdateTasks = new();

		AppActive = false;
		AppPaused = false;
		RequestPause = false;
	}

	/// <summary>
	/// Creates a Haven application with the specified renderer and layer count
	/// </summary>
	/// <typeparam name="T">The renderer type to use for drawing the screen [WindowsNativeRenderer, ConWriteRenderer, etc.]</typeparam>
	/// <param name="LayerCount">The amount of layers that can be drawn in one frame</param>
	/// <returns>A singleton instance of a Haven app</returns>
	public static App Create<T>(int LayerCount = 3) where T : Renderer
	{
		if (LayerCount < 1)
			throw new ArgumentOutOfRangeException(nameof(LayerCount));

		if (_instance is null)
			_instance = new(Activator.CreateInstance<T>(), LayerCount);

		return _instance;
	}

	private void MainLoop()
	{
		State s = new();
		List<Widget> CurrentWidgets = new(30);
		Dimensions d = Dimensions.Current;

		int LastMaxHeight = Console.LargestWindowHeight;
		int LastMaxWidth = Console.LargestWindowWidth;

		FPSIntervalTimer.Start();

		while (AppActive)
		{
			// Hang the main loop while the AppPaused flag is set
			if (RequestPause)
			{
				AppPaused = true;

				while (AppPaused)
					Thread.Sleep(25);
			}

			FrameTimer.Restart();

			s.FPS = LastFPS;
			s.LastFrameTime = LastFrameTime;

			s.WidgetRenderTimeMs = Screen.WidgetRenderTimeMs;
			s.StdoutWriteTimeMs = Screen.StdoutWriteTimeMs;
			s.DiagTime1Ms = Screen.DiagTime1Ms;
			s.DiagTime2Ms = Screen.DiagTime2Ms;

			// Check for updated console dimensions and update current state accordingly
			#region Update Dimensions

			d.WindowWidth = Console.WindowWidth;
			d.WindowHeight = Console.WindowHeight;
			d.BufferWidth = Console.BufferWidth;
			d.BufferHeight = Console.BufferHeight;

			bool WindowWidthChanged = d.WindowWidth != LastDimensions.WindowWidth;
			bool WindowHeightChanged = d.WindowHeight != LastDimensions.WindowHeight;
			bool BufferWidthChanged = d.BufferWidth != LastDimensions.BufferWidth;
			bool BufferHeightChanged = d.BufferHeight != LastDimensions.BufferHeight;

			// Dimensions were changed if any of those conditions were true
			s.DimensionsChanged = WindowWidthChanged || WindowHeightChanged || BufferWidthChanged || BufferHeightChanged;

			// Set current dimensions
			s.Dimensions = d;

			// Update last dimensions
			LastDimensions = d;

			if (s.DimensionsChanged)
				Screen.UpdateScreenDimensions();

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

			// Run update tasks
			if (UpdateTasks.Count > 0)
				foreach (var Task in UpdateTasks)
					Task.Value(s);

			// Clear the list of widgets to render in this frame
			CurrentWidgets.Clear();

			// Update each layer's layout and add its widgets to the render list
			foreach (var Layer in ActiveLayers)
			{
				if (Layer is null)
					continue;

				Layer.UpdateLayout(s.Dimensions);
				CurrentWidgets.AddRange(Layer.Widgets);
			}

			// Render all of the visible widgets in this frame
			Screen.Render(CurrentWidgets);

			LastFrameTime = (int) FrameTimer.ElapsedMilliseconds;

			CurrentFPS++;
		}

		FPSIntervalTimer.Stop();
		FPSIntervalTimer.Dispose();
	}

	public void Run()
	{
		Console.CursorVisible = false;

		AppActive = true;
		MainLoop();

		Console.ResetColor();
		Console.Clear();
	}

	#region Modal Stuff

	public void DoModal(ConsoleKey QuitKey, params string[] Messages)
	{
		Task.Run(delegate ()
		{
			PauseMainloop();

			Console.CursorVisible = false;
			Console.OutputEncoding = Encoding.UTF8;
			ConsoleColor InitialForeground = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.White;

			// Modal window variables
			string BottomText = string.Create(null, stackalloc char[64], $"<press {QuitKey.ToString().ToUpper()} to continue>");
			int MaxMessageLength = Messages.Max(msg => msg.Length);

			int WindowWidth = MaxMessageLength > BottomText.Length ? MaxMessageLength + 4 : BottomText.Length + 4;
			int WindowHeight = 5 + Messages.Length;

			int X = (Console.WindowWidth / 2) - (WindowWidth / 2);
			int Y = (Console.WindowHeight / 2) - (WindowHeight / 2) - 1;

			// Draw window
			DrawWindow(X, Y, WindowWidth, WindowHeight);

			// Draw window contents
			for (int i = 0; i < Messages.Length; i++)
			{
				Console.SetCursorPosition(X + (WindowWidth / 2) - (Messages[i].Length / 2), Y + 2 + i);
				Console.Write(Messages[i]);
			}

			// Draw prompt
			Console.SetCursorPosition(X + (WindowWidth / 2) - (BottomText.Length / 2), Y + WindowHeight - 2);
			Console.Write(BottomText);

			Console.ForegroundColor = InitialForeground;
			AwaitKeypress(QuitKey);
			ResumeMainloop();
		});
	}

	private void DrawWindow(int X, int Y, int Width, int Height)
	{
		int LeftBound = (Console.WindowWidth / 2) - (Width / 2);
		int TopBound = (Console.WindowHeight / 2) - (Height / 2) - 1;

		Span<char> WindowTop = stackalloc char[Width];
		Span<char> WindowBottom = stackalloc char[Width];
		Span<char> WindowMiddle = stackalloc char[Width];

		WindowTop[0] = '╭';
		WindowTop[Width - 1] = '╮';

		WindowBottom[0] = '╰';
		WindowBottom[Width - 1] = '╯';

		WindowMiddle[0] = '│';
		WindowMiddle[Width - 1] = '│';

		for (int i = 1; i <= Width - 2; i++)
		{
			WindowTop[i] = '─';
			WindowBottom[i] = '─';
			WindowMiddle[i] = ' ';
		}

		// Start drawing
		Console.SetCursorPosition(X, Y);
		Console.Write(WindowTop.ToString());

		for (int i = 1; i <= Height - 1; i++)
		{
			Console.SetCursorPosition(X, Y + i);
			Console.Write(WindowMiddle.ToString());
		}

		Console.SetCursorPosition(X, Y + Height);
		Console.Write(WindowBottom.ToString());
	}

	private void AwaitKeypress(ConsoleKey Key)
	{
		while (true)
		{
			if (!Console.KeyAvailable)
				Thread.Sleep(25);

			if (Console.ReadKey(true).Key == Key)
				return;
		}
	}

	#endregion

	#region Basic Controls

	private void PauseMainloop()
	{
		RequestPause = true;

		while (!AppPaused)
			Thread.Sleep(25);

		FPSIntervalTimer.Enabled = false;
		FrameTimer.Stop();
	}

	private void ResumeMainloop()
	{
		RequestPause = false;
		AppPaused = false;

		FPSIntervalTimer.Enabled = true;
		FrameTimer.Start();
	}

	/// <summary>
	/// Signals the MainLoop to exit after the current iteration. This is the primary way of closing a Haven Application.
	/// </summary>
	public void SignalExit() => AppActive = false;

	#endregion

	#region UpdateTask Controls

	public bool AddUpdateTask(string TaskID, Action<State> Action)
	{
		return UpdateTasks.TryAdd(TaskID, Action);
	}

	public bool RemoveUpdateTask(string TaskID)
	{
		if (!UpdateTasks.ContainsKey(TaskID))
			return false;

		var kvp = UpdateTasks.First(kvp => kvp.Key == TaskID);
		return UpdateTasks.TryRemove(kvp);
	}

	#endregion

	#region Layer Controls

	public void AddLayer(string ID, Layer p)
	{
		if (ID == "")
			throw new Exception("ID cannot be empty");

		Layers.Add(ID, p);
	}

	/// <summary>
	/// Retrieves a layer object from the internal dictionary of layers. Can return null.
	/// </summary>
	/// <param name="ID">ID of the layer object to retrieve</param>
	/// <returns>The layer with the specified ID or null if the ID does not correspond to any layer</returns>
	public Layer GetLayer(string ID) => Layers.FirstOrDefault(kvp => kvp.Key == ID).Value;

	public T GetLayer<T>(string ID) where T : Layer
	{
		Layer l = Layers.FirstOrDefault(kvp => kvp.Key == ID).Value;

		if (l is null)
			return null;

		try
		{
			T temp = l as T;
			return temp;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public Layer GetLayer(int ZIndex)
	{
		bool ZIndexInvalid = ZIndex < 0 || ZIndex >= ActiveLayers.Length;

		if (ZIndexInvalid)
			return null;

		return ActiveLayers[ZIndex];
	}

	/// <summary>
	/// Sets a layer with the specified index to a layer with the specified ID. ID can be left empty to just hide the layer at the specified index.
	/// </summary>
	/// <param name="ID">The ID of the layer that was added with AddLayer(string, Layer). If left as default, it will hide the layer and not show anything in its place.</param>
	/// <param name="ZIndex">The Z Index to draw the layer in. Ranges from 0 to LayerCount - 1 inclusive. Layers with higher Z values are displayed over top of those with smaller ones.</param>
	public void SetLayer(int ZIndex, string ID = "")
	{
		bool EmptyID = ID == "";

		if (!Layers.ContainsKey(ID) && !EmptyID)
			return;

		bool ZIndexInvalid = ZIndex < 0 || ZIndex >= ActiveLayers.Length;

		if (ZIndexInvalid)
			return;

		if (ActiveLayers[ZIndex] is not null)
		{
			ActiveLayers[ZIndex]._OnHide(this);
			ActiveLayers[ZIndex] = null;
		}

		if (EmptyID)
			return;

		ActiveLayers[ZIndex] = Layers[ID];
		ActiveLayers[ZIndex].ZIndex = ZIndex;

		Layers[ID]._OnShow(this);
	}

	public bool IsLayerVisible(string ID)
	{
		if (!Layers.ContainsKey(ID))
			return false;

		return ActiveLayers.Contains(Layers[ID]);
	}

	#endregion
}