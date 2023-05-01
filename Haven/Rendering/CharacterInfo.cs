
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
				case '\t':
					return ' ';

				default:
					return Character;
			}
		}
	}

	public byte Foreground;
	public byte Background;

	public CharacterInfo()
	{
		Character = ' ';
		Foreground = ConsoleColor.White.ToByte();
		Background = ConsoleColor.Black.ToByte();
	}
}
