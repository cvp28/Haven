using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Haven;

public class Screen : Renderer
{

	private Stream StandardOutput;
	private int TotalCells;
	private int TotalBytes;
	private byte[] ScreenBuffer;
	private byte[] FinalFrame;
	private byte[] ClearScreen;

	private int WindowWidth;
	private int WindowHeight;

	private Stopwatch sw;

	private readonly int ScreenBufferSize = Console.LargestWindowWidth * Console.LargestWindowHeight * 14;

	public Screen()
	{
		// Acquire the stdout stream
		StandardOutput = Console.OpenStandardOutput(TotalBytes);

		// Preload the ANSI byte sequences into their respective byte arrays
		Sequences.Init();

		WindowWidth = Console.WindowWidth;
		WindowHeight = Console.WindowHeight;

		TotalCells = WindowWidth * WindowHeight;
		TotalBytes = TotalCells * 14;

		// Create screen buffer
		ScreenBuffer = new byte[ScreenBufferSize];			// Primary screen buffer for writing data to screen
		FinalFrame = new byte[ScreenBufferSize];				// The final frame that is written to the screen (stripped of NULL bytes from the ScreenBuffer writing to StandardOut)
		ClearScreen = new byte[ScreenBufferSize];				// A buffer intended for use in clearing the ScreenBuffer through a simple Array.Copy call

		// Initialize each element of ClearScreen to the values that will be held for a clear ScreenBuffer before every frame is drawn
		for (int i = 0; i < Console.LargestWindowWidth * Console.LargestWindowHeight; i++)
			ClearScreen[i * 14 + 10] = (byte) ' ';

		// Initialize each element of screen buffer
		ClearBuffer();

		sw = new();
	}

	public override void Render(IEnumerable<Widget> Widgets)
	{
		Console.CursorVisible = false;

		// Reset cursor to 0, 0
		StandardOutput.Write(Sequences.CursorToTopLeft, 0, Sequences.CursorToTopLeft.Length);

		ClearBuffer();

		// -- WIDGET RENDERING STARTS HERE --

		sw.Restart();

		if (Widgets is not null)
			foreach (Widget w in Widgets)
				if (w.Visible)
					w.Draw(this);

		WidgetRenderTimeMs = (int) sw.ElapsedMilliseconds;

		// -- WIDGET RENDERING ENDS HERE --

		// Copy every non-NULL byte in the primary screen buffer to the secondary screen buffer
		// so that way we're not writing any useless information to the output stream
		sw.Restart();

		int FrameDataIndex = 0;

		for (int i = 0; i < TotalBytes; i++)
			if (ScreenBuffer[i] != 0)
			{
				FinalFrame[FrameDataIndex] = ScreenBuffer[i];
				FrameDataIndex++;
			}

		DiagTime1Ms = (int) sw.ElapsedMilliseconds;

		sw.Restart();

		// Write cleansed buffer to screen
		StandardOutput.Write(FinalFrame, 0, FrameDataIndex);

		StdoutWriteTimeMs = (int) sw.ElapsedMilliseconds;
	}

	public override void UpdateScreenDimensions()
	{
		WindowWidth = Console.WindowWidth;
		WindowHeight = Console.WindowHeight;

		TotalCells = WindowWidth * WindowHeight;
		TotalBytes = TotalCells * 14;

		//Array.Resize(ref ClearScreen, TotalBytes);
		//Array.Resize(ref ScreenBuffer, TotalBytes);
		//Array.Resize(ref FinalFrame, TotalBytes);
	}

	private int IX(int X, int Y)
	{
		int Cell = Y * WindowWidth + X;

		if (Cell >= TotalCells)
			return Cell % TotalCells;
		else
			return Cell;
	}

	public override void WriteStringAt(int X, int Y, string Text)
	{
		int CellIndex = IX(X, Y);

		for (int i = 0; i < Text.Length; i++)
			ModifyCharAt(CellIndex + i, Text[i]);
	}

	public override void WriteColorStringAt(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background)
	{
		if (Text.Length == 0) { return; }

		int CellIndex = IX(X, Y);

		ModifyCharAt(CellIndex, Text[0]);
		ModifyForegroundAt(CellIndex, Sequences.ToFgByteArray(Foreground));
		ModifyBackgroundAt(CellIndex, Sequences.ToBgByteArray(Background));

		for (int i = 1; i < Text.Length; i++)
			ModifyCharAt(CellIndex + i, Text[i]);

		int ClearIndex = CellIndex + Text.Length - 1;

		AddClearAt(ClearIndex);
	}

