using System.Text;
using System.Timers;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Haven;

public unsafe class App
{
	public bool AppActive { get; private set; }
	public uint ResizeTimeoutMilliseconds { get; set; } = 500;

	public static App Instance => _instance;

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

	#region Private Members
	private bool AppPaused;
	private bool RequestPause;

	private Stopwatch FrameTimer;
	private System.Timers.Timer FPSIntervalTimer;

	private Widget _focused;
	
	private int CurrentFPS = 0;
	private long LastFPS = 0;
	private long LastFrameTime = 0;

	private Dimensions LastDimensions;

	private ConcurrentDictionary<string, Action<State>> UpdateTasks;
	private Dictionary<string, Layer> Layers;
	private Layer[] ActiveLayers;

	private static App _instance;

	private Stream Out;
	private Action<byte[], int> PlatformWriteStdout;
	private StringBuilder FrameBuilder;

	private bool RenderBufferDumpPending;
	private int RenderBufferDumpQuantity;
	#endregion

	private App(int LayerCount)
	{
		// Create objects
		Layers = new();
		ActiveLayers = new Layer[LayerCount];
		UpdateTasks = new();
		FrameTimer = new();
		FrameBuilder = new();
		FPSIntervalTimer = new();
		AppActive = false;
		AppPaused = false;
		RequestPause = false;
		RenderBufferDumpPending = false;
		RenderBufferDumpQuantity = 0;
		LastDimensions = Dimensions.Current;
		if (OperatingSystem.IsWindows())
		{
			IntPtr FileHandle = Kernel32.GetStdHandle(-11);

			PlatformWriteStdout = delegate (byte[] buf, int len)
			{
				Kernel32.WriteFile(FileHandle, buf, (uint) len, out _, nint.Zero);
			};
		}
		else
			PlatformWriteStdout = delegate (byte[] buf, int len)
			{
				fixed (byte* ptr = buf)
					Libc.write(1, ptr, (uint)len);
			};

		// Setup timer
		FPSIntervalTimer.Interval = 1000;
		FPSIntervalTimer.Elapsed += delegate (object sender, ElapsedEventArgs e)
		{
			LastFPS = CurrentFPS;
			CurrentFPS = 0;
		};

		// Set to UTF-8 encoding
		Console.OutputEncoding = Encoding.UTF8;

		// If we are running on Windows, enable VT processing manually using P/Invoke
		if (OperatingSystem.IsWindows())
		{
			IntPtr hOut = Kernel32.GetStdHandle(-11);

			Kernel32.GetConsoleMode(hOut, out uint mode);
			mode |= 4;
			Kernel32.SetConsoleMode(hOut, mode);
		}
	}

	/// <summary>
	/// Creates a Haven application with the specified renderer and layer count
	/// </summary>
	/// <param name="LayerCount">The amount of layers that can be drawn in one frame</param>
	/// <returns>A singleton instance of the Haven app</returns>
	public static App Create(int LayerCount = 3)
	{
		if (LayerCount < 1)
			throw new ArgumentOutOfRangeException(nameof(LayerCount));

		if (_instance is null)
			_instance = new(LayerCount);

		return _instance;
	}

	private bool DimensionsAreDifferent(Dimensions d, Dimensions d2)
	{
		bool WindowWidthChanged = d.WindowWidth != d2.WindowWidth;
		bool WindowHeightChanged = d.WindowHeight != d2.WindowHeight;
		bool BufferWidthChanged = d.BufferWidth != d2.BufferWidth;
		bool BufferHeightChanged = d.BufferHeight != d2.BufferHeight;

		return WindowWidthChanged || WindowHeightChanged || BufferWidthChanged || BufferHeightChanged;
	}

	private void ResizeRoutine()
	{
		Stopwatch ResizeTimer = new();

		Dimensions LastDimensions = Dimensions.Current;

		ResizeTimer.Restart();

		while (true)
		{
			Console.CursorVisible = false;

			var CurrentDimensions = Dimensions.Current;

			if (DimensionsAreDifferent(CurrentDimensions, LastDimensions))
				ResizeTimer.Restart();

			LastDimensions = CurrentDimensions;

			if (ResizeTimer.ElapsedMilliseconds >= 100)
			{
				ResizeTimer.Reset();
				return;
			}

			Thread.Sleep(10);
		}
	}

	private void UpdateDimensions(ref Dimensions d)
	{
		d.WindowWidth = Console.WindowWidth;
		d.WindowHeight = Console.WindowHeight;
		d.BufferWidth = Console.BufferWidth;
		d.BufferHeight = Console.BufferHeight;
	}

