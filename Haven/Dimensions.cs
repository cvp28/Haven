
namespace HavenUI;

public struct Dimensions
{
	public int WindowWidth;
	public int WindowHeight;

	public int BufferWidth;
	public int BufferHeight;

	public int VerticalCenter => WindowHeight / 2;

	public int HorizontalCenter => WindowWidth / 2;

	public int TotalCells => BufferWidth * BufferHeight;

	public static Dimensions Current => new()
	{
		WindowWidth = Console.WindowWidth,
		WindowHeight = Console.WindowHeight,
		BufferWidth = Console.BufferWidth,
		BufferHeight = Console.BufferHeight
	};

	public static bool XCoordIsValid(int X) => X <= Current.WindowWidth - 1 && X >= 0;
	public static bool YCoordIsValid(int Y) => Y <= Current.WindowHeight - 1 && Y >= 0;

	public static bool CoordIsValid(int X, int Y) => XCoordIsValid(X) && YCoordIsValid(Y);
}