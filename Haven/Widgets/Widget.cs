
namespace Haven;

public abstract class Widget
{
	public bool Focused { get; internal set; }
	public bool Visible { get; set; }
	public Dictionary<ConsoleKey, Action> KeyActions { get; set; }

	public Widget()
	{
		KeyActions = new();
		Visible = true;
	}

	public abstract void Draw(Renderer s);

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
