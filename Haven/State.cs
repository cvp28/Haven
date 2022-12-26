using System;

namespace Haven;

public struct State
{
	// Tracks whether or not user input has already been sent to a focused widget
	public bool InputAlreadyHandled;

	// Tracks if a key was pressed in the current iteration of the app main loop
	public bool KeyPressed;

	// Tracks if the console dimensions were changed in the current iteration of the app main loop
	public bool DimensionsChanged;

	// Event parameters for input and dimension change events
	public ConsoleKeyInfo KeyInfo;
	public Dimensions Dimensions;

	// Tracks render performance as reported by the renderer
	public long FPS;
	public long LastFrameTime;

	public int WidgetRenderTimeMs;
	public int StdoutWriteTimeMs;
	public int DiagTime1Ms;
	public int DiagTime2Ms;
}