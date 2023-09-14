namespace HavenUI;

public class BulletListState
{
	public int X { get; set; }
	public int Y { get; set; }

	internal Dictionary<string, BulletItem> Children = new();
	
	public byte ListBackground { get; set; } = VTColor.Black;
	public int TabWidth { get; set; } = 2;
	
	public BulletItem this[string ID]
	{
		get => Children.ContainsKey(ID) ? Children[ID] : null;
	}
	
	public bool AddChild(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		if (Children.ContainsKey(ID))
			return false;

		Children.Add(ID, new BulletItem(Text, BulletPointForeground, BulletTextForeground));
		return true;
	}
}

public class BulletItem
{
	public byte BulletPointForeground = VTColor.White;
	public byte BulletTextForeground = VTColor.White;
	public string Text { get; set; }

	private Dictionary<string, BulletItem> Children = new();

	public BulletItem this[string ID]
	{
		get => Children.ContainsKey(ID) ? Children[ID] : null;
	}
	
	public BulletItem(string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		this.Text = Text;
		this.BulletPointForeground = BulletPointForeground;
		this.BulletTextForeground = BulletTextForeground;
	}

	public bool AddChild(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
	{
		if (Children.ContainsKey(ID))
			return false;

		Children.Add(ID, new BulletItem(Text, BulletPointForeground, BulletTextForeground));
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
			YOffset += Children.Values.ElementAt(i).Render(X + TabWidth, Y + i + 1 + YOffset, RenderContext, ListBackground, TabWidth);

		// Return the number of children as a Y Offset so the next renderable can be offset by the appropriate amount
		return Children.Count + YOffset;
	}
}