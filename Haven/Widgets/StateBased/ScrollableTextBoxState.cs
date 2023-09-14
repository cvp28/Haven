using System.Diagnostics;

namespace HavenUI;

public class ScrollableTextBoxState
{
	public int X { get; set; }
	public int Y { get; set; }

	private int _CursorY;
	private int _CursorX;

	public int CursorX
	{
		get => _CursorX;

		set
		{
			EnsureCursorVisible();
			
			_CursorX = value;

			if (OmitCursorHistoryEntries)
				return;

			ModifyHistory.Add(new()
			{
				Type = TextBoxModifyType.MoveCursor,

				CursorX = CursorX,
				CursorY = CursorY
			});
		}
	}

	public int CursorY
	{
		get => _CursorY;

		set
		{
			EnsureCursorVisible();
			
			_CursorY = value;

			if (OmitCursorHistoryEntries)
				return;

			ModifyHistory.Add(new()
			{
				Type = TextBoxModifyType.MoveCursor,

				CursorX = CursorX,
				CursorY = CursorY
			});
		}
	}

	public bool CursorVisible { get; set; }
	public int CursorBlinkIntervalMs { get; set; }

	internal bool DoCursor;
	private bool OmitCursorHistoryEntries;
	private int IndexUnderCursor => IX(CursorX, CursorY);
	public CharacterInfo CellUnderCursor => ScreenBuffer[IndexUnderCursor];

	public int Width { get; set; }
	public int Height { get; set; }

	public int BufferHeight => Height + ScrollbackLines;

	public byte ScrollbarColor { get; set; }
	public bool ScrollbarVisible { get; set; }

	internal int TotalCells => Width * (Height + ScrollbackLines);
	private int ScrollbackLines;

	internal List<CharacterInfo> ScreenBuffer;
	private List<CharacterInfo> ClearBuffer;

	private List<TextBoxModifyAction> ModifyHistory;
	
	private int ScreenBufferSize => Console.LargestWindowWidth * (Console.LargestWindowHeight + ScrollbackLines);

	internal int ViewY;

	private bool DoLogging;
	private FileStream LogStream;
	private StreamWriter LogStreamWriter;

	private static CharacterInfo BlankCharacter = new()
	{
		Character = ' ',
		Foreground = ConsoleColor.White.ToByte(),
		Background = ConsoleColor.Black.ToByte()
	};

	internal static readonly object ScreenBufferLock = new();
	internal Stopwatch CursorTimer = new();
	
	public ScrollableTextBoxState(int X, int Y, int Width, int Height, int ScrollbackSize = 150)
	{
		this.X = X;
		this.Y = Y;
		this.Width = Width;
		this.Height = Height;

		ScrollbackLines = ScrollbackSize + 1;

		// Create screen buffer with maximum amount of memory reserved but it will rarely ever actually be this size
		// Should make indexes easier to work with and translate between the widget and the renderer
		ScreenBuffer = new(ScreenBufferSize);
		ClearBuffer = new(ScreenBufferSize);

		// Initalize ModifyHistory
		ModifyHistory = new(1000);		// Arbitrary 1000 count before resizing - I figure that's a good amount of modifications to have before resizing I guess

		// Set up the clear buffer
		InitializeClearBuffer();

		// Clear the main screen buffer
		Clear();

		OmitCursorHistoryEntries = false;
		CursorBlinkIntervalMs = 500;
		CursorVisible = true;
		DoCursor = true;
		
		_CursorX = 0;
		_CursorY = 0;
		
		// Blinking cursor background task
		Task.Run(() =>
		{
			OuterLoop:
			while (CursorTimer.ElapsedMilliseconds < CursorBlinkIntervalMs)
				Thread.Sleep(1);
			
			DoCursor = !DoCursor;
			CursorTimer.Restart();
			
			if (this is not null)
				goto OuterLoop;

		});

		ScrollbarColor = ConsoleColor.DarkGray.ToByte();
		ScrollbarVisible = true;
		DoLogging = false;
	}
	
	private void InitializeClearBuffer()
	{
		ClearBuffer.Clear();

		// Set up clear buffer
		for (int i = 0; i < TotalCells; i++)
			ClearBuffer.Add(BlankCharacter);
	}

	// Can be used to manually override the blinking cursor and ensure that the cursor is visible at any given moment
	public void EnsureCursorVisible()
	{
		DoCursor = true;
		CursorTimer.Restart();
	}
	
	public void Clear()
	{
		lock (ScreenBufferLock)
		{
			ViewY = 0;

			CursorX = 0;
			CursorY = 0;

			ScreenBuffer.Clear();
			ScreenBuffer.AddRange(ClearBuffer);

			ModifyHistory.Clear();
		}
	}
	
	public void Resize(int Width, int Height)
	{
		lock (ScreenBufferLock)
		{
			if (Width <= 0)
				Width = 1;

			if (Height <= 0)
				Height = 1;

			// Prevent all this super computationally intensive stuff from happening if the dimensions have not even changed
			if (this.Width == Width && this.Height == Height)
				return;

			// Update with and height fields
			this.Width = Width;
			this.Height = Height;

			// Re-initialize the clear buffer so its size is consistent
			InitializeClearBuffer();
			
			// Clear screen buffer
			ScreenBuffer.Clear();
			ScreenBuffer.AddRange(ClearBuffer);

			OmitCursorHistoryEntries = true;

			CursorX = 0;
			CursorY = 0;

			// Replay the modify history of the textbox buffer
			foreach (var m in ModifyHistory.ToArray())
			{
				switch (m.Type)
				{
					case TextBoxModifyType.MoveCursor:
						CursorX = m.CursorX;
						CursorY = m.CursorY;
						break;

					case TextBoxModifyType.WriteCharInPlace:
						CursorX = m.CursorX;
						CursorY = m.CursorY;
						ModifyChar(m.Character, m.Foreground, m.Background);
						break;

					case TextBoxModifyType.Write:
						Write(m.Character, m.Foreground, m.Background, true);
						OmitCursorHistoryEntries = true;
						break;
				}
			}

			OmitCursorHistoryEntries = false;
		}
	}
	
