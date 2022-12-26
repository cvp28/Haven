using System.Collections.Concurrent;
using System.Text;

namespace Haven;

public class HConsole : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public int CursorX { get; set; }
	public int CursorY { get; set; }

	public bool CursorVisible { get; set; }
	public int CursorBlinkIntervalMs { get; set; }

	public ConsoleColor CursorForeground { get; set; }
	public ConsoleColor CursorBackground { get; set; }

	public int Width { get; private set; }
	public int Height { get; private set; }

	public ConsoleColor ForegroundColor { get; set; }
	public ConsoleColor BackgroundColor { get; set; }

	public bool KeyAvailable => InputBuffer.Count > 0;

	private ConcurrentQueue<ConsoleKeyInfo> InputBuffer;
	private CharacterInfo[] ScreenBuffer;
	private CharacterInfo[] ClearBuffer;
	private int TotalCells;

	private bool DoCursorRender;
	private bool IsFullscreen;

	public Action<string> OnInput { get; set; }

	public HConsole()
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
		CursorBlinkIntervalMs = 500;

		IsFullscreen = true;
		InitializeBuffers(Console.WindowWidth, Console.WindowHeight);
	}

	public HConsole(int X, int Y, int Width, int Height)
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
		CursorBlinkIntervalMs = 500;

		IsFullscreen = false;
		InitializeBuffers(Width, Height);
	}

	private void InitializeBuffers(int Width, int Height)
	{
		this.Width = Width;
		this.Height = Height;

		TotalCells = Width * Height;

		InputBuffer = new();

		ScreenBuffer = new CharacterInfo[TotalCells];
		ClearBuffer = new CharacterInfo[TotalCells];

		CursorForeground = ConsoleColor.Black;
		CursorBackground = ConsoleColor.White;

		ForegroundColor = ConsoleColor.White;
		BackgroundColor = ConsoleColor.Black;

		for (int i = 0; i < TotalCells; i++)
		{
			ClearBuffer[i].Character = ' ';
			ClearBuffer[i].Foreground = ForegroundColor;
			ClearBuffer[i].Background = BackgroundColor;
		}

		Clear();
	}

	public ConsoleKeyInfo ReadKey(bool Intercept = false)
	{
		while (!KeyAvailable)
			continue;

		ConsoleKeyInfo cki;

		while (!InputBuffer.TryDequeue(out cki))
			continue;

		if (!Intercept)
			Write(cki.KeyChar);

		return cki;
	}

	public void Clear()
	{
		Array.Copy(ClearBuffer, ScreenBuffer, TotalCells);
		CursorX = 0;
		CursorY = 0;
	}

	public override void Draw(Renderer s)
	{
		//	if (IsFullscreen)
		//		DrawFullscreen(s);
		//	else
		//		DrawWindowed(s);
	}

	//	private void DrawFullscreen(Renderer s)
	//	{
	//		s.CopyToBuffer2D(0, 0, Width, Height, Scr ref ScreenBuffer);
	//	
	//		if (DoCursorRender && CursorVisible)
	//			s.AddColorsAt(X + CursorX, Y + CursorY, CursorForeground, CursorBackground);
	//	}
	//	
	//	private void DrawWindowed(Renderer s)
	//	{
	//		s.DrawBox(X, Y, Width + 2, Height + 2);
	//	
	//		s.CopyToBuffer2D(X + 1, Y + 1, Width, Height, ref ScreenBuffer);
	//	
	//		if (DoCursorRender && CursorVisible)
	//			s.AddColorsAt(X + CursorX + 1, Y + CursorY + 1, CursorForeground, CursorBackground);
	//	}

	public override void OnConsoleKey(ConsoleKeyInfo cki) => InputBuffer.Enqueue(cki);

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

	public void NextLine()
	{
		if (ReachedHeightLimit())
			ScrollDown();
		else
			CursorY++;

		CursorX = 0;
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
		ScreenBuffer[CellIndex].Foreground = ForegroundColor;
		ScreenBuffer[CellIndex].Background = BackgroundColor;
	}

	private StringBuilder ReadLineBuffer;
	private Thread ReadLineThread;
	private bool AbortReadLine = false;

	public void BeginReadLine()
	{
		AbortReadLine = false;

		if (ReadLineBuffer is null)
			ReadLineBuffer = new();

		ReadLineThread = new Thread(ReadLineProcedure);
		ReadLineThread.Start();
	}

	public void CancelReadLine()
	{
		AbortReadLine = true;
	}

	private void ReadLineProcedure()
	{
		CursorVisible = false;
		int InitialX = CursorX;
		int InitialY = CursorY;
		int CurrentBufferIndex = 0;
		ConsoleKeyInfo cki;
		ConsoleColor InitialForeground = ForegroundColor;
		ConsoleColor InitialBackground = BackgroundColor;

		do
		{
			if (KeyAvailable)
				cki = ReadKey(true);
			else
			{
				if (AbortReadLine)
				{
					ClearLine();
					ReadLineBuffer.Clear();
					return;
				}
				else
				{
					continue;
				}
			}

			switch (cki.Key)
			{
				case ConsoleKey.Enter:
					string Result = ReadLineBuffer.ToString();
					ReadLineBuffer.Clear();
					Write('\n');
					OnInput?.Invoke(Result);
					CursorVisible = true;
					return;

				case ConsoleKey.LeftArrow:
					CursorLeft();
					break;

				case ConsoleKey.RightArrow:
					CursorRight();
					break;

				case ConsoleKey.Home:
					CursorToStart();
					break;

				case ConsoleKey.End:
					CursorToEnd();
					break;

				case ConsoleKey.Backspace:
					Backspace();
					break;

				default:
					char c = cki.KeyChar;
					bool Valid = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c);

					if (!Valid)
						break;

					ReadLineBuffer.Insert(CurrentBufferIndex, c);
					CurrentBufferIndex++;
					break;
			}

			ClearLine();
			Redraw();
		}
		while (true);

		#region ReadLine Interal Procedures

		void CursorToStart()
		{
			CurrentBufferIndex = 0;
		}

		void CursorToEnd()
		{
			CurrentBufferIndex = ReadLineBuffer.Length;
		}

		void CursorLeft()
		{
			if (CurrentBufferIndex == 0)
				return;

			CurrentBufferIndex--;
		}

		void CursorRight()
		{
			if (CurrentBufferIndex == ReadLineBuffer.Length)
				return;

			CurrentBufferIndex++;
		}

		void Backspace(bool Force = false)
		{
			if (!Force)
				if (CurrentBufferIndex == 0)
					return;

			ClearLine();

			// Remove 1 character starting at the current buffer index
			ReadLineBuffer.Remove(CurrentBufferIndex - 1, 1);
			CurrentBufferIndex--;

			Redraw();
		}

		void Redraw()
		{
			CursorVisible = false;
			SetCursorPosition(InitialX, InitialY);

			for (int i = 0; i < ReadLineBuffer.Length; i++)
				Write(ReadLineBuffer[i]);
				//if (i == CurrentBufferIndex)
				//	WriteHighlighted(ReadLineBuffer[i]);
				//else
			CursorVisible = true;
		}

		void ClearLine()
		{
			CursorVisible = false;
			string spaces = new(' ', ReadLineBuffer.Length);
			SetCursorPosition(InitialX, InitialY);
			Write(spaces);
			SetCursorPosition(InitialX, InitialY);
			CursorVisible = true;
		}

		void WriteHighlighted(char c)
		{
			SetHighlighted();
			Write(c);
			SetUnhighlighted();
		}

		void SetHighlighted()
		{
			ForegroundColor = InitialBackground;
			BackgroundColor = InitialForeground;
		}

		void SetUnhighlighted()
		{
			ForegroundColor = InitialForeground;
			BackgroundColor = InitialBackground;
		}

		#endregion
	}

	public (int X, int Y) GetCursorPosition() => (CursorX, CursorY);

	public void SetCursorPosition(int X, int Y)
	{
		TrySetCursorPosition(X, Y);
	}

	private bool TrySetCursorPosition(int X, int Y)
	{
		if (X < 0 || X >= Width)
			return false;

		if (Y < 0 || Y >= Height)
			return false;

		CursorX = X;
		CursorY = Y;

		return true;
	}

	public void WriteLine(string Text)
	{
		Write(Text);
		NextLine();
	}

	public void WriteLine(char Character)
	{
		Write(Character);
		NextLine();
	}

	public void Write(string Text)
	{
		foreach (char c in Text)
			Write(c);
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

	public void WriteCharInPlace(char Character)
	{
		if (Character == '\n')
			return;
		else if (Character == '\r')
			return;

		ModifyChar(Character);
	}

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