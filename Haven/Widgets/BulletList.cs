
namespace Haven;

public class BulletList : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	private List<BulletItem> Children;

	public byte ListBackground { get; set; }
	public int TabWidth { get; set; }

	public BulletItem this[string ID]
	{
		get => Children.FirstOrDefault(child => child.ID == ID);
	}

	public BulletList(int X, int Y, byte ListBackground = VTColor.Black, int TabWidth = 2)
	{
		this.X = X;
		this.Y = Y;

		Children = new();

		this.ListBackground = ListBackground;
		this.TabWidth = TabWidth;
	}

	public override void Draw()
	{
		int YOffset = 0;

		for (int i = 0; i < Children.Count; i++)
			YOffset += Children[i].Render(X, Y + i + YOffset, RenderContext, ListBackground, TabWidth);
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{ }

	public bool AddChild(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		if (Children.Any(child => child.ID == ID))
			return false;

		Children.Add(new BulletItem(ID, Text, BulletPointForeground, BulletTextForeground));
		return true;
	}

	public void Clear() => Children.Clear();
}

public class BulletItem
{
	public string ID { get; set; }
	public byte BulletPointForeground = VTColor.White;
	public byte BulletTextForeground = VTColor.White;
	public string Text { get; set; }

	private List<BulletItem> Children;

	public BulletItem this[string ID]
	{
		get => Children.FirstOrDefault(child => child.ID == ID);
	}

	public BulletItem(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		Children = new();

		this.ID = ID;
		this.Text = Text;
		this.BulletPointForeground = BulletPointForeground;
		this.BulletTextForeground = BulletTextForeground;
	}

	public bool AddChild(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		if (Children.Any(child => child.ID == ID))
			return false;

		Children.Add(new BulletItem(ID, Text, BulletPointForeground, BulletTextForeground));
		return true;
	}

	internal int Render(int X, int Y, VTRenderContext RenderContext, byte ListBackground, int TabWidth)
	{
		// First, render this item

		RenderContext.VTSetCursorPosition(X, Y);
		RenderContext.VTSetBackgroundColor(ListBackground);

		RenderContext.VTSetForegroundColor(BulletPointForeground);
		RenderContext.VTDrawChar('■');
		RenderContext.VTDrawChar(' ');
		RenderContext.VTSetForegroundColor(BulletTextForeground);
		RenderContext.VTDrawText(Text);

		//	s.WriteColorStringAt(X, Y, "■", BulletPointForeground, ListBackground);			// Write bullet point
		//	s.WriteColorStringAt(X + 2, Y, Text, BulletTextForeground, ListBackground);     // Write bullet text

		int YOffset = 0;

		for (int i = 0; i < Children.Count; i++)
			YOffset += Children[i].Render(X + TabWidth, Y + i + 1 + YOffset, RenderContext, ListBackground, TabWidth);

		// Return the number of children as a Y Offset so the next renderable can be offset by the appropriate amount
		return Children.Count + YOffset;
	}
}