	public int IX(int X, int Y)
	{
		int Cell = Y * Width + X;

		if (Cell >= TotalCells)
			return Cell % TotalCells;
		else
			return Cell;
	}
	
	public (int X, int Y) GetCursorPosition() => (CursorX, CursorY);
	
	public bool IsCursorInView() => (CursorY >= ViewY) && (CursorY < ViewY + Height);
	
	private bool ReachedBufferHeightLimit => CursorY == Height + ScrollbackLines - 1;
	private bool ReachedWindowWidthLimit => CursorX == Width - 1;
	
	private void ScrollBuffer()
	{
		lock (ScreenBufferLock)
		{
			// Scroll entire buffer and destroy contents at Y = 0
			ScreenBuffer.RemoveRange(0, Width);

			// Add a new line at the bottom full of blank characters
			for (int i = 0; i < Width; i++)
				ScreenBuffer.Add(BlankCharacter);

			CursorX = 0;
			CursorY = BufferHeight - 2;
		}
	}
	
	private void LocateCursor()
	{
		bool ScrollDownToFindCursor = CursorY >= ViewY + Height - 1;

		while (!IsCursorInView())
		{
			if (ScrollDownToFindCursor)
				ScrollViewDown();
			else
				ScrollViewUp();
		}
	}
	
	private void AdvanceCursor()
	{
		if (ReachedWindowWidthLimit)
		{
			CursorY++;
			CursorX = 0;
		}
		else
		{
			CursorX++;
		}

		if (ReachedBufferHeightLimit)
		{
			ScrollBuffer();
			return;
		}

		LocateCursor();
	}
	
	private void NextLine()
	{
		CursorX = 0;
		CursorY++;

		if (ReachedBufferHeightLimit)
		{
			ScrollBuffer();
			return;
		}

		LocateCursor();
	}
	
	public void ScrollViewUp()
	{
		ViewY--;

		if (ViewY < 0)
			ViewY = 0;
	}
	
	public void ScrollViewDown()
	{
		if (ViewY == BufferHeight - Height - 1)
			return;

		ViewY++;

		//	while (ViewY + Height >= BufferHeight)
		//		ViewY--;
	}
	
	#region Logging

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

	#endregion
	
	#region Writing API

	public void WriteLine() => Write('\n');

	public void WriteLine(string Text, byte Foreground = 15, byte Background = 0)
	{
		Write(Text, Foreground, Background);
		Write('\n');
	}

	public void WriteLine(char Character, byte Foreground = 15, byte Background = 0)
	{
		Write(Character, Foreground, Background);
		Write('\n');
	}

	public void Write(string Text, byte Foreground = 15, byte Background = 0)
	{
		for (int i = 0; i < Text.Length; i++)
			Write(Text[i], Foreground, Background);
	}

	// Base-level writing function that actually writes the character to the buffer and does newline/carriage-return logic
	public void Write(char Character, byte Foreground = 15, byte Background = 0, bool OmitHistoryEntry = false)
	{
		if (DoLogging && !OmitHistoryEntry)
			LogStreamWriter.Write(Character);

		if (!OmitHistoryEntry)
		{
			ModifyHistory.Add(new()
			{
				Type = TextBoxModifyType.Write,

				CursorX = CursorX,
				CursorY = CursorY,

				Character = Character,

				Foreground = Foreground,
				Background = Background
			});
		}

		OmitCursorHistoryEntries = true;

		ModifyChar(Character, Foreground, Background);

		if (Character == '\r')
			CursorX = 0;
		else if (Character == '\n')
			NextLine();
		else
			AdvanceCursor();

		OmitCursorHistoryEntries = false;

		if (OmitHistoryEntry)
			return;

		
	}

	public void WriteCharInPlace(char Character, byte Foreground = 15, byte Background = 0)
	{
		ModifyChar(Character, Foreground, Background);

		ModifyHistory.Add(new()
		{
			Type = TextBoxModifyType.WriteCharInPlace,

			CursorX = CursorX,
			CursorY = CursorY,

			Character = Character,

			Foreground = Foreground,
			Background = Background
		});
	}

	private void ModifyChar(char Character, byte Foreground = 15, byte Background = 0)
	{
		try
		{
			lock (ScreenBufferLock)
			{
				var CharInfo = ScreenBuffer[IndexUnderCursor];
				
				CharInfo.Character = Character;
				CharInfo.Foreground = Foreground;
				CharInfo.Background = Background;
				
				ScreenBuffer[IndexUnderCursor] = CharInfo;
			}
		}
		catch (Exception)
		{
			throw new Exception($"IndexUnderCursor: {IndexUnderCursor}, Character: '{Character}', ScreenBuffer Count: {ScreenBuffer.Count}\nScreenBufferSize: {ScreenBufferSize}");
		}
	}

	#endregion
	
	public void CursorUp()
	{
		if (CursorY > 0)
			CursorY--;
	}
	
	public void CursorDown()
	{
		if (CursorY < BufferHeight - 1) 
			CursorY++;
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
}
