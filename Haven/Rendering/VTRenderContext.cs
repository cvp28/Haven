using System.Text;

namespace Haven;

public class VTRenderContext
{
	public static byte CurrentForegroundColor { get; internal set; }
	public static byte CurrentBackgroundColor { get; internal set; }

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

	private StringBuilder Buffer;

	public VTRenderContext()
	{
		// Initialize private members
		Buffer = new();

		// Initialize public members
		CurrentForegroundColor = VTColor.White;
		CurrentBackgroundColor = VTColor.Black;
	}

	public void Clear()
	{
		Buffer.Clear();
	}

	public void VTSetCursorPosition(int X, int Y)
	{
		Buffer.Append("\x1b[");
		Buffer.Append(Math.Abs(Y + 1));
		Buffer.Append(';');
		Buffer.Append(Math.Abs(X + 1));
		Buffer.Append('H');
	}

	public void VTClearLine()
	{
		Buffer.Append("\x1b[2K");
	}

	public void VTDrawText(string Text)
	{
		Buffer.Append(Text);
	}

	public void VTDrawChar(char Character)
	{
		Buffer.Append(Character);
	}

	public void VTDrawBox(int X, int Y, int Width, int Height)
	{
		VTSetCursorPosition(X, Y);

		// Top line
		Buffer.Append(BoxChars.TopLeft);

		for (int i = 0; i < Width - 2; i++)
			Buffer.Append(BoxChars.Horizontal);

		Buffer.Append(BoxChars.TopRight);

		// Left and right lines
		for (int i = 1; i < Height - 1; i++)
		{
			VTSetCursorPosition(X, Y + i);
			VTDrawChar(BoxChars.Vertical);
			VTSetCursorPosition(X + Width - 1, Y + i);
			VTDrawChar(BoxChars.Vertical);
		}

		VTSetCursorPosition(X, Y + Height - 1);

		// Bottom line
		Buffer.Append(BoxChars.BottomLeft);

		for (int i = 0; i < Width - 2; i++)
			Buffer.Append(BoxChars.Horizontal);

		Buffer.Append(BoxChars.BottomRight);
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

		Buffer.Append("\x1b[38;5;");
		Buffer.Append(Color);
		Buffer.Append('m');

		return true;
	}

	public bool VTSetBackgroundColor(byte Color)
	{
		if (Color == CurrentBackgroundColor)
			return false;

		CurrentBackgroundColor = Color;

		Buffer.Append("\x1b[48;5;");
		Buffer.Append(Color);
		Buffer.Append('m');

		return true;
	}

	public bool VTSetColors(byte Foreground, byte Background) => VTSetForegroundColor(Foreground) || VTSetBackgroundColor(Background);

	public void VTEnterColorContext(byte Foreground, byte Background, Action ContextAction)
	{
		bool ColorsChanged = VTSetColors(Foreground, Background);

		ContextAction();

		if (ColorsChanged)
			VTResetColors();
	}

	public void VTInvert() => Buffer.Append("\x1b[7m");
	public void VTRevert() => Buffer.Append("\x1b[27m");

	public void VTResetColors()
	{
		CurrentForegroundColor = ConsoleColor.White.ToByte();
		CurrentBackgroundColor = ConsoleColor.Black.ToByte();
		Buffer.Append("\x1b[97m\x1b[40m");
	}

	public string GetBuffer() => Buffer.ToString();
}