	private StringBuilder LastFrameBuilder = new();

	private void MainLoop()
	{
		State s = new();
		List<Widget> CurrentWidgets = new(30);

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

			// Check for updated console dimensions and update current state accordingly
			#region Update Dimensions

			// Update 
			UpdateDimensions(ref s.Dimensions);

			// Dimensions were changed if LastDimensions is different to current dimensions
			s.DimensionsChanged = DimensionsAreDifferent(s.Dimensions, LastDimensions);

			// Update last dimensions
			LastDimensions = s.Dimensions;

			if (s.Dimensions.WindowHeight == 0)
				continue;

			if (s.DimensionsChanged)
			{
				Console.Clear();
				Console.CursorVisible = false;

				// Blocks until dimensions are stable for at least a second
				ResizeRoutine();
			}

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
					FocusedWidget._OnConsoleKey(cki);
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
				foreach (var Task in UpdateTasks.Values)
					Task(s);

			// Clear the list of widgets to render in this frame
			CurrentWidgets.Clear();

			foreach (var Layer in ActiveLayers)
			{
				if (Layer is null)
					continue;

				// Update the current layout
				Layer.UpdateLayout(s.Dimensions);

				// Add only the currently visible widgets to the render list
				CurrentWidgets.AddRange(Layer.Widgets.Where(w => w.Visible));
			}

			// Clear frame builder
			FrameBuilder.Clear();

			// Set cursor to top left
			FrameBuilder.Append("\x1b[;H");

			// Clear screen
			FrameBuilder.Append("\x1b[J");

			// Set foreground and background
			FrameBuilder.Append("\x1b[97m\x1b[40m");
			VTRenderContext.CurrentForegroundColor = 15;
			VTRenderContext.CurrentBackgroundColor = 0;

			// Draw each widget and append its render context buffer to the final frame
			foreach (var w in CurrentWidgets)
			{
				w.Draw();
				FrameBuilder.Append(w.RenderContext.GetBuffer());
			}

			// Local variables for the final frame data
			string FinalFrame = FrameBuilder.ToString();
			byte[] FinalFrameBytes = Encoding.UTF8.GetBytes(FinalFrame);

			// Handle Render Buffer Dumps
			if (RenderBufferDumpPending)
			{
				if (RenderBufferDumpQuantity <= 0)
				{
					RenderBufferDumpPending = false;
				}
				else
				{
					using (var sw = File.AppendText(@".\HavenRenderBufferDump.txt"))
					{
						sw.Write("--- Begin frame ");
						sw.Write(RenderBufferDumpQuantity);
						sw.Write(" (");
						sw.Write(FinalFrameBytes.Length);
						sw.WriteLine(" bytes)");


						sw.WriteLine(FinalFrame.ToString());

						sw.Write("--- End frame ");
						sw.WriteLine(RenderBufferDumpQuantity);
					}

					RenderBufferDumpQuantity--;
				}
			}

			string ThisFrame = FrameBuilder.ToString();
			string LastFrame = LastFrameBuilder.ToString();

			// If this frame is different than the last frame
			if (ThisFrame != LastFrame)
			{
				// Write final frame using platform-specific implementation
				PlatformWriteStdout(FinalFrameBytes, FinalFrameBytes.Length);
			}

			// Clear each widget's render context buffer after rendering
			CurrentWidgets.ForEach(w => w.RenderContext.Clear());

			// Update LastFrameTime
			LastFrameTime = (int) FrameTimer.ElapsedMilliseconds;

			// Store this frame so that it can be checked for equality on the next frame
			LastFrameBuilder.Clear();
			LastFrameBuilder.Append(FrameBuilder.ToString());

			// Increment FPS counter
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

	#region Debug

	public void DumpRenderBuffers(int Quantity)
	{
		RenderBufferDumpPending = true;
		RenderBufferDumpQuantity = Quantity;
	}

	#endregion

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

	public bool HasUpdateTask(string TaskID) => UpdateTasks.ContainsKey(TaskID);

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

	public T GetLayer<T>() where T : Layer
	{
		Layer l = Layers.Values.FirstOrDefault(t => t.GetType() == typeof(T) );

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
	public void SetLayer(int ZIndex, string ID = "", params object[] Args)
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

		Layers[ID]._OnShow(this, Args);
	}

	public bool IsLayerVisible(string ID)
	{
		if (!Layers.ContainsKey(ID))
			return false;

		return ActiveLayers.Contains(Layers[ID]);
	}

	#endregion
}