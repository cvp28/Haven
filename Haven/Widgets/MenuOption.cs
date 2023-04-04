

namespace Haven;

public class MenuOption
{
	public string ID { get; set; }
	public string Text { get; set; }

	public ConsoleColor TextForeground { get; set; }
	public ConsoleColor TextBackground { get; set; }

	public Action Action { get; set; }
}