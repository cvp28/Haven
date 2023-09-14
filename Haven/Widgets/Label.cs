
namespace HavenUI;

public class Label : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public string Text { get; set; }

	public byte ForegroundColor { get; set; }
	public byte BackgroundColor { get; set; }

	public Label(int X, int Y) : this(X, Y, string.Empty)
	{ }

	public Label(int X, int Y, string Text) : this(X, Y, Text, VTColor.White, VTColor.Black)
	{ }

	public Label(int X, int Y, string Text, byte Foreground, byte Background) : base()
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

	public override void CalculateBoundaries()
	{
		Bounds.X = X;
		Bounds.Y = Y;

		Bounds.Width = Text.Length;
		Bounds.Height = 1;
	}

	public override void Draw()
	{
		if (Text.Length == 0) { return; }

		// Set cursor position then color data
		RenderContext.VTSetCursorPosition(X, Y);

		// The color context will determine automatically if the colors need to be reset after the code finishes

		bool DoReset = RenderContext.VTSetBackgroundColor(BackgroundColor);
		DoReset |= RenderContext.VTSetForegroundColor(ForegroundColor);

		RenderContext.VTDrawText(Text);

		if (DoReset)
			RenderContext.VTResetColors();

		//	RenderContext.VTEnterColorContext(ForegroundColor, BackgroundColor, delegate ()
		//	{
		//		RenderContext.VTDrawText(Text);
		//	});

		//RenderContext.VTDrawBox(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{ }
}
