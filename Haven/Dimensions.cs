
namespace Haven;

public struct Dimensions
{
	public int WindowWidth;
	public int WindowHeight;

	public int BufferWidth;
	public int BufferHeight;

	public static Dimensions Current => new()
	{
		WindowWidth = Console.WindowWidth,
		WindowHeight = Console.WindowHeight,
		BufferWidth = Console.BufferWidth,
		BufferHeight = Console.BufferHeight
	};
}