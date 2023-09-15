using System.Text;
using System.Timers;
using System.Diagnostics;
using System.Collections.Concurrent;

using System.IO.Pipes;

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Haven.Diagnostics")]

namespace HavenUI;

public static unsafe partial class Haven
{
	public static bool AppActive { get; private set; }
	public static uint ResizeTimeoutMilliseconds { get; set; } = 500;
	
	public static string ActiveInputHandle { get; set; }
	
	/// <summary>
	/// Sets the frame rate limiter to the specified amount.
	/// Valid range is 1 - 1000 inclusive.
	/// 0 means the limiter is disabled (default).
	/// </summary>
	public static int FrameRateLimit { get; set; } = 0;
	
	public static int FrameRate { get; private set; }
	public static int LastFrameTime { get; private set; }
	public static int CurrentFrameByteLength { get; private set; }
	public static int WindowWidth => CurrentDimensions.WindowWidth;
	public static int WindowHeight => CurrentDimensions.WindowHeight;
	
	public static Dictionary<ConsoleKey, Action> GlobalKeyActions { get; private set; } = new();
	
	#region Private/Internal Members
	private static bool Initialized = false;
	
	//private static NamedPipeServerStream DiagnosticsPipe;
	
	private static bool AppPaused;
	private static bool RequestPause;
	
	private static System.Timers.Timer FPSIntervalTimer;
	
	//private static Thread MainLoopWorker;
	
	internal static int CurrentMainLoopIterations = 0;
	internal static int CurrentWorkerIterations = 0;
	private static long LastFPS = 0;
	
	public static Dimensions CurrentDimensions = Dimensions.Current;
	private static Dimensions LastDimensions = Dimensions.Current;
	private static bool DimensionsHaveChanged;
	
	private static Dictionary<string, Queue<ConsoleKeyInfo>> OpenInputHandles;
	private static ConcurrentQueue<ConsoleKeyInfo> InputBuffer;
	
	internal static ConcurrentDictionary<string, Action<State>> UpdateTasks;
	internal static Dictionary<string, Layer> Layers;
	internal static Layer[] ActiveLayers;
	
	private static Action<byte[]> PlatformWriteStdout;
	private static StringBuilder FrameBuilder;
	
	private static VTRenderContext RenderContext;
	
	private static bool RenderBufferDumpPending;
	private static int RenderBufferDumpQuantity;
	
	// Cross-platform high-res scheduler timing delegates
	private static Action PlatformEnableHighResolutionTiming;
	private static Action PlatformDisableHighResolutionTiming;
	
	// Linux-specific thread scheduling stuff
	private static int InitialLinuxThreadPolicy;
	private static Libc.sched_param SchedulerParams;
	
	#endregion

