namespace HavenUI;

public class MenuState
{
	public int X { get; set; }
	public int Y { get; set; }

	internal List<MenuOption> Options;
	
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
	public bool DoStyle { get; set; }

	public int SelectedOption { get; internal set; }
	
	public MenuState(int X, int Y)
	{
		this.X = X;
		this.Y = Y;
		SelectedOption = 0;
		DoStyle = false;

		Options = new();
	}
	
	private bool IsValidIndex(int Index) => Index >= 0 && Index < Options.Count;

	public void AddOption(string Option, Action Action, byte Foreground = 15, byte Background = 0)
	{
		if (Option is null || Action is null)
			return;

		var NewOption = new MenuOption()
		{
			Index = Options.Count,
			
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
}