	public override void DrawBox(int X, int Y, int Width, int Height)
	{
		int TopLeftIndex = IX(X, Y);
		int TopRightIndex = IX(X + Width - 1, Y);
		int BottomLeftIndex = IX(X, Y + Height - 1);
		int BottomRightIndex = IX(X + Width - 1, Y + Height - 1);

		ModifyCharAt(TopLeftIndex, BoxChars.TopLeft);
		ModifyCharAt(TopRightIndex, BoxChars.TopRight);
		ModifyCharAt(BottomLeftIndex, BoxChars.BottomLeft);
		ModifyCharAt(BottomRightIndex, BoxChars.BottomRight);


		// Draw top and bottom
		for (int i = 1; i <= Width - 2; i++)
		{
			int TopIndex = IX(X + i, Y);
			int BottomIndex = IX(X + i, Y + Height - 1);

			ModifyCharAt(TopIndex, BoxChars.Horizontal);
			ModifyCharAt(BottomIndex, BoxChars.Horizontal);
		}

		// Draw left and right
		for (int i = 1; i <= Height - 2; i++)
		{
			int LeftIndex = IX(X, Y + i);
			int RightIndex = IX(X + Width - 1, Y + i);

			ModifyCharAt(LeftIndex, BoxChars.Vertical);
			ModifyCharAt(RightIndex, BoxChars.Vertical);
		}
	}

	private void ModifyForegroundAt(int CellIndex, byte[] ColorSequence)
	{
		int BufferIndex = CellIndex * 14;

		for (int i = 0; i < ColorSequence.Length; i++)
			ScreenBuffer[BufferIndex + i] = ColorSequence[i];
	}

	private void ModifyBackgroundAt(int CellIndex, byte[] ColorSequence)
	{
		int BufferIndex = CellIndex * 14 + 5;

		for (int i = 0; i < ColorSequence.Length; i++)
			ScreenBuffer[BufferIndex + i] = ColorSequence[i];
	}

	private void AddClearAt(int CellIndex)
	{
		int BufferIndex = CellIndex * 14 + 11;

		for (int i = 0; i < 3; i++)
			ScreenBuffer[BufferIndex + i] = Sequences.Clear[i];
	}


	private void ModifyCharAt(int CellIndex, char Character)
	{
		int BufferIndex = CellIndex * 14 + 10;

		// If we exceed the buffer length, just write the char to the last screen position
		if (BufferIndex > TotalBytes - 1)
		{
			ScreenBuffer[TotalBytes - 1] = (byte) Character;
			return;
		}

		ScreenBuffer[BufferIndex] = (byte) Character;
	}

	public override void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(X, Y);

		ModifyForegroundAt(CellIndex, Sequences.ToFgByteArray(Foreground));
		ModifyBackgroundAt(CellIndex, Sequences.ToBgByteArray(Background));
		AddClearAt(CellIndex);
	}

	public override void CopyToBuffer2D(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, Span<CharacterInfo> Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < ViewWidth * ViewHeight; i++)
		{
			int CellIndex = IX(X + OffX, Y + OffY);

			ModifyCharAt(CellIndex, Buffer[OffX + BufferWidth * OffY].Character);

			if (Buffer[i].DoColors)
			{
				ModifyForegroundAt(CellIndex, Sequences.ToFgByteArray(Buffer[OffX + BufferWidth * OffY].Foreground));
				ModifyBackgroundAt(CellIndex, Sequences.ToBgByteArray(Buffer[OffX + BufferWidth * OffY].Background));
			}
			
			if (Buffer[i].SignalAnsiClear)
				AddClearAt(CellIndex);

			if (OffX == ViewWidth - 1)
			{
				OffX = 0;
				OffY++;
			}
			else
			{
				OffX++;
			}
		}
	}

	public void ClearBuffer()
	{
		// Clear screen buffer byte-for-byte to clear leftover color data
		// No need to clear FinalFrame because it gets overwritten every frame anyways
		Array.Copy(ClearScreen, ScreenBuffer, TotalBytes);
	}
}
