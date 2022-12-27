
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

	/// <summary>
	/// Centers the label according the specified dimensions
	/// </summary>
	/// <param name="d">The dimensions object to center</param>
	/// <param name="XOff">Positive/negative X offset relative to the screen center</param>
	/// <param name="YOff">Positive/negative Y offset relative to the screen center</param>
	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		X = d.HorizontalCenter - (Text.Length / 2) + XOff;
		Y = d.VerticalCenter + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}

	public override void Draw(Renderer s)
	{
		if (Text.Length == 0) { return; }
		
		s.WriteColorStringAt(X, Y, Text, ForegroundColor, BackgroundColor);
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{
		
	}
}