	public static void Initialize(int LayerCount = 3)
	{
		
		if (Initialized)
			throw new Exception("Haven is already initialized");
		
		// Create objects
		FrameRate = 0;
		Layers = new();
		ActiveLayers = new Layer[LayerCount];
		UpdateTasks = new();
		FrameBuilder = new();
		FPSIntervalTimer = new();
		AppActive = false;
		AppPaused = false;
		RequestPause = false;
		RenderContext = new();
		RenderBufferDumpPending = false;
		RenderBufferDumpQuantity = 0;
		LastDimensions = Dimensions.Current;
		InputBuffer = new();
		OpenInputHandles = new();
		if (OperatingSystem.IsWindows())
		{
			IntPtr FileHandle = Kernel32.GetStdHandle(-11);

			PlatformWriteStdout = delegate (byte[] buf)
			{
				Kernel32.WriteFile(FileHandle, buf, (uint) buf.Length, out _, nint.Zero);
			};
			
			PlatformEnableHighResolutionTiming = delegate { WinMM.TimeBeginPeriod(1); };
			PlatformDisableHighResolutionTiming = delegate { WinMM.TimeEndPeriod(1); };
		}
		else
		{	
			PlatformWriteStdout = delegate (byte[] buf)
			{
				fixed (byte* ptr = buf)
					Libc.Write(1, ptr, (uint)buf.Length);
			};
			
			PlatformEnableHighResolutionTiming = delegate
			{
				InitialLinuxThreadPolicy = Libc.GetScheduler(0);
				SchedulerParams.sched_priority = Libc.SchedulerGetPriorityMin(Libc.SCHED_RR);
				Libc.SetScheduler(0, Libc.SCHED_RR, ref SchedulerParams);
			};
			
			PlatformDisableHighResolutionTiming = delegate
			{
				SchedulerParams.sched_priority = Libc.SchedulerGetPriorityMin(InitialLinuxThreadPolicy);
				Libc.SetScheduler(0, InitialLinuxThreadPolicy, ref SchedulerParams);
			};
		}
		
		//	MainLoopWorker = new(MainLoopWorkerRoutine)
		//	{
		//		IsBackground = true
		//	};
		
		//DiagnosticsPipe = new("HavenDiagnosticsServer", PipeDirection.InOut);
		
		// Setup timer
		FPSIntervalTimer.Interval = 1000;
		FPSIntervalTimer.Elapsed += delegate (object sender, ElapsedEventArgs e)
		{
			//	if (CurrentFPS > FrameRateLimit)
			//	{
			//		CurrentFrameRateLimiterSleepTimeMs++;
			//		Task.Run(() => Console.Beep());
			//	}
			//	else if (CurrentFPS < FrameRateLimit)
			//	{
			//		CurrentFrameRateLimiterSleepTimeMs--;
			//		Task.Run(() => Console.Beep());
			//	}
			
			FrameRate = CurrentMainLoopIterations;
			CurrentMainLoopIterations = 0;
			CurrentWorkerIterations = 0;
		};

		// Set to UTF-8 encoding
		Console.OutputEncoding = Encoding.UTF8;
		Console.TreatControlCAsInput = true;

		// If we are running on Windows, enable VT processing manually using P/Invoke
		if (OperatingSystem.IsWindows())
		{
			IntPtr hOut = Kernel32.GetStdHandle(-11);

			Kernel32.GetConsoleMode(hOut, out uint mode);
			mode |= 4;
			Kernel32.SetConsoleMode(hOut, mode);
		}
		
		Initialized = true;
	}
	
	public static (int X, int Y) GetCoordsFromOffset(int Offset, int X = 0, int Y = 0)
	{
		bool XOutOfBounds = X < 0 || X >= WindowWidth;
		bool YOutOfBounds = Y < 0 || Y >= WindowHeight;
		
		if (XOutOfBounds || YOutOfBounds)
			return (0, 0);
		
		int TempX = X;
		int TempY = Y;
		
		for (int i = 0; i < Offset; i++)
			if (TempX == WindowWidth - 1)
			{
				TempX = 0;
				TempY++;
			}
			else
			{
				TempX++;
			}
		
		return (TempX, TempY);
	}
	
	public static bool HasInputHandle(string Handle) => OpenInputHandles.ContainsKey(Handle);
	
	public static void OpenInputHandle(string Handle, bool Activate = false)
	{
		if (OpenInputHandles.ContainsKey(Handle))
			return;
		
		OpenInputHandles.Add(Handle, new());
		
		if (Activate)
			ActiveInputHandle = Handle;
	}

	public static void CloseInputHandle(string Handle)
	{
		if (!OpenInputHandles.ContainsKey(Handle))
			return;

		OpenInputHandles.Remove(Handle);
		
		if (ActiveInputHandle == Handle)
			ActiveInputHandle = string.Empty;
	}

	public static Maybe<ConsoleKeyInfo> GetKey(string Handle)
	{
		if (!OpenInputHandles.ContainsKey(Handle))
			return Maybe<ConsoleKeyInfo>.Fail();

		if (OpenInputHandles[Handle].Count == 0)
			return Maybe<ConsoleKeyInfo>.Fail();

		return Maybe<ConsoleKeyInfo>.Success(OpenInputHandles[Handle].Dequeue());
	}
	
