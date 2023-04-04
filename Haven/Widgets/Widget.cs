
namespace Haven;

public abstract class Widget
{
	public bool Focused { get; internal set; }
	public bool Visible { get; set; }
	public Dictionary<ConsoleKey, Action> KeyActions { get; set; }
	public KeyActionMode KeyActionProcessingMode { get; set; }

	public Widget()
	{
		KeyActions = new();
		//KeyActionProcessingMode = KeyActionMode.Disabled;

		Visible = true;
	}

	public abstract void Draw(Renderer s);

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
