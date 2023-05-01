

namespace Haven;

public class MenuOption
{
	public string ID { get; set; }
	public string Text { get; set; }

	public byte TextForeground { get; set; }
	public byte TextBackground { get; set; }

	public Action Action { get; set; }
}