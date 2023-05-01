
namespace Haven;

internal struct TextBoxModifyAction
{
	public TextBoxModifyType Type;

	// Move Type
	public int CursorX;
	public int CursorY;


	// Write Type
	public char Character;

	public byte Foreground;
	public byte Background;
}

internal enum TextBoxModifyType
{
	MoveCursor,
	WriteCharInPlace,
	Write
}