	public static bool KeyAvailable(string Handle)
	{
		if (!OpenInputHandles.ContainsKey(Handle))
			return false;
		
		return OpenInputHandles[Handle].Count > 0;
	}

	private static bool DimensionsAreDifferent(Dimensions d, Dimensions d2)
	{
		bool WindowWidthChanged = d.WindowWidth != d2.WindowWidth;
		bool WindowHeightChanged = d.WindowHeight != d2.WindowHeight;
		bool BufferWidthChanged = d.BufferWidth != d2.BufferWidth;
		bool BufferHeightChanged = d.BufferHeight != d2.BufferHeight;

		return WindowWidthChanged || WindowHeightChanged || BufferWidthChanged || BufferHeightChanged;
	}

	private static void ResizeRoutine()
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

	private static void UpdateDimensions(ref Dimensions d)
	{
		d.WindowWidth = Console.WindowWidth;
		d.WindowHeight = Console.WindowHeight;
		d.BufferWidth = Console.BufferWidth;
		d.BufferHeight = Console.BufferHeight;
	}
	
	public static SleepState MainSleepState = new();
	private static SleepState WorkerSleepState = new();
	
	private static double MainLoopElapsedSeconds;
	private static double WorkerElapsedSeconds;
	
	private static void MainLoopWorkerRoutine()
	{
		double time_start = CurrentTimeSeconds;
		
		while (Console.KeyAvailable)
		{
			var cki = Console.ReadKey(true);
			
			// Enqueue user input in both the mainloop's input buffer as well as on the active input handle (for widgets)
			InputBuffer.Enqueue(cki);
			
			if (ActiveInputHandle is not null && OpenInputHandles.ContainsKey(ActiveInputHandle))
				OpenInputHandles[ActiveInputHandle].Enqueue(cki);
		}
		
		UpdateDimensions(ref CurrentDimensions);
		DimensionsHaveChanged = DimensionsAreDifferent(CurrentDimensions, LastDimensions);
		LastDimensions = CurrentDimensions;
		
		WorkerElapsedSeconds = CurrentTimeSeconds - time_start;
	}
	
	private static State s = new();
	
