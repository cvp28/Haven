using System.Text;
using System.Text.RegularExpressions;

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

	public byte CursorForeground;
	public byte CursorBackground;

	public bool DrawCursor = true;

	public string Prompt { get; set; }
	private StringBuilder Buffer;

	public int CurrentHistoryIndex { get; private set; } = 0;
	public List<string> History = new();

	public int BufferLength => Buffer.Length;

	public int CursorX => (X + Prompt.Length + CurrentBufferIndex) % Dimensions.Current.WindowWidth;
	public int CursorY => ((X + Prompt.Length + CurrentBufferIndex) / Dimensions.Current.WindowWidth) + 1;

	private int CurrentBufferIndex = 0;
	public int MaxBufferIndex => Buffer.Length - 1;

	public bool HighlightingEnabled = false;
	public bool HistoryEnabled = true;

	public Action<string> OnInput { get; set; }

	public Func<Token, IEnumerable<Token>, IEnumerable<string>> OnRetrieveSuggestions { get; set; }
	public Action<IEnumerable<Token>> OnHighlight { get; set; }

	public List<Token> CurrentTokens = new();

	private bool InAutoCompleteMode = false;
	private string[] AutoCompleteSuggestions;
	private int CurrentCompletionsIndex = 0;

	public InputFilter Filter { get; set; }

	public InputField(int X, int Y, string Prompt) : this(X, Y, Prompt, ConsoleColor.Black.ToByte(), ConsoleColor.White.ToByte())
	{ }

	public InputField(int X, int Y, string Prompt, byte CursorForeground, byte CursorBackground)
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

	public override void Draw()
	{
		// Set cursor position
		RenderContext.VTSetCursorPosition(X, Y);

		// Draw buffer
		RenderContext.VTDrawText($"{Prompt}{Buffer}");

		// Do highlighting
		if (HighlightingEnabled)
			foreach (var Token in CurrentTokens)
			{
				bool ForegroundDiff = Token.HighlightForeground != VTRenderContext.CurrentForegroundColor;
				bool BackgroundDiff = Token.HighlightBackground != VTRenderContext.CurrentBackgroundColor;

				// Only do token highlighting if the colors are different
				if (ForegroundDiff || BackgroundDiff)
				{
					RenderContext.VTEnterColorContext(Token.HighlightForeground, Token.HighlightBackground, delegate ()
					{
						RenderContext.VTSetCursorPosition(X + Prompt.Length + Token.StartIndex + (Token.Quoted ? 1 : 0), Y);
						RenderContext.VTDrawText(Token.Content);
					});
				}
				
			}

		// Draw cursor
		if (DrawCursor && Focused)
		{
			RenderContext.VTSetCursorPosition(X + Prompt.Length + CurrentBufferIndex, Y);

			RenderContext.VTInvert();

			if (Buffer.Length > 0 && CurrentBufferIndex != Buffer.Length)
				RenderContext.VTDrawText($"{Buffer[CurrentBufferIndex]}");
			else 
				RenderContext.VTDrawText(" ");

			RenderContext.VTRevert();
		}

			//	RenderContext.VTEnterColorContext(1, 196, delegate ()
			//	{
			//		RenderContext.VTSetCursorPosition(X + Prompt.Length + CurrentBufferIndex, Y);
			//	
			//		if (CurrentBufferIndex == Buffer.Length)
			//			RenderContext.VTDrawText(" ");
			//		else
			//			RenderContext.VTDrawText($"{Buffer[CurrentBufferIndex]}");
			//	});
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

	private bool InHistoryMode = false;
	private string BufferBackup;

	private int IncrementInRange(int Value, int LowerBound, int UpperBound)
	{
		if (Value < UpperBound)
			return Value + 1;
		else
			return LowerBound;
	}

	private int DecrementInRange(int Value, int LowerBound, int UpperBound)
	{
		if (Value > LowerBound)
			return Value - 1;
		else
			return UpperBound;
	}

	public override void OnConsoleKey(ConsoleKeyInfo cki)
	{
		bool RaiseInputEvent = false;

		if (cki.Key != ConsoleKey.Tab)
			InAutoCompleteMode = false;

		if (cki.Key != ConsoleKey.UpArrow && cki.Key != ConsoleKey.DownArrow)
		{
			InHistoryMode = false;
			CurrentHistoryIndex = 0;
		}

		// Handle key
		switch (cki.Key)
		{
			case ConsoleKey.Enter:
				RaiseInputEvent = true;
				break;

			case ConsoleKey.UpArrow:
				if (History.Count == 0)
					break;

				if (!InHistoryMode)
				{
					BufferBackup = Buffer.ToString();
					InHistoryMode = true;

					if (History.Count > 0)
						CurrentHistoryIndex = History.Count - 1;
				}
				else
				{
					if (History.Count > 0)
						CurrentHistoryIndex = DecrementInRange(CurrentHistoryIndex, -1, History.Count - 1);
					else
						break;
				}

				if (CurrentHistoryIndex == -1)
				{
					Buffer.Clear();
					Buffer.Insert(0, BufferBackup);

					if (Buffer.Length == 0)
						CurrentBufferIndex = 0;
					else
						CurrentBufferIndex = Buffer.Length;

					InHistoryMode = false;
				}
				else
				{
					Buffer.Clear();
					Buffer.Insert(0, History[CurrentHistoryIndex]);
					CurrentBufferIndex = Buffer.Length;
				}

				break;

			case ConsoleKey.DownArrow:
				if (History.Count == 0)
					break;

				if (!InHistoryMode)
				{
					BufferBackup = Buffer.ToString();
					InHistoryMode = true;

					CurrentHistoryIndex = 0;
				}
				else
				{
					if (History.Count > 0)
						CurrentHistoryIndex = IncrementInRange(CurrentHistoryIndex, -1, History.Count - 1);
					else
						break;
				}

				if (CurrentHistoryIndex == -1)
				{
					Buffer.Clear();
					Buffer.Insert(0, BufferBackup);

					if (Buffer.Length == 0)
						CurrentBufferIndex = 0;
					else
						CurrentBufferIndex = Buffer.Length;

					InHistoryMode = false;
				}
				else
				{
					Buffer.Clear();
					Buffer.Insert(0, History[CurrentHistoryIndex]);
					CurrentBufferIndex = Buffer.Length;
				}

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

			case ConsoleKey.Tab:
				var SelectedToken = CurrentTokens.FirstOrDefault(t => t.Selected);

				// If there is no currently selected token, break
				if (SelectedToken is null)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				int Index = CurrentTokens.IndexOf(SelectedToken);

				// If token is not found in list (for whatever reason), break
				if (Index == -1)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				// If suggestions hander is null, break
				if (OnRetrieveSuggestions is null)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = false;
					break;
				}

				// This is ugly, I know
				void SetCurrentToken(string NewTokenContent, bool InsertQuotes)
				{
					if (SelectedToken.Quoted)
					{
						Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length + 2);

						if (InsertQuotes)
						{
							Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
						}
						else
						{
							Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
						}

					}
					else
					{
						Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length);

						if (InsertQuotes)
						{
							Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
						}
						else
						{
							Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
							CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
						}
					}
				}

				if (!InAutoCompleteMode)
				{
					CurrentCompletionsIndex = 0;
					InAutoCompleteMode = true;

					var temp = OnRetrieveSuggestions(SelectedToken, CurrentTokens)?.ToArray();

					// If the caller returned null (for whatever reason) or the returned container did not have any elements
					if (temp is null || !temp.Any())
					{
						CurrentCompletionsIndex = 0;
						InAutoCompleteMode = false;
						break;
					}

					AutoCompleteSuggestions = temp;

					SetCurrentToken(AutoCompleteSuggestions[CurrentCompletionsIndex], AutoCompleteSuggestions[CurrentCompletionsIndex].Contains(' '));
					break;
				}
				else
				{
					if (CurrentCompletionsIndex == AutoCompleteSuggestions.Length - 1)
						CurrentCompletionsIndex = 0;
					else
						CurrentCompletionsIndex++;

					SetCurrentToken(AutoCompleteSuggestions[CurrentCompletionsIndex], AutoCompleteSuggestions[CurrentCompletionsIndex].Contains(' '));
					break;
				}

			#region Shit that is old and complicated
			//	// Construct temporary buffer using updated tokens
			//	TempBuilder.Clear();
			//	
			//	for (int i = 0; i < CurrentTokens.Count; i++)
			//	{
			//		if (CurrentTokens[i].Quoted)
			//		{
			//			TempBuilder.Append($"\"{CurrentTokens[i].Content}\"");
			//		}
			//		else
			//		{
			//			TempBuilder.Append(CurrentTokens[i].Content);
			//		}
			//	
			//		if (i != CurrentTokens.Count - 1)
			//			TempBuilder.Append(' ');
			//	}
			//	
			//	// Tokenize temporary buffer
			//	var TempTokens = Tokenizer.Tokenize(TempBuilder.ToString());
			//	
			//	// Also tokenize the replacement string to see if it has more than 1 token
			//	// If it does, then it messes up the math and we have to perform another check to ensure the cursor gets put in the right place
			//	var ReplacedTokens = Tokenizer.Tokenize(SelectedToken.Content);
			//	
			//	int IndexOffset = 0;
			//	
			//	if (ReplacedTokens.Length > 1 && !SelectedToken.Quoted)
			//		IndexOffset = ReplacedTokens.Length - 1;
			//	
			//	if (TempTokens.Length == 0)
			//		goto ReconstructBuffer;
			//	
			//	if (Index > TempTokens.Length - 1)
			//		Index = TempTokens.Length - 1;
			//	
			//	if (Index < 0)
			//		Index = 0;
			//	
			//	Token token = TempTokens[Index + IndexOffset];
			//	
			//	if (token.Quoted)
			//		CurrentBufferIndex = token.StartIndex + token.Content.Length + 2;
			//	else
			//		CurrentBufferIndex = token.StartIndex + token.Content.Length;
			//	
			//	ReconstructBuffer:
			//	
			//	Buffer.Clear();
			//	Buffer.Append(TempBuilder.ToString());
			//	
			//	if (CurrentBufferIndex > MaxBufferIndex + 1)
			//		CurrentBufferIndex = MaxBufferIndex + 1;
			//	break;
			#endregion

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

		// Handle input callback
		if (RaiseInputEvent)
		{
			string Result = Buffer.ToString();

			OnInput?.Invoke(Result);

			if (Result.Length > 0)
				History.Add(Result);

			Buffer.Clear();
			CurrentBufferIndex = 0;
		}

		// Clear current tokens list
		CurrentTokens.Clear();

		// Tokenize buffer
		var tokens = Tokenize(Buffer.ToString());

		// Determine currently selected token
		for (int i = 0; i < tokens.Length; i++)
		{
			Token CurrentToken = tokens[i];

			int Offset = 0;

			if (CurrentToken.Quoted)
			{
				bool IsTokenImmediatelyAfter = tokens.FirstOrDefault(t => t.StartIndex == CurrentToken.StartIndex + CurrentToken.Content.Length + 2) is not null;

				Offset = IsTokenImmediatelyAfter ? 2 : 3;
			}
			else
			{
				Offset = 1;
			}

			if (CurrentBufferIndex >= CurrentToken.StartIndex && CurrentBufferIndex < CurrentToken.StartIndex + CurrentToken.Content.Length + Offset)
				CurrentToken.Selected = true;

			CurrentTokens.Add(CurrentToken);
		}

		// Perform syntax highlighting via user-set rules if enabled
		if (HighlightingEnabled && OnHighlight is not null)
			OnHighlight(CurrentTokens);
	}

	private Regex TokenizerRegex = new("\"(.*?)\"|([\\S]*)", RegexOptions.Compiled);
	private List<Token> TempTokens = new();

	public Token[] Tokenize(string Buffer)
	{
		TempTokens.Clear();

		foreach (Match m in TokenizerRegex.Matches(Buffer))
		{
			if (m.Value.Length == 0)
				continue;

			if (m.Value.StartsWith('\"') && m.Value.EndsWith('\"'))
			{
				TempTokens.Add(new Token()
				{
					Content = m.Value.Trim('\"'),
					StartIndex = m.Index,
					Quoted = true,
					HighlightForeground = ConsoleColor.White.ToByte(),
					HighlightBackground = ConsoleColor.Black.ToByte()
				});
			}
			else
			{
				TempTokens.Add(new Token()
				{
					Content = m.Value,
					StartIndex = m.Index,
					Quoted = false,
					HighlightForeground = ConsoleColor.White.ToByte(),
					HighlightBackground = ConsoleColor.Black.ToByte()
				});
			}
		}

		return TempTokens.ToArray();
	}
}
