
namespace Haven;

public struct CharacterInfo
{
	public char Character;

	public char RenderingCharacter
	{
		get
		{
			switch (Character)
			{
				case '\n':
				case '\r':
					return ' ';

				default:
					return Character;
			}
		}
	}

	public ConsoleColor Foreground;
	public ConsoleColor Background;

	public bool SignalAnsiClear;
	public bool DoColors;

	public CharacterInfo()
	{
		Character = ' ';
		Foreground = ConsoleColor.White;
		Background = ConsoleColor.Black;
		SignalAnsiClear = false;
		DoColors = true;
	}

	public short ColorsToShort()
	{
		// Foreground in bottom 4 bits
		// Background in top 4 bits
		int Attributes = (byte) Foreground | ((byte) Background << 4);

		return (short) Attributes;
	}
}
