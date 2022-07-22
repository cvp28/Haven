using System.Text;

namespace Haven;

public static class Sequences
{
	private static Random ansi_rand = new();

	public static void Init()
	{
		Null = new byte[] { 0, 0, 0, 0, 0 };

		CursorToTopLeft = Encoding.UTF8.GetBytes("\u001b[;H");
		Clear = new byte[3] { 27, 91, 109 };

		FgBlack = Encoding.UTF8.GetBytes("\u001b[30m");
		FgRed = Encoding.UTF8.GetBytes("\u001b[31m");
		FgGreen = Encoding.UTF8.GetBytes("\u001b[32m");
		FgYellow = Encoding.UTF8.GetBytes("\u001b[33m");
		FgBlue = Encoding.UTF8.GetBytes("\u001b[34m");
		FgMagenta = Encoding.UTF8.GetBytes("\u001b[35m");
		FgCyan = Encoding.UTF8.GetBytes("\u001b[36m");
		FgWhite = Encoding.UTF8.GetBytes("\u001b[37m");

		FgGray = Encoding.UTF8.GetBytes("\u001b[90m");
		FgBrightRed = Encoding.UTF8.GetBytes("\u001b[91m");
		FgBrightGreen = Encoding.UTF8.GetBytes("\u001b[92m");
		FgBrightYellow = Encoding.UTF8.GetBytes("\u001b[93m");
		FgBrightBlue = Encoding.UTF8.GetBytes("\u001b[94m");
		FgBrightMagenta = Encoding.UTF8.GetBytes("\u001b[95m");
		FgBrightCyan = Encoding.UTF8.GetBytes("\u001b[96m");
		FgBrightWhite = Encoding.UTF8.GetBytes("\u001b[97m");

		BgBlack = Encoding.UTF8.GetBytes("\u001b[40m");
		BgRed = Encoding.UTF8.GetBytes("\u001b[41m");
		BgGreen = Encoding.UTF8.GetBytes("\u001b[42m");
		BgYellow = Encoding.UTF8.GetBytes("\u001b[43m");
		BgBlue = Encoding.UTF8.GetBytes("\u001b[44m");
		BgMagenta = Encoding.UTF8.GetBytes("\u001b[45m");
		BgCyan = Encoding.UTF8.GetBytes("\u001b[46m");
		BgWhite = Encoding.UTF8.GetBytes("\u001b[47m");
	}

	public static byte[] Null;
	public static byte[] Clear;

	// Cursor controls

	public static byte[] CursorToTopLeft;

	// Foreground colors

	public static byte[] FgBlack;
	public static byte[] FgRed;
	public static byte[] FgGreen;
	public static byte[] FgYellow;
	public static byte[] FgBlue;
	public static byte[] FgMagenta;
	public static byte[] FgCyan;
	public static byte[] FgWhite;

	public static byte[] FgGray;
	public static byte[] FgBrightRed;
	public static byte[] FgBrightGreen;
	public static byte[] FgBrightYellow;
	public static byte[] FgBrightBlue;
	public static byte[] FgBrightMagenta;
	public static byte[] FgBrightCyan;
	public static byte[] FgBrightWhite;

	public static byte[] RandomForeground()
	{
		return ansi_rand.Next(0, 16) switch
		{
			0 => FgBlack,
			1 => FgRed,
			2 => FgGreen,
			3 => FgYellow,
			4 => FgBlue,
			5 => FgMagenta,
			6 => FgCyan,
			7 => FgWhite,

			8 => FgGray,
			9 => FgBrightRed,
			10 => FgBrightGreen,
			11 => FgBrightYellow,
			12 => FgBrightBlue,
			13 => FgBrightMagenta,
			14 => FgBrightCyan,
			15 => FgBrightWhite,
		};
	}

	public static byte[] ToFgByteArray(ConsoleColor ForegroundColor)
	{
		return ForegroundColor switch
		{
			ConsoleColor.Blue => FgBrightBlue,
			ConsoleColor.Magenta => FgBrightMagenta,
			ConsoleColor.Cyan => FgBrightCyan,
			ConsoleColor.Red => FgBrightRed,
			ConsoleColor.Green => FgBrightGreen,
			ConsoleColor.Yellow => FgBrightYellow,

			ConsoleColor.DarkBlue => FgBlue,
			ConsoleColor.DarkRed => FgRed,
			ConsoleColor.DarkMagenta => FgMagenta,
			ConsoleColor.DarkCyan => FgCyan,
			ConsoleColor.DarkYellow => FgYellow,
			ConsoleColor.DarkGreen => FgGreen,

			ConsoleColor.Black => FgBlack,
			ConsoleColor.White => FgBrightWhite,
			ConsoleColor.Gray => FgGray,
			ConsoleColor.DarkGray => FgGray,

			_ => FgBrightWhite
		};
	}

	public static byte[] ToBgByteArray(ConsoleColor BackgroundColor)
	{
		return BackgroundColor switch
		{
			ConsoleColor.Blue => BgBlue,
			ConsoleColor.Magenta => BgMagenta,
			ConsoleColor.Cyan => BgCyan,
			ConsoleColor.Red => BgRed,
			ConsoleColor.Green => BgGreen,
			ConsoleColor.Yellow => BgYellow,

			ConsoleColor.DarkBlue => BgBlue,
			ConsoleColor.DarkMagenta => BgMagenta,
			ConsoleColor.DarkCyan => BgCyan,
			ConsoleColor.DarkRed => BgRed,
			ConsoleColor.DarkGreen => BgGreen,
			ConsoleColor.DarkYellow => BgYellow,

			ConsoleColor.Black => BgBlack,
			ConsoleColor.White => BgWhite,
			ConsoleColor.Gray => BgWhite,
			ConsoleColor.DarkGray => BgWhite,

			_ => BgWhite
		};
	}

	// Background colors

	public static byte[] BgBlack;
	public static byte[] BgRed;
	public static byte[] BgGreen;
	public static byte[] BgYellow;
	public static byte[] BgBlue;
	public static byte[] BgMagenta;
	public static byte[] BgCyan;
	public static byte[] BgWhite;

	public static byte[] RandomBackground()
	{
		return ansi_rand.Next(0, 8) switch
		{
			0 => BgBlack,
			1 => BgRed,
			2 => BgGreen,
			3 => BgYellow,
			4 => BgBlue,
			5 => BgMagenta,
			6 => BgCyan,
			7 => BgWhite
		};
	}
}
