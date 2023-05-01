
using System.Collections.Concurrent;

namespace Haven;

public class Menu : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	private List<MenuOption> Options;

	public MenuOption this[int Index]
	{
		get
		{
			if (!IsValidIndex(Index))
				return null;

			return Options[Index];
		}

		set
		{
			if (!IsValidIndex(Index))
				return;

			Options[Index] = value;
		}
	}

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
	}

	private bool IsValidIndex(int Index) => Index >= 0 && Index < Options.Count;

	public void AddOption(string Option, Action Action, byte Foreground = 15, byte Background = 0)
	{
		if (Option is null || Action is null)
			return;

		var NewOption = new MenuOption()
		{
			Text = Option,
			Action = Action,
			TextForeground = Foreground,
			TextBackground = Background
		};

		Options.Add(NewOption);
	}

	public void RemoveAllOptions()
	{
		SelectedOption = 0;
		Options.Clear();
	}

	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		if (Options.Count == 0)
			return;

		int LongestOptionLength = Options.Max(op => op.Text.Length);

		X = d.HorizontalCenter - (LongestOptionLength / 2) + XOff;
		Y = d.VerticalCenter - (int) Math.Ceiling( OptionCount / 2.0f ) + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}

	public override void Draw()
	{
		if (Options.Count == 0)
			return;

		switch (TextAlignment)
		{
			case Alignment.Center:
				DrawCenterAligned();
				break;

			default:
			case Alignment.Left:
				DrawLeftAligned();
				break;
		}

		void DrawLeftAligned()
		{
			for (int i = 0; i < Options.Count; i++) 
			{
				if (i == SelectedOption)
					DrawStyledOption(X, Y + i, Options[i]);
				else
				{
					RenderContext.VTSetCursorPosition(X, Y + i);
					RenderContext.VTEnterColorContext(Options[i].TextForeground, Options[i].TextBackground, delegate ()
					{
						RenderContext.VTDrawText(Options[i].Text);
					});
				}
			}
		}

		void DrawCenterAligned()
		{
			int LongestOptionLength = Options.Max(op => op.Text.Length);

			for (int i = 0; i < Options.Count; i++)
			{
				int CurrentX = X + (LongestOptionLength / 2) - (Options[i].Text.Length / 2);

				if (i == SelectedOption)
					DrawStyledOption(CurrentX, Y + i, Options[i]);
				else
				{
					RenderContext.VTSetCursorPosition(CurrentX, Y + i);
					RenderContext.VTEnterColorContext(Options[i].TextForeground, Options[i].TextBackground, delegate ()
					{
						RenderContext.VTDrawText(Options[i].Text);
					});
				}
			}
		}
	}

	private void DrawStyledOption(int X, int Y, MenuOption Option)
	{
		if (!Focused && !AlwaysStyle)
		{
			RenderContext.VTSetCursorPosition(X, Y);
			RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
			{
				RenderContext.VTDrawText(Option.Text);
			});
			return;
		}

		switch (SelectedOptionStyle)
		{
			case MenuStyle.Arrow:
				{
					RenderContext.VTSetCursorPosition(X, Y);
					RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
					{
						RenderContext.VTDrawText(Option.Text);
					});
					break;
				}

			case MenuStyle.Highlighted:
				{
					RenderContext.VTSetCursorPosition(X, Y);
					RenderContext.VTInvert();
					RenderContext.VTDrawText(Option.Text);
					RenderContext.VTRevert();
					break;
				}
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
				Options[SelectedOption].Action();
				break;
		}
	}
}
