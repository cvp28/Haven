using System;

namespace HavenUI;

public struct State
{
	// Tracks if the console dimensions were changed in the current iteration of the app main loop
	public bool DimensionsChanged;

	// Event parameters for a dimension change event
	public Dimensions Dimensions;

	// Tracks render performance as reported by the renderer
	public long FPS;
	public long LastFrameTime;

	public int WidgetRenderTimeMs;
	public int StdoutWriteTimeMs;
	public int DiagTime1Ms;
	public int DiagTime2Ms;
}