
namespace Haven;

public class NativeScreen : IRenderer
{
	private IntPtr ConOutHnd;
	private COORD BufferCoordPosition;
	private COORD BufferCoordSize;
	private SmallRect WriteRegion;

	private int WindowWidthCells;
	private int WindowHeightCells;

	private int TotalCells;

	private CharInfo[] ScreenBuffer;
	private CharInfo[] ClearBuffer;

	public NativeScreen()
	{
		// Acquire handle to standard out
		ConOutHnd = Kernel32.GetStdHandle(-11);

		WindowWidthCells = Console.WindowWidth;
		WindowHeightCells = Console.WindowHeight;

		BufferCoordPosition = new(0, 0);
		BufferCoordSize = new((short) WindowWidthCells, (short) WindowHeightCells);
		WriteRegion = new() { Top = 0, Left = 0, Right = (short) WindowWidthCells, Bottom = (short) WindowHeightCells };

		TotalCells = Console.WindowWidth * Console.WindowHeight;

		ScreenBuffer = new CharInfo[TotalCells];
		ClearBuffer = new CharInfo[TotalCells];

		for (int i = 0; i < TotalCells; i++)
		{
			// White FG on Black BG by default
			ClearBuffer[i].Attributes = (short) ConsoleColor.White | ((short) ConsoleColor.Black << 4);
			ClearBuffer[i].Char.UnicodeChar = ' ';
		}

		Clear();
	}


	public void Render(List<Widget> Widgets)
	{
		Clear();

		if (Widgets is not null)
			foreach (Widget Widget in Widgets)
				if (Widget.Visible)
					Widget.Draw(this);

		Kernel32.WriteConsoleOutputW(ConOutHnd, ScreenBuffer, BufferCoordSize, BufferCoordPosition, ref WriteRegion);
	}

	private int IX(int X, int Y)
	{
		int Cell = X + WindowWidthCells * Y;

		if (Cell > TotalCells)
			return Cell % TotalCells;
		else
			return Cell;
	}

	public void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(X, Y);
		int CellAttribute = (byte) Foreground | ((byte) Background << 4);

