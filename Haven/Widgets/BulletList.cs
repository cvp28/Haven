
namespace HavenUI;

//	public class BulletList : Widget
//	{
//		public int X { get; set; }
//		public int Y { get; set; }
//	
//		private List<BulletItem> Children;
//	
//		public byte ListBackground { get; set; }
//		public int TabWidth { get; set; }
//	
//		public BulletItem this[string ID]
//		{
//			get => Children.FirstOrDefault(child => child.ID == ID);
//		}
//	
//		public BulletList(int X, int Y, byte ListBackground = VTColor.Black, int TabWidth = 2)
//		{
//			this.X = X;
//			this.Y = Y;
//	
//			Children = new();
//	
//			this.ListBackground = ListBackground;
//			this.TabWidth = TabWidth;
//		}
//	
//		public override void CalculateBoundaries()
//		{
//			
//		}
//	
//		public override void Draw()
//		{
//			int YOffset = 0;
//	
//			for (int i = 0; i < Children.Count; i++)
//				YOffset += Children[i].Render(X, Y + i + YOffset, RenderContext, ListBackground, TabWidth);
//		}
//	
//		public override void OnConsoleKey(ConsoleKeyInfo cki)
//		{ }
//	
//		public bool AddChild(string ID, string Text, byte BulletPointForeground = VTColor.White, byte BulletTextForeground = VTColor.White)
//		{
//			if (Children.Any(child => child.ID == ID))
//				return false;
//	
//			Children.Add(new BulletItem(ID, Text, BulletPointForeground, BulletTextForeground));
//			return true;
//		}
//	
//		public void Clear() => Children.Clear();
//	}