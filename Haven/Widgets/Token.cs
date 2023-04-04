
namespace Haven;

public class Token
{
	public string Content;

	public int StartIndex;

	public bool Selected = false;
	public bool Quoted = false;

	public ConsoleColor HighlightForeground = ConsoleColor.White;
	public ConsoleColor HighlightBackground = ConsoleColor.Black;

}