		ScreenBuffer[CellIndex].Attributes = (short) CellAttribute;
	}

	private void AddColorsAt(int CellIndex, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellAttribute = (byte)Foreground | ((byte)Background << 4);

		ScreenBuffer[CellIndex].Attributes = (short)CellAttribute;
	}

	public void CopyToBuffer2D(int X, int Y, int LineWidth, CharacterInfo[] Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < Buffer.Length; i++)
		{
			int CellIndex = IX(X + OffX, Y + OffY);

			ModifyCharAt(CellIndex, Buffer[i].Character);
			ScreenBuffer[CellIndex].Attributes = Buffer[i].ColorsToShort();

			//	ModifyForegroundAt(CellIndex, Buffer[i].Foreground);
			//	ModifyBackgroundAt(CellIndex, Buffer[i].Background);

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

	public void DrawBox(int X, int Y, int Width, int Height)
	{
		int TopLeftIndex = IX(X, Y);
		int TopRightIndex = IX(X + Width - 1, Y);
		int BottomLeftIndex = IX(X, Y + Height - 1);
		int BottomRightIndex = IX(X + Width - 1, Y + Height - 1);

		ModifyCharAt(TopLeftIndex, BoxChars.TopLeft);
		ModifyCharAt(TopRightIndex, BoxChars.TopRight);
		ModifyCharAt(BottomLeftIndex, BoxChars.BottomLeft);
		ModifyCharAt(BottomRightIndex, BoxChars.BottomRight);

		AddColorsAt(TopLeftIndex, ConsoleColor.White, ConsoleColor.Black);
		AddColorsAt(TopRightIndex, ConsoleColor.White, ConsoleColor.Black);
		AddColorsAt(BottomLeftIndex, ConsoleColor.White, ConsoleColor.Black);
		AddColorsAt(BottomRightIndex, ConsoleColor.White, ConsoleColor.Black);

		//	ModifyCharAt(CellIndex, BoxChars.TopLeft);
		//	ModifyCharAt(CellIndex + Width - 1, BoxChars.TopRight);
		//	ModifyCharAt(CellIndex + (WindowWidthCells * (Height - 1)), BoxChars.BottomLeft);
		//	ModifyCharAt(CellIndex + (WindowWidthCells * (Height - 1)) + Width - 1, BoxChars.BottomRight);

		for (int i = 1; i <= Width - 2; i++)
		{
			int WindowTopIndex = IX(X + i, Y);
			int WindowBottomIndex = IX(X + i, Y + Height - 1);

			ModifyCharAt(WindowTopIndex, BoxChars.Horizontal);
			ModifyCharAt(WindowBottomIndex, BoxChars.Horizontal);

			AddColorsAt(WindowTopIndex, ConsoleColor.White, ConsoleColor.Black);
			AddColorsAt(WindowBottomIndex, ConsoleColor.White, ConsoleColor.Black);

			//	ModifyCharAt(CellIndex + i, BoxChars.Horizontal);
			//	ModifyCharAt(CellIndex + (WindowWidthCells * (Height - 1) + i), BoxChars.Horizontal);
		}

		for (int i = 1; i <= Height - 2; i++)
		{
			int WindowLeftIndex = IX(X, Y + i);
			int WindowRightIndex = IX(X + Width - 1, Y + i);

			ModifyCharAt(WindowLeftIndex, BoxChars.Vertical);
			ModifyCharAt(WindowRightIndex, BoxChars.Vertical);

			AddColorsAt(WindowLeftIndex, ConsoleColor.White, ConsoleColor.Black);
			AddColorsAt(WindowRightIndex, ConsoleColor.White, ConsoleColor.Black);

			//	ModifyCharAt(CellIndex + (WindowWidthCells * i), BoxChars.Vertical);
			//	ModifyCharAt(CellIndex + (WindowWidthCells * i) + Width - 1, BoxChars.Vertical);
		}
	}

	public void WriteStringAt(int X, int Y, string Text)
	{
		for (int i = 0; i < Text.Length; i++)
		{
			int CellIndex = IX(X + i, Y);
			ModifyCharAt(CellIndex, Text[i]);
		}
	}

	public void WriteColorStringAt(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background)
	{
		for (int i = 0; i < Text.Length; i++)
		{
			int CellIndex = IX(X + i, Y);

			int CellAttribute = (byte) Foreground | ((byte) Background << 4);
			ScreenBuffer[CellIndex].Attributes = (short)CellAttribute;
			ModifyCharAt(CellIndex, Text[i]);
		}
	}

	private void Clear()
	{
		Array.Copy(ClearBuffer, ScreenBuffer, TotalCells);
	}

	private void ModifyCharAt(int CellIndex, char Character)
	{
		ScreenBuffer[CellIndex].Char.UnicodeChar = Character;
	}

	private void ModifyForegroundAt(int CellIndex, ConsoleColor Foreground)
	{
		byte NewForeground = (byte) Foreground;
		byte NewAttribute = (byte) ScreenBuffer[CellIndex].Attributes;

		// Contains current background color
		NewAttribute &= 0xF0;

		// New foreground color resides in lower 4 bits
		NewAttribute |= NewForeground;

		ScreenBuffer[CellIndex].Attributes = NewAttribute;
	}

	private void ModifyBackgroundAt(int CellIndex, ConsoleColor Background)
	{
		byte NewBackground = (byte) Background;
		byte NewAttribute = (byte) ScreenBuffer[CellIndex].Attributes;

		// Contains current foreground color
		NewAttribute &= 0x0F;

		// New background color resides in upper 4 bits
		NewAttribute |= NewBackground;

		ScreenBuffer[CellIndex].Attributes = NewAttribute;
	}
}
