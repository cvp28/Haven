using System.Text;

namespace Haven;

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
	private readonly object BufferLock;

	private int CurrentBufferIndex = 0;

	public Action<string> OnInput { get; set; }

	public InputField(int X, int Y, string Prompt)
	{
		this.X = X;
		this.Y = Y;
		this.Prompt = Prompt;
		CursorForeground = ConsoleColor.Black;
		CursorBackground = ConsoleColor.White;

		Buffer = new();
		History = new();
		BufferLock = new();
		OnInput = null;
	}

	public InputField(int X, int Y, string Prompt, ConsoleColor CursorForeground, ConsoleColor CursorBackground)
	{
		this.X = X;
		this.Y = Y;
		this.Prompt = Prompt;
		this.CursorForeground = CursorForeground;
		this.CursorBackground = CursorBackground;

		Buffer = new();
		History = new();
		BufferLock = new();
		OnInput = null;
	}

	public override void Draw(IRenderer s)
	{
		lock (BufferLock)
			s.WriteStringAt(X, Y, $"{Prompt}{Buffer}");

		if (DrawCursor && Focused)
			s.AddColorsAt(X + Prompt.Length + CurrentBufferIndex, Y, CursorForeground, CursorBackground);
	}

	public void Clear()
	{
		lock (BufferLock)
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
				bool Valid = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c);

				if (!Valid)
					break;

				lock (BufferLock)
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