	private static void MainLoop()
	{
		if (FrameRateLimit > 0 && CurrentMainLoopIterations >= FrameRateLimit)
			return;
		
		// Hang the main loop while the AppPaused flag is set
		if (RequestPause)
		{
			AppPaused = true;

			while (AppPaused)
				Thread.Sleep(25);
		}
		
		double time_start = CurrentTimeSeconds;

		s.FPS = LastFPS;
		s.LastFrameTime = LastFrameTime;
		
		// Check for updated console dimensions and update current state accordingly
		#region Update Dimensions
		s.Dimensions = CurrentDimensions;
		s.DimensionsChanged = DimensionsHaveChanged;
		
		if (s.Dimensions.WindowHeight == 0)
			return;
		
		if (DimensionsHaveChanged)
		{
			Console.Clear();
			Console.CursorVisible = false;
		
			// Blocks until dimensions are stable for at least a second
			ResizeRoutine();
		}
		#endregion
		
		#region Get User Input
		if (InputBuffer.Any())
		{
			InputBuffer.TryDequeue(out var cki);
			
			if (GlobalKeyActions.ContainsKey(cki.Key))
				GlobalKeyActions[cki.Key]();
			
			foreach (var Layer in ActiveLayers)
				if (Layer is not null && Layer.KeyActions.ContainsKey(cki.Key))
					Layer.KeyActions[cki.Key](cki);
		}
		#endregion
		
		// Run update tasks
		foreach (var Task in UpdateTasks.Values)
			Task(s);
		
		#region Rendering
		// Reset cursor position and clear display
		RenderContext.FrameBuffer.Append("\x1b[;H");
		RenderContext.FrameBuffer.Append("\x1b[J");
		
		// Reset foreground and background colors
		RenderContext.FrameBuffer.Append("\x1b[97m\x1b[40m");
		VTRenderContext.CurrentForegroundColor = 15;
		VTRenderContext.CurrentBackgroundColor = 0;

		foreach (var Layer in ActiveLayers)
		{	
			if (Layer is null)
				continue;
			
			// Update the current layout
			Layer.UpdateLayout(s.Dimensions);
			
			// Signal current layer to render its UI
			Layer.OnUIRender();
		}

		// Local variables for the final frame data
		string FinalFrame = RenderContext.GetCurrentFrameBuffer();
		byte[] FinalFrameBytes = Encoding.UTF8.GetBytes(FinalFrame);
		
		CurrentFrameByteLength = FinalFrameBytes.Length;
		
		// Handle Render Buffer Dumps
		if (RenderBufferDumpPending)
		{
			using (var sw = File.AppendText(@".\HavenRenderBufferDump.txt"))
			{
				sw.AutoFlush = false;
				
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
			RenderBufferDumpPending = RenderBufferDumpQuantity <= 0;
		}
		
		// Only write if this frame is different than the last frame
		if (RenderContext.ShouldRender)
			PlatformWriteStdout(FinalFrameBytes);
		
		// Store this frame so that it can be checked for equality on the next frame
		RenderContext.StoreCurrentFrame();
		RenderContext.ClearCurrentFrame();
		#endregion
		
		// Increment FPS counter
		CurrentMainLoopIterations++;
		
		MainLoopElapsedSeconds = CurrentTimeSeconds - time_start;
		
		// Update LastFrameTime
		LastFrameTime = (int) (MainLoopElapsedSeconds * 1000);
	}
	
	public struct SleepState
	{	
		public double estimate = 0.0015;
		
		public double mean = 0.0015;
		
		public double m2 = 0;
		
		public long count = 1;
		
		public SleepState()
		{ }
		
		public static SleepState Default = new();
	}
	
	private static void Sleep(double Seconds, ref SleepState s)
	{
		double observed;
		int powersaving_count = 0;
		
		// Power saving section
		double powersaving_time_start = CurrentTimeSeconds;
		PlatformEnableHighResolutionTiming();
		
		do
		{
			double start = CurrentTimeSeconds;
			Thread.Sleep(1);
			observed = CurrentTimeSeconds - start;
			
			Seconds -= observed;
			
			s.estimate = UpdateEstimate(observed, ref s);
			
			powersaving_count++;
		}
		while (Seconds > s.estimate);
		
		PlatformDisableHighResolutionTiming();
		double powersaving_time = CurrentTimeSeconds - powersaving_time_start;
		
		// Spin lock section
		double spinlock_time_start = CurrentTimeSeconds;
		
		int spinlock_count = 0;
		var spin_lock_start = CurrentTimeSeconds;
		while ((CurrentTimeSeconds - spin_lock_start) < Seconds) spinlock_count++;
		
		double spinlock_time = CurrentTimeSeconds - spinlock_time_start;
		
		
		double total_sleep_time = powersaving_time + spinlock_time;
		
		SleepPowerSavingPercentage = powersaving_time / total_sleep_time;
		SleepSpinlockPercentage = spinlock_time / total_sleep_time;
		
		NumPowerSavingIterations = powersaving_count;
		NumSpinlockIterations = spinlock_count;
		
		// local helper function (thank you https://blog.bearcats.nl/accurate-sleep-function/)
		static double UpdateEstimate(double observed, ref SleepState s)
		{
			double delta = observed - s.mean;
			s.count++;
			s.mean += delta / s.count;
			s.m2   += delta * (observed - s.mean);
			double stddev = Math.Sqrt(s.m2 / (s.count - 1));
			return s.mean + stddev;
		}
	}
	
	public static double SleepPowerSavingPercentage { get; private set; } = 0.0;
	public static double SleepSpinlockPercentage { get; private set; } = 0.0;
	
	public static int NumPowerSavingIterations { get; private set; } = 0;
	public static int NumSpinlockIterations { get; private set; } = 0;
	
	//private static IntPtr timer = Kernel32.CreateWaitableTimer(nint.Zero, false, null);
	
	//	private static void WinSleep(double Seconds, ref SleepState s)
	//	{
	//		// Win32 fuckery section (thank you https://blog.bearcats.nl/accurate-sleep-function/)
	//		
	//		WinMM.TimeBeginPeriod(1);
	//		
	//		do
	//		{
	//			double to_wait = Seconds - s.estimate;
	//			long due = -(long)(to_wait * 1e7);
	//			
	//			var start = CurrentTimeSeconds;
	//			Kernel32.SetWaitableTimerEx(timer, ref due, 0, null, nint.Zero, nint.Zero, 0);
	//			Kernel32.WaitForSingleObject(timer, (int)Kernel32.INFINITE);
	//			var observed = CurrentTimeSeconds - start;
	//			
	//			Seconds -= observed;
	//			
	//			s.count++;
	//			
	//			double error = observed - to_wait;
	//			double delta = error - s.mean;
	//			s.mean += delta / s.count;
	//			s.m2 += delta * (error - s.mean);
	//			double stddev = Math.Sqrt(s.m2 / (s.count - 1));
	//			s.estimate = s.mean + stddev;
	//		}
	//		while (Seconds - s.estimate > 1e-7);
	//		
	//		WinMM.TimeEndPeriod(1);
	//		
	//		// Spin lock section
	//		var spin_lock_start = CurrentTimeSeconds;
	//		while ((CurrentTimeSeconds - spin_lock_start) < Seconds);
	//		
	//		Kernel32.CloseHandle(timer);
	//	}
	
	//	private void ClearBounds(Rectangle Bounds)
	//	{
	//		Span<char> Whitespace
	//	}
	
	public static void Run()
	{
		if (!Initialized)
			throw new Exception("Haven is not initialized.");
		
		Console.CursorVisible = false;

		//Thread DiagnosticsThread = new(DiagnosticsServerHandler) { IsBackground = true };
		//DiagnosticsThread.Start();
		
		AppActive = true;
		FPSIntervalTimer.Start();
		
		var main = Task.Run(delegate
		{
			while (AppActive)
			{
				MainLoop();
				
				if (FrameRateLimit > 0)
				{
					double TargetTime = 1.0 / FrameRateLimit;
					Sleep(TargetTime - MainLoopElapsedSeconds, ref MainSleepState);
				}
			}
		});
		
		var worker = Task.Run(delegate
		{
			while (AppActive)
			{
				MainLoopWorkerRoutine();
				
				if (FrameRateLimit > 0)
				{
					double TargetTime = 1.0 / FrameRateLimit;
					Sleep(TargetTime - WorkerElapsedSeconds, ref MainSleepState);
				}
			}
		});
		
		Task.WaitAll(main, worker);

		//DiagnosticsPipe.Disconnect();
		//DiagnosticsPipe.Dispose();

		Console.ResetColor();
		Console.Clear();
	}
	
	private static double CurrentTimeSeconds => DateTime.Now.TimeOfDay.TotalSeconds;
	
	public static void In(int Milliseconds, Action Action)
	{
		Task.Run(delegate
		{
			Thread.Sleep(Milliseconds);
			Action();
		});
	}

	//	private XmlSerializer PipeSerializer = new(typeof(DiagInfo));
	//	
	//	private void DiagnosticsServerHandler()
	//	{
	//		DiagnosticsPipe.WaitForConnection();
	//	
	//		XmlWriter PipeWriter = XmlWriter.Create(DiagnosticsPipe);
	//		StreamReader PipeReader = new(DiagnosticsPipe);
	//	
	//		while (DiagnosticsPipe.IsConnected)
	//		{
	//			var InputMessage = DiagnosticsPipe.ReadByte();
	//	
	//			switch (InputMessage)
	//			{
	//				case DiagMessage.GetDiagInfo:
	//					PipeWriter.Settings.NewLineChars = "";
	//					PipeWriter.Settings.NewLineHandling = NewLineHandling.Replace;
	//					PipeWriter.Settings.Indent = false;
	//	
	//					DiagnosticsPipe.WriteByte(DiagMessage.Success);
	//					PipeSerializer.Serialize(PipeWriter, DiagInfo.FromCurrentState());
	//					break;
	//			}
	//	
	//	
	//	
	//			Thread.Sleep(100);
	//		}
	//	
	//		PipeWriter.Dispose();
	//		PipeReader.Dispose();
	//	}

	#region Debug

	public static void DumpRenderBuffers(int Quantity)
	{
		RenderBufferDumpPending = true;
		RenderBufferDumpQuantity = Quantity;
	}

	#endregion

	#region Modal Stuff

	public static void DoModal(ConsoleKey QuitKey, params string[] Messages)
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

	private static void DrawWindow(int X, int Y, int Width, int Height)
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

	private static void AwaitKeypress(ConsoleKey Key)
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

	private static void PauseMainloop()
	{
		RequestPause = true;

		while (!AppPaused)
			Thread.Sleep(25);

		FPSIntervalTimer.Enabled = false;
	}

	private static void ResumeMainloop()
	{
		RequestPause = false;
		AppPaused = false;
		
		FPSIntervalTimer.Enabled = true;
	}

	/// <summary>
	/// Signals the MainLoop to exit after the current iteration. This is the primary way of closing a Haven Application.
	/// </summary>
	public static void SignalExit() => AppActive = false;

	#endregion

	#region UpdateTask Controls

	public static bool AddUpdateTask(string TaskID, Action<State> Action)
	{
		return UpdateTasks.TryAdd(TaskID, Action);
	}

	public static bool RemoveUpdateTask(string TaskID)
	{
		if (!UpdateTasks.ContainsKey(TaskID))
			return false;

		var kvp = UpdateTasks.First(kvp => kvp.Key == TaskID);
		return UpdateTasks.TryRemove(kvp);
	}

	public static bool HasUpdateTask(string TaskID) => UpdateTasks.ContainsKey(TaskID);

	#endregion

	#region Layer Controls
	
	public static void AddLayer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string ID) where T : Layer => AddLayer(ID, Activator.CreateInstance<T>());
	
