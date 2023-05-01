
namespace Haven;

public class Token
{
	public string Content;

	public int StartIndex;

	public bool Selected = false;
	public bool Quoted = false;

	public byte HighlightForeground;
	public byte HighlightBackground;

}
