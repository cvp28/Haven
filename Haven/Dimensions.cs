
namespace Haven;

public struct Dimensions
{
	public int WindowWidth;
	public int WindowHeight;

	public int BufferWidth;
	public int BufferHeight;

	public int VerticalCenter => WindowHeight / 2;

	public int HorizontalCenter => WindowWidth / 2;

	public static Dimensions Current => new()
	{
		WindowWidth = Console.WindowWidth,
		WindowHeight = Console.WindowHeight,
		BufferWidth = Console.BufferWidth,
		BufferHeight = Console.BufferHeight
	};
}