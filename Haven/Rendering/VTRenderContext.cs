using System.Runtime.InteropServices;
using System.Text;

namespace HavenUI;

public class VTRenderContext
{
	public static byte CurrentForegroundColor { get; internal set; }
	public static byte CurrentBackgroundColor { get; internal set; }

	public static int CurrentConsoleX { get; internal set; }
	public static int CurrentConsoleY { get; internal set; }

	/// <summary>
	/// <para>
	/// Enables whitespace optimizations when VTDrawCharacterInfoBuffer is called by a widget to draw its own buffer to the screen
	/// </para>
	/// 
	/// <para>
	/// Note: When enabled, any widgets placed behind any other widget using VTDrawCharacterInfoBuffer (ie. ScrollableTextBox) will be visible
	/// inside the bounds of the textbox when you might normally expect them not to be.
	/// </para>
	/// 
	/// <para>
	/// This is because, when optimizations are enabled, the function optimizes away any whitespace characters inside of the drawn CharacterInfo buffer to save on frame size for lower frame times.
	/// </para>
	/// 
	/// <para>
	/// As such, these optimizations should only be enabled when YOU are sure that widgets will never be placed behind ScrollableTextBox or any other widget using VTDrawCharacterInfoBuffer.
	/// Yes, that is kind of a technical expectation. However, that is why this is false by default. :)
	/// </para>
	/// </summary>
	public static bool OptimizeCharaterInfoBufferDraws { get; set; } = false;

	internal StringBuilder FrameBuffer;
	private StringBuilder LastFrameBuffer;

	public VTRenderContext()
	{
		// Initialize private members
		FrameBuffer = new(1000);
		LastFrameBuffer = new(1000);

		// Initialize public members
		CurrentForegroundColor = VTColor.White;
		CurrentBackgroundColor = VTColor.Black;
	}

	public void VTSetCursorPosition(int X, int Y)
	{
		FrameBuffer.Append("\x1b[");
		FrameBuffer.Append(Math.Abs(Y + 1));
		FrameBuffer.Append(';');
		FrameBuffer.Append(Math.Abs(X + 1));
		FrameBuffer.Append('H');
	}

	public void VTClearLine()
	{
		FrameBuffer.Append("\x1b[2K");
	}

	public void VTDrawText(string Text)
	{
		foreach (var c in Text)
			VTDrawChar(c);
	}

	public void VTDrawChar(char Character)
	{
		FrameBuffer.Append(Character);
	}

	public void VTDrawBox(int X, int Y, int Width, int Height)
	{
		VTSetCursorPosition(X, Y);

		// Top line
		FrameBuffer.Append(BoxChars.TopLeft);

		for (int i = 0; i < Width - 2; i++)
			FrameBuffer.Append(BoxChars.Horizontal);

		FrameBuffer.Append(BoxChars.TopRight);

		// Left and right lines
		for (int i = 1; i < Height - 1; i++)
		{
			VTSetCursorPosition(X, Y + i);
			VTDrawChar(BoxChars.Vertical);
			VTCursorForward(Width - 2);
			//VTSetCursorPosition(X + Width - 1, Y + i);
			VTDrawChar(BoxChars.Vertical);
		}

		VTSetCursorPosition(X, Y + Height - 1);

		// Bottom line
		FrameBuffer.Append(BoxChars.BottomLeft);

		for (int i = 0; i < Width - 2; i++)
			FrameBuffer.Append(BoxChars.Horizontal);

		FrameBuffer.Append(BoxChars.BottomRight);
	}

	public void VTCursorForward(int Count = 1)
	{
		FrameBuffer.Append("\x1b[");
		FrameBuffer.Append(Count);
		FrameBuffer.Append('C');
	}

	private List<CharacterInfo> TempLineBuffer = new();
	private bool OptimizeCurrentLine = true;

