

namespace HavenUI;

public class MenuOption
{
	public int Index { get; internal set; }
	public string Text { get; set; }

	public byte TextForeground { get; set; }
	public byte TextBackground { get; set; }

	public Action Action { get; set; }
}