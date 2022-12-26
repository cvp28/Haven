
namespace Haven;

public static class CharSequences
{
	public static void Init()
	{
		FgBlack = "\u001b[30m".ToCharArray();
		FgRed = "\u001b[31m".ToCharArray();
		FgGreen = "\u001b[32m".ToCharArray();
		FgYellow = "\u001b[33m".ToCharArray();
		FgBlue = "\u001b[34m".ToCharArray();
		FgMagenta = "\u001b[35m".ToCharArray();
		FgCyan = "\u001b[36m".ToCharArray();
		FgWhite = "\u001b[37m".ToCharArray();

		FgGray = "\u001b[90m".ToCharArray();
		FgBrightRed = "\u001b[91m".ToCharArray();
		FgBrightGreen = "\u001b[92m".ToCharArray();
		FgBrightYellow = "\u001b[93m".ToCharArray();
		FgBrightBlue = "\u001b[94m".ToCharArray();
		FgBrightMagenta = "\u001b[95m".ToCharArray();
		FgBrightCyan = "\u001b[96m".ToCharArray();
		FgBrightWhite = "\u001b[97m".ToCharArray();

		BgBlack = "\u001b[40m\0".ToCharArray();
		BgRed = "\u001b[41m\0".ToCharArray();
		BgGreen = "\u001b[42m\0".ToCharArray();
		BgYellow = "\u001b[43m\0".ToCharArray();
		BgBlue = "\u001b[44m\0".ToCharArray();
		BgMagenta = "\u001b[45m\0".ToCharArray();
		BgCyan = "\u001b[46m\0".ToCharArray();
		BgWhite = "\u001b[47m\0".ToCharArray();

		BgGray = "\u001b[100m".ToCharArray();
		BgBrightRed = "\u001b[101m".ToCharArray();
		BgBrightGreen = "\u001b[102m".ToCharArray();
		BgBrightYellow = "\u001b[103m".ToCharArray();
		BgBrightBlue = "\u001b[104m".ToCharArray();
		BgBrightMagenta = "\u001b[105m".ToCharArray();
		BgBrightCyan = "\u001b[106m".ToCharArray();
		BgBrightWhite = "\u001b[107m".ToCharArray();
	}

	public static char[] FgBlack;
	public static char[] FgRed;
	public static char[] FgGreen;
	public static char[] FgYellow;
	public static char[] FgBlue;
	public static char[] FgMagenta;
	public static char[] FgCyan;
	public static char[] FgWhite;

	public static char[] FgGray;
	public static char[] FgBrightRed;
	public static char[] FgBrightGreen;
	public static char[] FgBrightYellow;
	public static char[] FgBrightBlue;
	public static char[] FgBrightMagenta;
	public static char[] FgBrightCyan;
	public static char[] FgBrightWhite;

	public static char[] BgBlack;
	public static char[] BgRed;
	public static char[] BgGreen;
	public static char[] BgYellow;
	public static char[] BgBlue;
	public static char[] BgMagenta;
	public static char[] BgCyan;
	public static char[] BgWhite;

	public static char[] BgGray;
	public static char[] BgBrightRed;
	public static char[] BgBrightGreen;
	public static char[] BgBrightYellow;
	public static char[] BgBrightBlue;
	public static char[] BgBrightMagenta;
	public static char[] BgBrightCyan;
	public static char[] BgBrightWhite;

	public static char[] ToFgCharArray(ConsoleColor ForegroundColor)
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
			ConsoleColor.DarkMagenta => FgMagenta,
			ConsoleColor.DarkCyan => FgCyan,
			ConsoleColor.DarkRed => FgRed,
			ConsoleColor.DarkGreen => FgGreen,
			ConsoleColor.DarkYellow => FgYellow,

			ConsoleColor.Black => FgBlack,
			ConsoleColor.White => FgBrightWhite,
			ConsoleColor.Gray => FgWhite,
			ConsoleColor.DarkGray => FgGray,

			_ => FgBrightWhite
		};
	}

	public static char[] ToBgCharArray(ConsoleColor BackgroundColor)
	{
		return BackgroundColor switch
		{
			ConsoleColor.Blue => BgBrightBlue,
			ConsoleColor.Magenta => BgBrightMagenta,
			ConsoleColor.Cyan => BgBrightCyan,
			ConsoleColor.Red => BgBrightRed,
			ConsoleColor.Green => BgBrightGreen,
			ConsoleColor.Yellow => BgBrightYellow,

			ConsoleColor.DarkBlue => BgBlue,
			ConsoleColor.DarkMagenta => BgMagenta,
			ConsoleColor.DarkCyan => BgCyan,
			ConsoleColor.DarkRed => BgRed,
			ConsoleColor.DarkGreen => BgGreen,
			ConsoleColor.DarkYellow => BgYellow,

			ConsoleColor.Black => BgBlack,
			ConsoleColor.White => BgBrightWhite,
			ConsoleColor.Gray => BgWhite,
			ConsoleColor.DarkGray => BgGray,

			_ => BgBrightWhite
		};
	}
}
