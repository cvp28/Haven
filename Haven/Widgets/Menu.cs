

using System.Runtime.InteropServices;

namespace Haven;

public class Menu : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	private List<string> Options;
	private List<Action> Actions;

	public int OptionCount => Options.Count;

	public MenuStyle SelectedOptionStyle { get; set; }

	public Alignment TextAlignment { get; set; }

	/// <summary>
	/// Determines if the options will always be drawn styled regardless of if the menu is focused
	/// </summary>
	public bool AlwaysStyle { get; set; }

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

	private bool ValidOptionNumber(int OptionNumber) => OptionNumber >= 0 && OptionNumber < Options.Count;

	public void EditOption(int OptionNumber, string NewText)
	{
		if (!ValidOptionNumber(OptionNumber))
			return;

		Options[OptionNumber] = NewText;
	}

	public void EditOption(int OptionNumber, Action NewAction)
	{
		if (!ValidOptionNumber(OptionNumber))
			return;

		Actions[OptionNumber] = NewAction;
	}

	public void EditOption(int OptionNumber, string NewText, Action NewAction)
	{
		if (!ValidOptionNumber(OptionNumber))
			return;

		Options[OptionNumber] = NewText;
		Actions[OptionNumber] = NewAction;
	}

	public string GetOptionText(int OptionNumber)
	{
		if (!ValidOptionNumber(OptionNumber))
			return string.Empty;

		return Options[OptionNumber];
	}

	public void RemoveAllOptions()
	{
		Options.Clear();
		Actions.Clear();
	}

	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		if (Options.Count == 0)
			return;

		int LongestOptionLength = Options.Max(op => op.Length);

		X = d.HorizontalCenter - (LongestOptionLength / 2) + XOff;
		Y = d.VerticalCenter - (int) Math.Ceiling( OptionCount / 2.0f ) + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}

	public override void Draw(Renderer s)
	{
		if (Options.Count == 0)
			return;

		switch (TextAlignment)
		{
			case Alignment.Left:
				DrawLeftAligned();
				break;

			case Alignment.Center:
				DrawCenterAligned();
				break;

			case Alignment.Right:
				DrawLeftAligned();
				break;
		}

		void DrawLeftAligned()
		{
			for (int i = 0; i < Options.Count; i++) 
			{
				if (i == SelectedOption)
					DrawStyledText(X, Y + i, s, Options[i]);
				else
					s.WriteStringAt(X, Y + i, Options[i]);
			}
		}

		void DrawCenterAligned()
		{
			int LongestOptionLength = Options.Max(op => op.Length);

			for (int i = 0; i < Options.Count; i++)
			{
				int CurrentX = X + (LongestOptionLength / 2) - (Options[i].Length / 2);

				if (i == SelectedOption)
					DrawStyledText(CurrentX, Y + i, s, Options[i]);
				else
					s.WriteStringAt(CurrentX, Y + i, Options[i]);
			}
		}
	}

	private void DrawStyledText(int X, int Y, Renderer s, string Text)
	{
		if (!Focused && !AlwaysStyle)
		{
			s.WriteStringAt(X, Y, Text);
			return;
		}

		switch (SelectedOptionStyle)
		{
			case MenuStyle.Arrow:
				s.WriteStringAt(X, Y, $"{Text} <");
				break;

			case MenuStyle.Highlighted:
				s.WriteColorStringAt(X, Y, $"{Text}", ConsoleColor.Black, ConsoleColor.White);
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