	public void VTDrawCharacterInfoBuffer(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, in Span<CharacterInfo> Buffer)
	{
		VTSetCursorPosition(X, Y);

		int OffX = 0;
		int OffY = 0;

		OptimizeCurrentLine = true;

		for (int i = 0; i < ViewWidth * ViewHeight; i++)
		{
			var CurrentCharInfo = Buffer[OffX + BufferWidth * OffY];

			// Add the current character to a temporary buffer
			TempLineBuffer.Add(CurrentCharInfo);

			// We will optimize away the current line so long as every character in the line is whitespace and optimizations are enabled
			// The moment we come across a character that is NOT whitespace, this flag will become false and we will draw the line at its end
			OptimizeCurrentLine &= char.IsWhiteSpace(CurrentCharInfo.RenderingCharacter) && OptimizeCharaterInfoBufferDraws;

			if (OffX == ViewWidth - 1)
			{
				OffX = 0;
				OffY++;

				// If the current line cannot be optimized using our current method, draw the line
				if (!OptimizeCurrentLine)
				{
					foreach (var CharInfo in TempLineBuffer)
					{
						VTSetForegroundColor(CharInfo.Foreground);
						VTSetBackgroundColor(CharInfo.Background);
						VTDrawChar(CharInfo.RenderingCharacter);
					}
				}

				// Reset line tracking parameters
				OptimizeCurrentLine = true;
				TempLineBuffer.Clear();

				// Set new cursor position
				VTSetCursorPosition(X + OffX, Y + OffY);
			}
			else
			{
				OffX++;
			}
		}
	}

	public bool VTSetForegroundColor(byte Color)
	{
		// If we have already set the terminal to this color, optimize away this call by returning prematurely
		if (Color == CurrentForegroundColor)
			return false;

		CurrentForegroundColor = Color;

		FrameBuffer.Append("\x1b[");
		FrameBuffer.Append("38;5;");
		FrameBuffer.Append(Color);
		FrameBuffer.Append('m');

		return true;
	}

	public bool VTSetBackgroundColor(byte Color)
	{
		if (Color == CurrentBackgroundColor)
			return false;

		CurrentBackgroundColor = Color;

		FrameBuffer.Append("\x1b[");
		FrameBuffer.Append("48;5;");
		FrameBuffer.Append(Color);
		FrameBuffer.Append('m');

		return true;
	}

	public bool VTSetColors(byte Foreground, byte Background) => VTSetBackgroundColor(Background) || VTSetForegroundColor(Foreground);

	public void VTEnterColorContext(byte Foreground, byte Background, Action ContextAction)
	{
		bool ColorsChanged = VTSetColors(Foreground, Background);

		ContextAction();

		if (ColorsChanged)
			VTResetColors();
	}

	public void VTInvert() => FrameBuffer.Append("\x1b[7m");
	public void VTRevert() => FrameBuffer.Append("\x1b[27m");

	public void VTResetColors()
	{
		CurrentForegroundColor = VTColor.White;
		CurrentBackgroundColor = VTColor.Black;
		FrameBuffer.Append("\x1b[97m\x1b[40m");
	}

	public string GetCurrentFrameBuffer() => FrameBuffer.ToString();
	public string GetLastFrameBuffer() => LastFrameBuffer.ToString();

	public void SetCurrentFrameBuffer(string Contents)
	{
		ClearCurrentFrame();
		FrameBuffer.Append(Contents);
	}

	// Stores the current frame for later reference
	public void StoreCurrentFrame()
	{
		LastFrameBuffer.Clear();
		LastFrameBuffer.Append(FrameBuffer);
	}

	public void ClearCurrentFrame()
	{
		FrameBuffer.Clear();
	}

	// Indicates if this context should be rendered because the current frame is different from the last frame
	//public bool ShouldRender => string.CompareOrdinal(FrameBuffer.ToString(), LastFrameBuffer.ToString()) != 0;

	public bool ShouldRender => !FrameBuffer.Equals(LastFrameBuffer);
}