	public static void AddLayer(string ID, Layer Layer)
	{
		if (ID == "")
			throw new Exception("ID cannot be empty");

		Layer.InitUpdateTasks();
		Layer.InitWidgets();

		Layers.Add(ID, Layer);
	}
	
	/// <summary>
	/// Retrieves a layer object from the internal dictionary of layers. Can return null.
	/// </summary>
	/// <param name="ID">ID of the layer object to retrieve</param>
	/// <returns>The layer with the specified ID or null if the ID does not correspond to any layer</returns>
	public static Layer GetLayer(string ID) => Layers.FirstOrDefault(kvp => kvp.Key == ID).Value;
	
	public static T GetLayer<T>() where T : Layer
	{
		Layer l = Layers.Values.FirstOrDefault(t => t.GetType() == typeof(T) );
		
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
	
	public static T GetLayer<T>(string ID) where T : Layer
	{
		Layer l = Layers.FirstOrDefault(kvp => kvp.Key == ID).Value;
		
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
	
	public static Layer GetLayer(int ZIndex)
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
	public static void SetLayer(int ZIndex, string ID = "", params object[] Args)
	{
		bool EmptyID = ID == "";

		if (!Layers.ContainsKey(ID) && !EmptyID)
			return;

		bool ZIndexInvalid = ZIndex < 0 || ZIndex >= ActiveLayers.Length;

		if (ZIndexInvalid)
			return;

		if (ActiveLayers[ZIndex] is not null)
		{
			ActiveLayers[ZIndex]._OnHide();
			ActiveLayers[ZIndex] = null;
		}

		if (EmptyID)
			return;

		ActiveLayers[ZIndex] = Layers[ID];
		ActiveLayers[ZIndex].ZIndex = ZIndex;

		Layers[ID]._OnShow(Args);
	}
	
	public static bool RemoveLayer(string ID)
	{
		if (!Layers.ContainsKey(ID))
			return false;
		
		return Layers.Remove(ID);
	}
	
	public static bool IsLayerVisible(string ID)
	{
		if (!Layers.ContainsKey(ID))
			return false;

		return ActiveLayers.Contains(Layers[ID]);
	}

	#endregion
}