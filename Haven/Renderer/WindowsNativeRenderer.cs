
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Haven;

public class WindowsNativeRenderer : Renderer
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

	private Stopwatch RenderTimer;

	private readonly int ScreenBufferSize = Console.LargestWindowWidth * Console.LargestWindowHeight;

	public WindowsNativeRenderer()
	{
		// Acquire handle to standard out
		ConOutHnd = Kernel32.GetStdHandle(-11);

		RenderTimer = new();

		WindowWidthCells = Console.WindowWidth;
		WindowHeightCells = Console.WindowHeight;

		BufferCoordPosition = new(0, 0);
		BufferCoordSize = new((short) Console.WindowWidth, (short) Console.WindowHeight);
		WriteRegion = new() { Top = 0, Left = 0, Right = (short) Console.WindowWidth, Bottom = (short) Console.WindowHeight };

		TotalCells = WindowWidthCells * WindowHeightCells;

		ScreenBuffer = new CharInfo[ScreenBufferSize];
		ClearBuffer = new CharInfo[ScreenBufferSize];

		for (int i = 0; i < ScreenBufferSize; i++)
		{
			// White FG on Black BG by default
			ClearBuffer[i].Attributes = (short) ConsoleColor.White | ((short) ConsoleColor.Black << 4);
			ClearBuffer[i].Char.UnicodeChar = ' ';
		}

		Clear();
	}

	public override void Render(IEnumerable<Widget> Widgets)
	{
		Clear();
		Console.CursorVisible = false;

		RenderTimer.Restart();

		if (Widgets is not null)
			foreach (Widget Widget in Widgets)
				if (Widget.Visible)
					Widget.Draw(this);

		WidgetRenderTimeMs = (int) RenderTimer.ElapsedMilliseconds;
		RenderTimer.Restart();

		Kernel32.WriteConsoleOutputW(ConOutHnd, ScreenBuffer, BufferCoordSize, BufferCoordPosition, ref WriteRegion);

		StdoutWriteTimeMs = (int) RenderTimer.ElapsedMilliseconds;
	}

	public override void UpdateScreenDimensions()
	{
		WindowWidthCells = Console.WindowWidth;
		WindowHeightCells = Console.WindowHeight;

		TotalCells = WindowWidthCells * WindowHeightCells;

		BufferCoordSize.X = (short) WindowWidthCells;
		BufferCoordSize.Y = (short) WindowHeightCells;

		WriteRegion.Right = (short) WindowWidthCells;
		WriteRegion.Bottom = (short) WindowHeightCells;

		//Array.Resize(ref ClearBuffer, TotalCells);
		//Array.Resize(ref ScreenBuffer, TotalCells);
	}

	private int IX(int X, int Y)
	{
		if (TotalCells == 0)
			return 0;

		int Cell = X + WindowWidthCells * Y;

		if (Cell >= TotalCells)
			return Cell % TotalCells;
		else
			return Cell;
	}

	public override void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(X, Y);
		AddColorsAt(CellIndex, Foreground, Background);
	}

	private void AddColorsAt(int CellIndex, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellAttribute = (byte)Foreground | ((byte)Background << 4);

		ScreenBuffer[CellIndex].Attributes = (short)CellAttribute;
	}

	public override void CopyToBuffer2D(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, ref CharacterInfo[] Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < ViewWidth * ViewHeight; i++)
		{
			int CellIndex = IX(X + OffX, Y + OffY);

			ModifyCharAt(CellIndex, Buffer[OffX + BufferWidth * OffY].Character);
			ScreenBuffer[CellIndex].Attributes = Buffer[OffX + BufferWidth * OffY].ColorsToShort();

			//	ModifyForegroundAt(CellIndex, Buffer[i].Foreground);
			//	ModifyBackgroundAt(CellIndex, Buffer[i].Background);

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

	public override void WriteStringAt(int X, int Y, string Text)
	{
		for (int i = 0; i < Text.Length; i++)
		{
			int CellIndex = IX(X + i, Y);
			ModifyCharAt(CellIndex, Text[i]);
		}
	}

	public override void WriteColorStringAt(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background)
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
