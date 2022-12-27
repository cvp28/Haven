using System.Text;

namespace Haven;

public enum InputFilter
{
	None,
	Numerics,
	NumericsWithDots,
	NumericsWithSingleDot,
}

public class InputField : Widget
{
	public int X { get; set; }
	public int Y { get; set; }

	public ConsoleColor CursorForeground;
	public ConsoleColor CursorBackground;

	public bool DrawCursor = true;

	public string Prompt { get; set; }
	private StringBuilder Buffer;
	private List<string> History;

	private int CurrentBufferIndex = 0;

	public Action<string> OnInput { get; set; }
	public InputFilter Filter { get; set; }

	public InputField(int X, int Y, string Prompt)
	{
		this.X = X;
		this.Y = Y;
		this.Prompt = Prompt;
		CursorForeground = ConsoleColor.Black;
		CursorBackground = ConsoleColor.White;

		Filter = InputFilter.None;

		Buffer = new();
		History = new();
		OnInput = null;
	}

	public InputField(int X, int Y, string Prompt, ConsoleColor CursorForeground, ConsoleColor CursorBackground)
	{
		this.X = X;
		this.Y = Y;
		this.Prompt = Prompt;
		this.CursorForeground = CursorForeground;
		this.CursorBackground = CursorBackground;

		Filter = InputFilter.None;

		Buffer = new();
		History = new();
		OnInput = null;
	}

	public override void Draw(Renderer s)
	{
		s.WriteStringAt(X, Y, $"{Prompt}{Buffer}");

		if (DrawCursor && Focused)
			s.AddColorsAt(X + Prompt.Length + CurrentBufferIndex, Y, CursorForeground, CursorBackground);
	}

	public void CenterTo(Dimensions d, int XOff = 0, int YOff = 0)
	{
		X = d.HorizontalCenter - ((Prompt.Length + Buffer.Length) / 2) + XOff;
		Y = d.VerticalCenter + YOff;

		if (X < 0)
			X = 0;

		if (Y < 0)
			Y = 0;
	}

	public void Clear()
	{
		while (CurrentBufferIndex > 0)
			Backspace();
	}

	private void CursorToStart()
	{
		CurrentBufferIndex = 0;
	}

	private void CursorToEnd()
	{
		CurrentBufferIndex = Buffer.Length;
	}

	private void CursorLeft()
	{
		if (CurrentBufferIndex == 0)
			return;

		CurrentBufferIndex--;
	}

	private void CursorRight()
	{
		if (CurrentBufferIndex == Buffer.Length)
			return;

		CurrentBufferIndex++;
	}

	private void Backspace(bool Force = false)
	{
		if (!Force)
			if (CurrentBufferIndex == 0)
				return;

		// Remove 1 character starting at the current buffer index
		Buffer.Remove(CurrentBufferIndex - 1, 1);
		CursorLeft();
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{
		bool RaiseInputEvent = false;

		switch (cki.Key)
		{
			case ConsoleKey.Enter:
				RaiseInputEvent = true;
				break;

			case ConsoleKey.LeftArrow:
				CursorLeft();
				break;

			case ConsoleKey.RightArrow:
				CursorRight();
				break;

			case ConsoleKey.Backspace:
				Backspace();
				break;

			case ConsoleKey.Home:
				CursorToStart();
				break;

			case ConsoleKey.End:
				CursorToEnd();
				break;

			default:
				char c = cki.KeyChar;
				bool Valid = false;

				switch (Filter)
				{
					case InputFilter.None:
						Valid = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c) || char.IsSymbol(c);
						break;

					case InputFilter.Numerics:
						Valid = char.IsDigit(c);
						break;

					case InputFilter.NumericsWithDots:
						Valid = char.IsDigit(c) || c == '.';
						break;

					case InputFilter.NumericsWithSingleDot:

						if (Buffer.ToString().Contains('.'))
							Valid = char.IsDigit(c);
						else
							Valid = char.IsDigit(c) || c == '.';

						break;
				}

				if (!Valid)
					break;

				Buffer.Insert(CurrentBufferIndex, c);
				CurrentBufferIndex++;
				break;
		}


		if (RaiseInputEvent)
		{
			string Result = Buffer.ToString();

			OnInput?.Invoke(Result);
			History.Append(Result);

			Buffer.Clear();
			CurrentBufferIndex = 0;
		}
	}
}
