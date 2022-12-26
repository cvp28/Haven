
namespace Haven;

public class TextBox : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public int CursorX { get; set; }
	public int CursorY { get; set; }

	public bool CursorVisible { get; set; }
	public int CursorBlinkIntervalMs { get; set; }

	public int Width { get; set; }
	public int Height { get; set; }

	private CharacterInfo[] ScreenBuffer;
	private CharacterInfo[] ClearBuffer;
	private CharacterInfo[] TempBuffer;

	private bool DoCursorRender;

	private bool DoLogging;
	private FileStream LogStream;
	private StreamWriter LogStreamWriter;

	private readonly int ScreenBufferWidth = Console.LargestWindowWidth;
	private readonly int ScreenBufferHeight = Console.LargestWindowHeight;

	private readonly int ScreenBufferSize = Console.LargestWindowWidth * Console.LargestWindowHeight;

	/// <summary>
	/// Creates a fullscreen TextBox
	/// </summary>
	public TextBox()
	{
		X = 0;
		Y = 0;

		DoLogging = false;

		Width = Console.WindowWidth - 2;
		Height = Console.WindowHeight - 2;

		SetupCursor();
		InitializeBuffers();
	}

	/// <summary>
	/// Creates a TextBox at the desired location with the desired width and height
	/// </summary>
	public TextBox(int X, int Y, int Width, int Height) : base()
	{
		this.X = X;
		this.Y = Y;

		DoLogging = false;

		this.Width = Width;
		this.Height = Height;

		SetupCursor();
		InitializeBuffers();
	}

	/// <summary>
	/// Creates an auto-sized textbox according to the desired screen partition
	/// </summary>
	public TextBox(ScreenSpace s) : base()
	{
		X = 0;
		Y = 0;
		Width = 1;
		Height = 1;

		DoLogging = false;

		Dimensions CurrentDimensions = Dimensions.Current;

		switch (s)
		{
			case ScreenSpace.TopLeftQuad:
				SetPosition(0, 0);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.TopRightQuad:
				SetPosition(CurrentDimensions.WindowWidth / 2, 0);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.BottomLeftQuad:
				SetPosition(0, CurrentDimensions.WindowHeight / 2);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.BottomRightQuad:
				SetPosition(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight / 2);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.TopHalf:
				SetPosition(0, 0);
				SetDimensions(CurrentDimensions.WindowWidth, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.BottomHalf:
				SetPosition(0, CurrentDimensions.WindowHeight / 2);
				SetDimensions(CurrentDimensions.WindowWidth, CurrentDimensions.WindowHeight / 2);
				break;

			case ScreenSpace.LeftHalf:
				SetPosition(0, 0);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight);
				break;

			case ScreenSpace.RightHalf:
				SetPosition(CurrentDimensions.WindowWidth / 2, 0);
				SetDimensions(CurrentDimensions.WindowWidth / 2, CurrentDimensions.WindowHeight);
				break;

			case ScreenSpace.Full:
				SetPosition(0, 0);
				SetDimensions(CurrentDimensions.WindowWidth, CurrentDimensions.WindowHeight);
				break;
		}

		SetupCursor();
		InitializeBuffers();
	}

	private void SetupCursor()
	{
		CursorX = 0;
		CursorY = 0;
		CursorVisible = true;
		CursorBlinkIntervalMs = 250;

		DoCursorRender = true;
		Task.Run(() =>
		{
			// gotta love english inside of if statements
			while (this is not null)
			{
				Thread.Sleep(CursorBlinkIntervalMs);
				DoCursorRender = !DoCursorRender;
			}
		});
	}

	private void InitializeBuffers()
	{
		ScreenBuffer = new CharacterInfo[ScreenBufferSize];
		ClearBuffer = new CharacterInfo[ScreenBufferSize];
		TempBuffer = new CharacterInfo[ScreenBufferSize];

		for (int i = 0; i < ScreenBufferSize; i++)
		{
			ClearBuffer[i].Character = ' ';
			ClearBuffer[i].Foreground = ConsoleColor.White;
			ClearBuffer[i].Background = ConsoleColor.Black;
			ClearBuffer[i].DoColors = false;
		}

		Clear();
	}

	public void SetPosition(int X, int Y)
	{
		if (!CheckPosition(X, Y))
			return;

		this.X = X;
		this.Y = Y;
	}

	private void SetDimensions(int Width, int Height)
	{
		this.Width = Width - 2;
		this.Height = Height - 2;
	}

	private bool CheckPosition(int X, int Y)
	{
		Dimensions CurrentDimensions = Dimensions.Current;

		bool ValidX = X >= 0 && X + Width + 2 <= CurrentDimensions.WindowWidth;
		bool ValidY = Y >= 0 && Y + Height + 2 <= CurrentDimensions.WindowHeight;

		return ValidX && ValidY;
	}

	public void Resize(int NewWidth, int NewHeight)
	{
		if (Width == NewWidth && Height == NewHeight)
			return;

		//	int OriginalWidth = Width;
		//	int OriginalHeight = Height;
		//	
		//	// Make sure TempBuffer is clear first
		//	Array.Copy(ClearBuffer, TempBuffer, ScreenBufferSize);
		//	
		//	// Copy current buffer (before resizing) to TempBuffer
		//	Array.Copy(ScreenBuffer, TempBuffer, OriginalWidth * OriginalHeight);
		//	
		Width = NewWidth;
		Height = NewHeight;
		//	
		//	for (int i = 0; i < OriginalWidth * OriginalHeight; i++)
		//	{
		//	
		//	}
	}

	public void Clear()
	{
		Array.Copy(ClearBuffer, ScreenBuffer, ScreenBufferSize);
		CursorX = 0;
		CursorY = 0;
	}

	public override void Draw(Renderer s)
	{
		// Draw window border
		s.DrawBox(X, Y, Width + 2, Height + 2);

		// Draw buffer view
		s.CopyToBuffer2D(X + 1, Y + 1, Width, Height, ScreenBufferWidth, ref ScreenBuffer);

		if (ShouldDrawCursor())
			s.AddColorsAt(X + CursorX + 1, Y + CursorY + 1, ConsoleColor.Black, ConsoleColor.White);
	}

	private bool ShouldDrawCursor()
	{
		bool XGood = CursorX < X + Width - 1;
		bool YGood = CursorY < Y + Height - 1;

		bool IsCursorInView = XGood && YGood;

		return DoCursorRender && CursorVisible && IsCursorInView;
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki) { }

	public void SetLogFile(string Path)
	{
		DoLogging = true;

		if (LogStream is not null)
			LogStream.Dispose();

		if (LogStreamWriter is not null)
			LogStreamWriter.Dispose();

		LogStream = File.Open(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
		LogStreamWriter = new(LogStream);

		LogStreamWriter.AutoFlush = true;
	}

	public void DisableLogFile()
	{
		DoLogging = false;
		LogStream?.Dispose();
		LogStreamWriter?.Dispose();
	}

	private bool ReachedWidthLimit() => CursorX >= Width - 1;

	private bool ReachedHeightLimit() => CursorY >= Height - 1;

	public void ScrollDown()
	{
		// Shift entire buffer up by one line
		Array.Copy(ScreenBuffer, ScreenBufferWidth, ScreenBuffer, 0, ScreenBuffer.Length - ScreenBufferWidth);

		// Clear last line
		for (int i = ScreenBufferWidth * (ScreenBufferHeight - 1); i < ScreenBufferSize; i++)
			ScreenBuffer[i] = ClearBuffer[i];
	}

	private void AdvanceCursor()
	{
		if (ReachedWidthLimit())
			NextLine();
		else
			CursorX++;
	}

	public int IX(int X, int Y)
	{
		int Cell = Y * ScreenBufferWidth + X;

		if (Cell >= ScreenBufferSize)
			return Cell % ScreenBufferSize;
		else
			return Cell;
	}

	private void ModifyChar(char Character)
	{
		int CellIndex = IX(CursorX, CursorY);
		ScreenBuffer[CellIndex].Character = Character;
	}

	private void ModifyChar(char Character, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(CursorX, CursorY);

		ScreenBuffer[CellIndex].Character = Character;
		ScreenBuffer[CellIndex].Foreground = Foreground;
		ScreenBuffer[CellIndex].Background = Background;
		ScreenBuffer[CellIndex].DoColors = true;
	}

	private void AddClear()
	{
		int CellIndex = IX(CursorX, CursorY);
		ScreenBuffer[CellIndex].SignalAnsiClear = true;
	}

	public void NextLine()
	{
		if (DoLogging)
			LogStreamWriter.WriteLine();

		if (ReachedHeightLimit())
		{
			int LineCount ;

			if (CursorY > Height - 1)
			{
				LineCount = CursorY - Height + 2;
				CursorY = Height - 1;
			}
			else
				LineCount = 1;

			for (int i = 0; i < LineCount; i++)
				ScrollDown();
		}
		else
			CursorY++;

		CursorX = 0;
	}

	public void WriteLine(string Text)
	{
		Write(Text);
		NextLine();
	}

	public void WriteLine(string Text, ConsoleColor Foreground, ConsoleColor Background)
	{
		Write(Text, Foreground, Background);
		NextLine();
	}

	public void WriteLine(char Character)
	{
		Write(Character);
		NextLine();
	}

	public void WriteLine(char Character, ConsoleColor Foreground, ConsoleColor Background)
	{
		Write(Character, Foreground, Background);
		NextLine();
	}

	public void Write(string Text)
	{
		foreach (char c in Text)
			Write(c);
	}

	public void Write(string Text, ConsoleColor Foreground, ConsoleColor Background)
	{
		for (int i = 0; i < Text.Length; i++)
		{
			if (ReachedWidthLimit() || i == Text.Length - 1)
				AddClear();

			Write(Text[i], Foreground, Background);
		}
	}

	public void Write(char Character)
	{
		if (DoLogging)
			LogStreamWriter.Write(Character);

		if (Character == '\r')
		{
			CursorX = 0;
			return;
		}

		if (Character == '\n')
		{
			NextLine();
			return;
		}

		ModifyChar(Character);
		AdvanceCursor();
	}

	public void Write(char Character, ConsoleColor Foreground, ConsoleColor Background)
	{
		if (DoLogging)
			LogStreamWriter.Write(Character);

		if (Character == '\r')
		{
			CursorX = 0;
			return;
		}

		if (Character == '\n')
		{
			NextLine();
			return;
		}

		ModifyChar(Character, Foreground, Background);
		AdvanceCursor();
	}

	public void WriteCharInPlace(char Character) => ModifyChar(Character);

	public void WriteCharInPlace(char Character, ConsoleColor Foreground, ConsoleColor Background) => ModifyChar(Character, Foreground, Background);

	public (int X, int Y) GetCursorPosition() => (CursorX, CursorY);


	public void CursorLeft()
	{
		if (CursorX > 0)
			CursorX--;
	}

	public void CursorRight()
	{
		if (CursorX < Width - 1)
			CursorX++;
	}

	public void CursorUp()
	{
		if (CursorY > 0)
			CursorY--;
	}

	public void CursorDown()
	{
		if (CursorY < Height - 1)
			CursorY++;
	}
}
