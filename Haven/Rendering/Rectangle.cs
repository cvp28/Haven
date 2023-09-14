
namespace HavenUI;

public struct Rectangle
{
	public int X { get; set; }
	public int Y { get; set; }

	public int Width { get; set; }
	public int Height { get; set; }

	public uint Area => (uint) (Width * Height);

	public Coord2D TopRight => new Coord2D(X + Width - 1, Y);
	public Coord2D BottomRight => new Coord2D(X + Width - 1, Y + Height - 1);
	public Coord2D BottomLeft => new Coord2D(X, Y + Height - 1);

	public Rectangle() { }

	public Rectangle(int X, int Y, int Width, int Height)
	{
		this.X = X;
		this.Y = Y;
		this.Width = Width;
		this.Height = Height;
	}

	public void TryExpandToCoord(int X, int Y)
	{
		// If this coord is already within the rectangle bounds, return
		if (ContainsCoord(X, Y))
			return;

		// If the coordinate is valid in SCREEN space (not rectangle space), then we're good to continue
		if (Dimensions.CoordIsValid(X, Y))
		{
			bool XIsAdjacentRight = X > TopRight.X;
			bool YIsBelow = Y > BottomRight.Y;

			// Expand X dimension first
			if (XIsAdjacentRight)
				Width = X - this.X;
			else
				this.X = X;

			// Then, expand Y dimension
			if (YIsBelow)
				Height = Y - this.Y;
			else
				this.X = Y;
		}
	}

	public bool Collides(Rectangle Other)
	{
		bool OtherContainsTL = Other.ContainsCoord(X, Y);
		bool OtherContainsTR = Other.ContainsCoord(TopRight.X, TopRight.Y);
		bool OtherContainsBL = Other.ContainsCoord(BottomLeft.X, BottomLeft.Y);
		bool OtherContainsBR = Other.ContainsCoord(BottomRight.X, BottomRight.Y);

		bool ThisContainsOtherTL = ContainsCoord(Other.X, Other.Y);
		bool ThisContainsOtherTR = ContainsCoord(Other.TopRight.X, Other.TopRight.Y);
		bool ThisContainsOtherBL = ContainsCoord(Other.BottomLeft.X, Other.BottomLeft.Y);
		bool ThisContainsOtherBR = ContainsCoord(Other.BottomRight.X, Other.BottomRight.Y);

		// Return true if we contains any of the checked rectangles points or if the checked rectangle contains any of our points
		return OtherContainsTL || OtherContainsTR || OtherContainsBL || OtherContainsBR || ThisContainsOtherTL || ThisContainsOtherTR || ThisContainsOtherBL || ThisContainsOtherBR;
	}

	public bool ContainsCoord(int X, int Y) => XCoordIsValid(X) && YCoordIsValid(Y);

	private bool XCoordIsValid(int X) => X >= this.X && X <= this.X + Width - 1;
	private bool YCoordIsValid(int Y) => Y >= this.Y && Y <= this.Y + Height - 1;
}
