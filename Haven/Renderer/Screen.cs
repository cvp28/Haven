using System.Text;

namespace Haven;

public class Screen : IRenderer
{

	private Stream StandardOutput;
	private int TotalCells;
	private int TotalBytes;
	private byte[] ScreenBuffer;
	private byte[] FinalFrame;
	private byte[] ClearScreen;

	private int WindowWidth;
	private int WindowHeight;

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
		ScreenBuffer = new byte[TotalBytes];			// Primary screen buffer for writing data to screen
		FinalFrame = new byte[TotalBytes];				// The final frame that is written to the screen (stripped of NULL bytes from the ScreenBuffer writing to StandardOut)
		ClearScreen = new byte[TotalBytes];				// A buffer intended for use in clearing the ScreenBuffer through a simple Array.Copy call

		// Initialize each element of ClearScreen to the values that will be held for a clear ScreenBuffer before every frame is drawn
		for (int i = 0; i < TotalCells; i++)
			ClearScreen[i * 14 + 10] = (byte) ' ';

		// Initialize each element of screen buffer
		ClearBuffer();
	}


	public void Render(List<Widget> Widgets)
	{
		// Reset cursor to 0, 0
		StandardOutput.Write(Sequences.CursorToTopLeft, 0, Sequences.CursorToTopLeft.Length);

		ClearBuffer();

		// -- WIDGET RENDERING STARTS HERE --

		if (Widgets is not null)
			foreach (Widget w in Widgets)
				if (w.Visible)
					w.Draw(this);

		// -- WIDGET RENDERING ENDS HERE --

		// Copy every non-NULL byte in the primary screen buffer to the secondary screen buffer
		// so that way we're not writing any useless information to the output stream
		int FrameDataIndex = 0;

		for (int i = 0; i < ScreenBuffer.Length; i++)
			if (ScreenBuffer[i] != 0)
			{
				FinalFrame[FrameDataIndex] = ScreenBuffer[i];
				FrameDataIndex++;
			}

		// Write cleansed buffer to screen
		StandardOutput.Write(FinalFrame, 0, FrameDataIndex);
	}

	private int IX(int X, int Y)
	{
		int Cell = Y * WindowWidth + X;

		if (Cell > TotalCells)
			return Cell % TotalCells;
		else
			return Cell;
	}

	public void WriteStringAt(int X, int Y, string Text)
	{
		int CellIndex = IX(X, Y);

		for (int i = 0; i < Text.Length; i++)
			ModifyCharAt(CellIndex + i, Text[i]);
	}

	public void WriteColorStringAt(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background)
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

	public void WriteBox(int X, int Y, int Width, int Height)
	{
		int CellIndex = IX(X, Y);

		ModifyCharAt(CellIndex, BoxChars.TopLeft);
		ModifyCharAt(CellIndex + Width - 1, BoxChars.TopRight);
		ModifyCharAt(CellIndex + (WindowWidth * (Height - 1)), BoxChars.BottomLeft);
		ModifyCharAt(CellIndex + (WindowWidth * (Height - 1) + Width - 1), BoxChars.BottomRight);


		// Draw top and bottom
		for (int i = 1; i <= Width - 2; i++)
		{
			ModifyCharAt(CellIndex + i, BoxChars.Horizontal);
			ModifyCharAt(CellIndex + (WindowWidth * (Height - 1) + i), BoxChars.Horizontal);
		}

		// Draw left and right
		for (int i = 1; i <= Height - 2; i++)
		{
			ModifyCharAt(CellIndex + (WindowWidth * i), BoxChars.Vertical);
			ModifyCharAt(CellIndex + (WindowWidth * i) + Width - 1, BoxChars.Vertical);
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

	public void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(X, Y);

		ModifyForegroundAt(CellIndex, Sequences.ToFgByteArray(Foreground));
		ModifyBackgroundAt(CellIndex, Sequences.ToBgByteArray(Background));
		AddClearAt(CellIndex);
	}

	public void CopyToBuffer2D(int X, int Y, int LineWidth, CharacterInfo[] Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < Buffer.Length; i++)
		{
			int CellIndex = IX(X + OffX, Y + OffY);

			ModifyCharAt(CellIndex, Buffer[i].Character);

			if (Buffer[i].DoColors)
			{
				ModifyForegroundAt(CellIndex, Sequences.ToFgByteArray(Buffer[i].Foreground));
				ModifyBackgroundAt(CellIndex, Sequences.ToBgByteArray(Buffer[i].Background));
			}
			
			if (Buffer[i].SignalAnsiClear)
				AddClearAt(CellIndex);

			if (OffX == LineWidth - 1)
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
