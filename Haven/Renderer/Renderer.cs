
using System.Diagnostics.CodeAnalysis;

namespace Haven;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Renderer
{
	public abstract void Render(IEnumerable<Widget> Widgets);

	public abstract void UpdateScreenDimensions();

	public abstract void AddColorsAt(int X, int Y, ConsoleColor Foreground, ConsoleColor Background);

	/// <summary>
	/// Copies a CharacterInfo buffer to the main screen renderer buffer.
	/// </summary>
	/// <param name="X">X-Coordinate to copy to</param>
	/// <param name="Y">Y-Coordinate to copy to</param>
	/// <param name="ViewWidth">Width of the view that the CharacterInfo buffer will be projected in to</param>
	/// <param name="ViewHeight">Height of the view that the CharacterInfo buffer will be projected in to</param>
	/// <param name="BufferWidth">The full width of the 2D CharacterInfo buffer (likely larger than the ViewWidth)</param>
	/// <param name="Buffer">The CharacterInfo buffer to copy from</param>
	public abstract void CopyToBuffer2D(int X, int Y, int ViewWidth, int ViewHeight, int BufferWidth, ref CharacterInfo[] Buffer);

	public abstract void WriteStringAt(int X, int Y, string Text);

	public abstract void WriteColorStringAt(int X, int Y, string Text, ConsoleColor Foreground, ConsoleColor Background);

	public abstract void DrawBox(int X, int Y, int Width, int Height);

	public int WidgetRenderTimeMs { get; protected set; }
	public int StdoutWriteTimeMs { get; protected set; }
	public int DiagTime1Ms { get; protected set; }
	public int DiagTime2Ms { get; protected set; }

	public Renderer() { }
}
