
namespace Haven;

internal struct TextBoxModifyAction
{
	public TextBoxModifyType Type;

	// Move Type
	public int CursorX;
	public int CursorY;


	// Write Type
	public char Character;

	public ConsoleColor Foreground;
	public ConsoleColor Background;
}

internal enum TextBoxModifyType
{
	MoveCursor,
	ModifyChar,
	Write
}
