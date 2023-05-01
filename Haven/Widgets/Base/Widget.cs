
namespace Haven;

public abstract class Widget
{
	public bool Focused { get; protected internal set; }
	public bool Visible { get; set; }
	public Dictionary<ConsoleKey, Action> KeyActions { get; set; }
	public KeyActionMode KeyActionProcessingMode { get; set; }

	// Determines if the widget is clear to be drawn with no extra steps
	// If false, the widget must clean up its own garbage before drawing itself again
	public bool Valid { get; protected internal set; }

	// The render (drawing) context for the widget
	protected internal VTRenderContext RenderContext;

	public Widget()
	{
		Visible = true;
		KeyActions = new();
		RenderContext = new();
	}

	// Called when the widget is requested to draw itself to the screen
	public abstract void Draw();

	internal void _OnConsoleKey(ConsoleKeyInfo cki)
	{
		switch (KeyActionProcessingMode)
		{
			case KeyActionMode.Disabled:
				OnConsoleKey(cki);
				return;

			case KeyActionMode.Override:
				if (KeyActions.ContainsKey(cki.Key))
					KeyActions[cki.Key]();

				return;

			case KeyActionMode.Before:
				if (KeyActions.ContainsKey(cki.Key))
					KeyActions[cki.Key]();

				OnConsoleKey(cki);
				return;

			case KeyActionMode.After:
				OnConsoleKey(cki);

				if (KeyActions.ContainsKey(cki.Key))
					KeyActions[cki.Key]();
				return;
		}
	}

	public abstract void OnConsoleKey(ConsoleKeyInfo cki);

	public void AddKeyAction(ConsoleKey Key, Action Action)
	{
		if (KeyActions.ContainsKey(Key))
			return;

		KeyActions.Add(Key, Action);
	}

	public void OverrideKeyAction(ConsoleKey Key, Action Action)
	{
		if (!KeyActions.ContainsKey(Key))
			return;

		KeyActions[Key] = Action;
	}

	public void RemoveKeyAction(ConsoleKey Key)
	{
		if (!KeyActions.ContainsKey(Key))
			return;

		KeyActions.Remove(Key);
	}
}
