
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Haven;

/*

ConWriteRenderer character entries will be 12 chars

5 chars - FG
6 chars - BG
1 char  - character

*/

public class ConWriteRenderer : Renderer
{
	private int TotalCells;
	private int TotalChars;
	private char[] ScreenBuffer;
	private char[] ClearScreen;

	private int WindowWidth;
	private int WindowHeight;

	private Stopwatch sw;

	public Stream stdout;

	private readonly int ScreenBufferSize = Console.LargestWindowWidth * Console.LargestWindowHeight * 12;

	public ConWriteRenderer()
	{
		stdout = Console.OpenStandardOutput();

		WindowWidth = Console.WindowWidth;
		WindowHeight = Console.WindowHeight;

		TotalCells = WindowWidth * WindowHeight;
		TotalChars = TotalCells * 12;

		CharSequences.Init();
		Console.OutputEncoding = Encoding.UTF8;

		ScreenBuffer = new char[ScreenBufferSize];
		ClearScreen = new char[ScreenBufferSize];

		for (int i = 0; i < Console.LargestWindowWidth * Console.LargestWindowHeight; i++)
		{
			ModifyForegroundAt(i, CharSequences.FgBrightWhite);
			ModifyBackgroundAt(i, CharSequences.BgBlack);
			ModifyCharAt(i, ' ');
		}

		Array.Copy(ScreenBuffer, ClearScreen, ScreenBufferSize);

		sw = new();
	}

	private void ModifyForegroundAt(int CellIndex, char[] ForegroundSequence) => Array.Copy(ForegroundSequence, 0, ScreenBuffer, CellIndex * 12, ForegroundSequence.Length);
	private void ModifyBackgroundAt(int CellIndex, char[] BackgroundSequence) => Array.Copy(BackgroundSequence, 0, ScreenBuffer, CellIndex * 12 + 5, BackgroundSequence.Length);
	private void ModifyCharAt(int CellIndex, char c) => ScreenBuffer[CellIndex * 12 + 11] = c;
	private void ClearBuffer() => Array.Copy(ClearScreen, ScreenBuffer, ScreenBufferSize);

	public override void Render(IEnumerable<Widget> Widgets)
	{
		Console.CursorVisible = false;

		Console.SetCursorPosition(0, 0);
		ClearBuffer();

		// Render widgets
		sw.Restart();

		if (Widgets is not null)
			foreach (Widget w in Widgets)
				if (w.Visible)
					w.Draw(this);

		WidgetRenderTimeMs = (int) sw.ElapsedMilliseconds;

		sw.Restart();

		ReadOnlySpan<byte> temp = Encoding.UTF8.GetBytes(ScreenBuffer, 0, TotalChars);

		DiagTime1Ms = (int)sw.ElapsedMilliseconds;

		// Write to stdout
		sw.Restart();

		stdout.Write(temp);

		StdoutWriteTimeMs = (int) sw.ElapsedMilliseconds;
	}

	public override void UpdateScreenDimensions()
	{
		WindowWidth = Console.WindowWidth;
		WindowHeight = Console.WindowHeight;

		TotalCells = WindowWidth * WindowHeight;
		TotalChars = TotalCells * 12;

		//Array.Resize(ref ClearScreen, TotalChars);
		//Array.Resize(ref ScreenBuffer, TotalChars);
	}

	private int IX(int X, int Y)
	{
		int Cell = Y * WindowWidth + X;

		if (Cell >= TotalCells)
			return Math.Abs(Cell % TotalCells);
		else
			return Math.Abs(Cell);
	}

	public override void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background)
	{
		int CellIndex = IX(X, Y);

		ModifyForegroundAt(CellIndex, CharSequences.ToFgCharArray(Foreground));
		ModifyBackgroundAt(CellIndex, CharSequences.ToBgCharArray(Background));
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

			ModifyCharAt(CellIndex, Text[i]);

			ModifyForegroundAt(CellIndex, CharSequences.ToFgCharArray(Foreground));
			ModifyBackgroundAt(CellIndex, CharSequences.ToBgCharArray(Background));
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

		ModifyForegroundAt(TopLeftIndex, CharSequences.FgBrightWhite);
		ModifyForegroundAt(TopRightIndex, CharSequences.FgBrightWhite);
		ModifyForegroundAt(BottomLeftIndex, CharSequences.FgBrightWhite);
		ModifyForegroundAt(BottomRightIndex, CharSequences.FgBrightWhite);

		ModifyBackgroundAt(TopLeftIndex, CharSequences.BgBlack);
		ModifyBackgroundAt(TopRightIndex, CharSequences.BgBlack);
		ModifyBackgroundAt(BottomLeftIndex, CharSequences.BgBlack);
		ModifyBackgroundAt(BottomRightIndex, CharSequences.BgBlack);

		// Draw top and bottom
		for (int i = 1; i <= Width - 2; i++)
		{
			int TopIndex = IX(X + i, Y);
			int BottomIndex = IX(X + i, Y + Height - 1);

			ModifyCharAt(TopIndex, BoxChars.Horizontal);
			ModifyCharAt(BottomIndex, BoxChars.Horizontal);

			ModifyForegroundAt(TopIndex, CharSequences.FgBrightWhite);
			ModifyBackgroundAt(TopIndex, CharSequences.BgBlack);

			ModifyForegroundAt(BottomIndex, CharSequences.FgBrightWhite);
			ModifyBackgroundAt(BottomIndex, CharSequences.BgBlack);
		}

		// Draw left and right
		for (int i = 1; i <= Height - 2; i++)
		{
			int LeftIndex = IX(X, Y + i);
			int RightIndex = IX(X + Width - 1, Y + i);

			ModifyCharAt(LeftIndex, BoxChars.Vertical);
			ModifyCharAt(RightIndex, BoxChars.Vertical);

			ModifyForegroundAt(LeftIndex, CharSequences.FgBrightWhite);
			ModifyBackgroundAt(LeftIndex, CharSequences.BgBlack);

			ModifyForegroundAt(RightIndex, CharSequences.FgBrightWhite);
			ModifyBackgroundAt(RightIndex, CharSequences.BgBlack);
		}
	}

	public override void CopyToBuffer2D(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, ref CharacterInfo[] Buffer)
	{
		int OffX = 0;
		int OffY = 0;

		for (int i = 0; i < ViewWidth * ViewHeight; i++)
		{
			int CellIndex = IX(X + OffX, Y + OffY);

			ModifyCharAt(CellIndex, Buffer[OffX + BufferWidth * OffY].Character);
			ModifyForegroundAt(CellIndex, CharSequences.ToFgCharArray(Buffer[OffX + BufferWidth * OffY].Foreground));
			ModifyBackgroundAt(CellIndex, CharSequences.ToBgCharArray(Buffer[OffX + BufferWidth * OffY].Background));

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
}
