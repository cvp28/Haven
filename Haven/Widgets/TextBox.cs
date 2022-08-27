
namespace Haven;

public class TextBox : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public int CursorX { get; set; }
	public int CursorY { get; set; }

	public bool CursorVisible { get; set; }
	public int CursorBlinkIntervalMs { get; set; }

	public int Width { get; private set; }
	public int Height { get; private set; }

	private CharacterInfo[] ScreenBuffer;
	private CharacterInfo[] ClearBuffer;
	private int TotalCells;

	private bool DoCursorRender;

	public TextBox()
	{
		X = 0;
		Y = 0;

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

		CursorX = 0;
		CursorY = 0;
		CursorVisible = true;
		CursorBlinkIntervalMs = 250;

		Width = Console.WindowWidth - 2;
		Height = Console.WindowHeight - 2;

		TotalCells = Width * Height;

		ScreenBuffer = new CharacterInfo[TotalCells];
		ClearBuffer = new CharacterInfo[TotalCells];

		for (int i = 0; i < TotalCells; i++)
		{
			ClearBuffer[i].Character = ' ';
			ClearBuffer[i].Foreground = ConsoleColor.White;
			ClearBuffer[i].Background = ConsoleColor.Black;
			ClearBuffer[i].DoColors = false;
		}

		Clear();
	}

	public TextBox(int X, int Y, int Width, int Height) : base()
	{
		this.X = X;
		this.Y = Y;

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

		CursorX = 0;
		CursorY = 0;
		CursorVisible = true;
		CursorBlinkIntervalMs = 250;

		this.Width = Width;
		this.Height = Height;

		TotalCells = Width * Height;

		ScreenBuffer = new CharacterInfo[TotalCells];
		ClearBuffer = new CharacterInfo[TotalCells];

		for (int i = 0; i < TotalCells; i++)
		{
			ClearBuffer[i].Character = ' ';
			ClearBuffer[i].Foreground = ConsoleColor.White;
			ClearBuffer[i].Background = ConsoleColor.Black;
			ClearBuffer[i].DoColors = false;
		}

		Clear();
	}

	public TextBox(ScreenSpace s) : base()
	{
		X = 0;
		Y = 0;
		Width = 1;
		Height = 1;

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

		CursorX = 0;
		CursorY = 0;
		CursorVisible = true;
		CursorBlinkIntervalMs = 250;

		TotalCells = Width * Height;

		ScreenBuffer = new CharacterInfo[TotalCells];
		ClearBuffer = new CharacterInfo[TotalCells];

		for (int i = 0; i < TotalCells; i++)
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

	public void SetDimensions(int Width, int Height)
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

	public void Clear()
	{
		Array.Copy(ClearBuffer, ScreenBuffer, TotalCells);
		CursorX = 0;
		CursorY = 0;
	}

	public override void Draw(IRenderer s)
	{
		s.DrawBox(X, Y, Width + 2, Height + 2);

		s.CopyToBuffer2D(X + 1, Y + 1, Width, ScreenBuffer);

		if (DoCursorRender && CursorVisible)
			s.AddColorsAt(X + CursorX + 1, Y + CursorY + 1, ConsoleColor.Black, ConsoleColor.White);
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{

	}

	private bool ReachedWidthLimit() => CursorX == Width - 1;

	private bool ReachedHeightLimit() => CursorY == Height - 1;

	public void ScrollDown()
	{
		// Shift entire buffer up by one line
		Array.Copy(ScreenBuffer, Width, ScreenBuffer, 0, ScreenBuffer.Length - Width);

		// Clear last line
		for (int i = Width * (Height - 1); i < TotalCells; i++)
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
		int Cell = Y * Width + X;

		if (Cell > TotalCells)
			return Cell % TotalCells;
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
		if (ReachedHeightLimit())
			ScrollDown();
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
		for (int i = 0; i <  Text.Length; i++)
		{
			if (ReachedWidthLimit() || i == Text.Length - 1)
				AddClear();

			Write(Text[i], Foreground, Background);
		}
	}

	public void Write(char Character)
	{
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
