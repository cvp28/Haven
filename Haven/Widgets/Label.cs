
namespace Haven;

public class Label : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public string Text { get; set; }

	public ConsoleColor ForegroundColor;
	public ConsoleColor BackgroundColor;

	public Label(int X, int Y) : base()
	{
		this.X = X;
		this.Y = Y;
		Text = string.Empty;
		ForegroundColor = ConsoleColor.White;
		BackgroundColor = ConsoleColor.Black;
	}

	public Label(int X, int Y, string Text) : base()
	{
		this.X = X;
		this.Y = Y;
		this.Text = Text;
		ForegroundColor = ConsoleColor.White;
		BackgroundColor = ConsoleColor.Black;
	}

	public Label(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background) : base()
	{
		this.X = X;
		this.Y = Y;
		this.Text = Text;
		ForegroundColor = Foreground;
		BackgroundColor = Background;
	}

	public override void Draw(IRenderer s)
	{
		if (Text.Length == 0) { return; }
		
		s.WriteColorStringAt(X, Y, Text, ForegroundColor, BackgroundColor);
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{
		
	}
}
