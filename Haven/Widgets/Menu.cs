

namespace Haven;

public class Menu : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	private List<string> Options;
	private List<Action> Actions;

	public MenuStyle SelectedOptionStyle { get; set; }

	/// <summary>
	/// Determines if the options will always be drawn styled regardless of if the menu is focused
	/// </summary>
	public bool AlwaysStyle;

	public int SelectedOption { get; private set; }

	public Menu(int X, int Y)
	{
		this.X = X;
		this.Y = Y;
		SelectedOption = 0;
		AlwaysStyle = false;

		Options = new();
		Actions = new();
	}

	public void AddOption(string Option, Action Action)
	{
		if (Option is null || Action is null)
			return;

		Options.Add(Option);
		Actions.Add(Action);
	}

	public void RemoveAllOptions()
	{
		Options.Clear();
		Actions.Clear();
	}

	public override void Draw(IRenderer s)
	{
		int OptionNum = 0;
		foreach (var Option in Options)
		{
			if (OptionNum == SelectedOption)
				DrawSelectedOption(s, OptionNum);
			else
				s.WriteStringAt(X, Y + OptionNum, $"{Option}");

			OptionNum++;
		}
	}

	private void DrawSelectedOption(IRenderer s, int OptionNum)
	{
		if (!Focused && !AlwaysStyle)
		{
			s.WriteStringAt(X, Y + OptionNum, $"{Options[OptionNum]}");
			return;
		}

		switch (SelectedOptionStyle)
		{
			case MenuStyle.Arrow:
				s.WriteStringAt(X, Y + OptionNum, $"{Options[OptionNum]} <");
				break;

			case MenuStyle.Highlighted:
				s.WriteColorStringAt(X, Y + OptionNum, $"{Options[OptionNum]}", ConsoleColor.Black, ConsoleColor.White);
				break;
		}
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{
		if (Options.Count == 0)
			return;

		switch (cki.Key)
		{
			case ConsoleKey.UpArrow:
				if (SelectedOption == 0)
					SelectedOption = Options.Count - 1;
				else
					SelectedOption--;
				break;

			case ConsoleKey.DownArrow:
				if (SelectedOption == Options.Count - 1)
					SelectedOption = 0;
				else
					SelectedOption++;
				break;

			case ConsoleKey.Enter:
				Actions[SelectedOption].Invoke();
				break;
		}
	}
}
