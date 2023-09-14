using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HavenUI;

public static partial class Haven
{
	public static void Text(int X, int Y, string Text, byte Foreground = 15, byte Background = 0, bool Invert = false)
	{
		RenderContext.VTSetCursorPosition(X, Y);
		RenderContext.VTEnterColorContext(Foreground, Background, delegate
		{
			if (Invert)
				RenderContext.VTInvert();
				
			RenderContext.VTDrawText(Text);
			
			if (Invert)
				RenderContext.VTRevert();
		});
	}
	
	public static Maybe<string> InputField(InputFieldState State, string InputHandle)
	{
		Maybe<string> InputFieldResult = Maybe<string>.Fail();
		
		#region User Input
		while (KeyAvailable(InputHandle))
		{
			var cki = GetKey(InputHandle).Result;
			
			if (cki.Key != ConsoleKey.Tab)
				State.InAutoCompleteMode = false;
			
			if (cki.Key != ConsoleKey.UpArrow && cki.Key != ConsoleKey.DownArrow)
			{
				State.InHistoryMode = false;
				State.CurrentHistoryIndex = 0;
			}
			
			// If any key is pressed, ensure cursor visibility and restart the cursor timer
			State.InternalDrawCursor = true;
			State.CursorTimer.Restart();
			
			bool RaiseInputEvent = false;
			
			switch (cki.Key)
			{
				case ConsoleKey.Tab:
					var SelectedToken = State.CurrentTokens.FirstOrDefault(t => t.Selected);

					// If there is no currently selected token, break
					if (SelectedToken is null)
					{
						State.CurrentCompletionsIndex = 0;
						State.InAutoCompleteMode = false;
						break;
					}

					int Index = State.CurrentTokens.IndexOf(SelectedToken);

					// If token is not found in list (for whatever reason), break
					if (Index == -1)
					{
						State.CurrentCompletionsIndex = 0;
						State.InAutoCompleteMode = false;
						break;
					}

					// If suggestions handler is null, break
					if (State.OnRetrieveSuggestions is null)
					{
						State.CurrentCompletionsIndex = 0;
						State.InAutoCompleteMode = false;
						break;
					}

					// This is ugly, I know
					void SetCurrentToken(string NewTokenContent, bool InsertQuotes)
					{
						if (SelectedToken.Quoted)
						{
							State.Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length + 2);

							if (InsertQuotes)
							{
								State.Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
								State.CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
							}
							else
							{
								State.Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
								State.CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
							}

						}
						else
						{
							State.Buffer.Remove(SelectedToken.StartIndex, SelectedToken.Content.Length);

							if (InsertQuotes)
							{
								State.Buffer.Insert(SelectedToken.StartIndex, $"\"{NewTokenContent}\"");
								State.CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length + 2;
							}
							else
							{
								State.Buffer.Insert(SelectedToken.StartIndex, NewTokenContent);
								State.CurrentBufferIndex = SelectedToken.StartIndex + NewTokenContent.Length;
							}
						}
					}

					if (!State.InAutoCompleteMode)
					{
						State.CurrentCompletionsIndex = 0;
						State.InAutoCompleteMode = true;

						var temp = State.OnRetrieveSuggestions(SelectedToken, State.CurrentTokens)?.ToArray();

						// If the caller returned null (for whatever reason) or the returned container did not have any elements
						if (temp is null || !temp.Any())
						{
							State.CurrentCompletionsIndex = 0;
							State.InAutoCompleteMode = false;
							break;
						}

						State.AutoCompleteSuggestions = temp;
					}
					else
					{
						if (State.CurrentCompletionsIndex == State.AutoCompleteSuggestions.Length - 1)
							State.CurrentCompletionsIndex = 0;
						else
							State.CurrentCompletionsIndex++;
					}
					
					SetCurrentToken(State.AutoCompleteSuggestions[State.CurrentCompletionsIndex], State.AutoCompleteSuggestions[State.CurrentCompletionsIndex].Contains(' '));
					break;
				
				case ConsoleKey.UpArrow:
					if (State.History.Count == 0)
						break;

					if (!State.InHistoryMode)
					{
						State.BufferBackup = State.Buffer.ToString();
						State.InHistoryMode = true;

						if (State.History.Count > 0)
							State.CurrentHistoryIndex = State.History.Count - 1;
					}
					else
					{
						if (State.History.Count > 0)
							State.CurrentHistoryIndex = DecrementInRange(State.CurrentHistoryIndex, -1, State.History.Count - 1);
						else
							break;
					}

					if (State.CurrentHistoryIndex == -1)
					{
						State.Buffer.Clear();
						State.Buffer.Insert(0, State.BufferBackup);

						if (State.Buffer.Length == 0)
							State.CurrentBufferIndex = 0;
						else
							State.CurrentBufferIndex = State.Buffer.Length;

						State.InHistoryMode = false;
					}
					else
					{
						State.Buffer.Clear();
						State.Buffer.Insert(0, State.History[State.CurrentHistoryIndex]);
						State.CurrentBufferIndex = State.Buffer.Length;
					}
					break;
				
				case ConsoleKey.DownArrow:
					if (State.History.Count == 0)
						break;

					if (!State.InHistoryMode)
					{
						State.BufferBackup = State.Buffer.ToString();
						State.InHistoryMode = true;

						State.CurrentHistoryIndex = 0;
					}
					else
					{
						if (State.History.Count > 0)
							State.CurrentHistoryIndex = IncrementInRange(State.CurrentHistoryIndex, -1, State.History.Count - 1);
						else
							break;
					}

					if (State.CurrentHistoryIndex == -1)
					{
						State.Buffer.Clear();
						State.Buffer.Insert(0, State.BufferBackup);

						if (State.Buffer.Length == 0)
							State.CurrentBufferIndex = 0;
						else
							State.CurrentBufferIndex = State.Buffer.Length;

						State.InHistoryMode = false;
					}
					else
					{
						State.Buffer.Clear();
						State.Buffer.Insert(0, State.History[State.CurrentHistoryIndex]);
						State.CurrentBufferIndex = State.Buffer.Length;
					}
					break;
				
				case ConsoleKey.LeftArrow:
					if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
					{
						var ValidSnapPoints = State.SnapPoints.Where(p => p < State.CurrentBufferIndex);
						
						if (ValidSnapPoints.Any())
						{
							State.CurrentBufferIndex = ValidSnapPoints.Last();
							break;
						}
						else
							break;
					}
					
					CursorLeft();
					break;
				
				case ConsoleKey.RightArrow:
					if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
					{		
						var ValidSnapPoints = State.SnapPoints.Where(p => p > State.CurrentBufferIndex);
						
						if (ValidSnapPoints.Any())
						{
							State.CurrentBufferIndex = ValidSnapPoints.First();
							break;
						}
						else
							break;
					}
					
					CursorRight();
					break;
				
				case ConsoleKey.Delete:
					if ((cki.Modifiers & ConsoleModifiers.Control) != 0)
					{
						State.Clear();
						break;
					}
					
					InverseBackspace();
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
				
				case ConsoleKey.Enter:
					RaiseInputEvent = true;
					break;
				
				default:
					char c = cki.KeyChar;
					bool Valid = false;

					switch (State.Filter)
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
							if (State.Buffer.ToString().Contains('.'))
								Valid = char.IsDigit(c);
							else
								Valid = char.IsDigit(c) || c == '.';

							break;
					}

					if (!Valid)
						break;

					State.Buffer.Insert(State.CurrentBufferIndex, c);
					State.CurrentBufferIndex++;
					break;
			}
			
			if (State.OnCharInput is not null && State.OnCharInput(State.Buffer.ToString()))
				RaiseInputEvent = true;
			
			if (RaiseInputEvent)
			{
				string Result = State.Buffer.ToString();
				InputFieldResult = Maybe<string>.Success(Result);
				
				State.OnInputReady?.Invoke(Result);
				
				if (Result.Length > 0 && State.HistoryEnabled)
					State.History.Add(Result);
				
				State.Buffer.Clear();
				State.CurrentBufferIndex = 0;
			}
			
			// Clear current tokens list
			State.CurrentTokens.Clear();
			
			// Clear cursor snap point list (Makes CTRL+LeftArrow/CTRL+RightArrow behavior work)
			State.SnapPoints.Clear();
			
			// Tokenize buffer
			var tokens = Tokenize(State.Buffer.ToString());
			
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

				if (State.CurrentBufferIndex >= CurrentToken.StartIndex && State.CurrentBufferIndex < CurrentToken.StartIndex + CurrentToken.Content.Length + Offset)
					CurrentToken.Selected = true;

				State.CurrentTokens.Add(CurrentToken);
				
				// Add a cursor snap point at the start and end of this token
				State.SnapPoints.Add(CurrentToken.StartIndex);
				State.SnapPoints.Add(CurrentToken.StartIndex + CurrentToken.FullLength - 1);
			}
			
			// Add a cursor snap point at the end of the buffer
			State.SnapPoints.Add(State.Buffer.Length);

			// Perform syntax highlighting via user-set rules if enabled
			if (State.HighlightingEnabled && State.OnHighlight is not null)
				State.OnHighlight(State.CurrentTokens);
		}
		#endregion
		
		#region Drawing
		RenderContext.VTSetCursorPosition(State.X, State.Y);
		RenderContext.VTDrawText(State.Prompt);
		
		// Draw EmptyMessage if set
		if (State.Buffer.Length == 0 && State.EmptyMessage is not null && State.EmptyMessage.Length != 0)
		{
			RenderContext.VTEnterColorContext(State.EmptyMessageForeground, VTColor.Black, delegate
			{
				RenderContext.VTDrawText(State.EmptyMessage);
			});
		}
		
		byte InitialForeground = VTRenderContext.CurrentForegroundColor;
		byte InitialBackground = VTRenderContext.CurrentBackgroundColor;
		
		// Draw buffer using tokens from tokenizer
		foreach (var Token in State.CurrentTokens)
		{
			if (State.HighlightingEnabled)
				RenderContext.VTSetColors(Token.HighlightForeground, Token.HighlightBackground);
			
			var (TokenX, TokenY) = GetCoordsFromOffset(Token.StartIndex, State.X + State.Prompt.Length, State.Y);
			
			RenderContext.VTSetCursorPosition(TokenX, TokenY);
			RenderContext.VTDrawText(Token.RawContent);
			
			// If this is the last token, reset the colors
			if (Token == State.CurrentTokens.Last())
				RenderContext.VTSetColors(InitialForeground, InitialBackground);
		}
		
		// Draw cursor if visible
		if (State.DrawCursor && State.InternalDrawCursor)
		{
			RenderContext.VTSetCursorPosition(State.CursorX, State.CursorY);
			
			RenderContext.VTInvert();
			
			if (State.Buffer.Length > 0 && State.CurrentBufferIndex != State.Buffer.Length)
				RenderContext.VTDrawText($"{State.Buffer[State.CurrentBufferIndex]}");
			else if (State.Buffer.Length == 0 && State.EmptyMessage is not null && State.EmptyMessage.Length > 0)
				RenderContext.VTDrawChar(State.EmptyMessage[0]);
			else
				RenderContext.VTDrawChar(' ');
			
			RenderContext.VTRevert();
		}
		#endregion
		
		return InputFieldResult;
		
		#region Internal Functions
		Token[] Tokenize(string Buffer)
		{
			State.TempTokens.Clear();
			
			foreach (Match m in InputFieldState.TokenizerRegex().Matches(Buffer).Cast<Match>())
			{
				if (m.Value.Length == 0)
					continue;
				
				State.TempTokens.Add(new ()
				{
					StartIndex = m.Index,
					Content = m.Value.Trim('\"'),
					RawContent = m.Value,
					
					Quoted = m.Value.StartsWith('\"') && m.Value.EndsWith('\"'),
					
					HighlightForeground = VTColor.White,
					HighlightBackground = VTColor.Black
				});
			}
			
			return State.TempTokens.ToArray();
		}
		
		void CursorToStart()
		{
			State.CurrentBufferIndex = 0;
		}

		void CursorToEnd()
		{
			State.CurrentBufferIndex = State.Buffer.Length;
		}

		void CursorLeft()
		{
			if (State.CurrentBufferIndex == 0)
				return;
			
			State.CurrentBufferIndex--;
		}

		void CursorRight()
		{
			if (State.CurrentBufferIndex == State.Buffer.Length)
				return;
			
			State.CurrentBufferIndex++;
		}
		
		void InverseBackspace()
		{
			if (State.CurrentBufferIndex == State.Buffer.Length)
				return;
			
			State.Buffer.Remove(State.CurrentBufferIndex, 1);
		}
		
		void Backspace(bool Force = false)
		{
			if (!Force)
				if (State.CurrentBufferIndex == 0)
					return;
			
			// Remove 1 character starting at the current buffer index
			State.Buffer.Remove(State.CurrentBufferIndex - 1, 1);
			CursorLeft();
		}
		
		int IncrementInRange(int Value, int LowerBound, int UpperBound)
		{
			if (Value < UpperBound)
				return Value + 1;
			else
				return LowerBound;
		}
		
		int DecrementInRange(int Value, int LowerBound, int UpperBound)
		{
			if (Value > LowerBound)
				return Value - 1;
			else
				return UpperBound;
		}
		#endregion
	}
	
	public static void ScrollableTextBox(ScrollableTextBoxState State)
	{
		if (State.Height <= 0 || State.Width <= 0)
			return;

		int IndexStart = State.IX(0, State.ViewY);
		int Length = State.IX(0, State.ViewY + State.Height) - IndexStart;

		int ViewYMax = State.BufferHeight - State.Height - 1;
		double ScrollbarPercentage = State.ViewY /  (double) ViewYMax;

		// Draw window
		RenderContext.VTDrawBox(State.X, State.Y,State.Width + 2, State.Height + 2);

		lock (ScrollableTextBoxState.ScreenBufferLock)
		{
			var BufferToRender = CollectionsMarshal.AsSpan(State.ScreenBuffer).Slice(IndexStart, Length);

			// Draw screen buffer
			RenderContext.VTDrawCharacterInfoBuffer(State.X + 1, State.Y + 1, State.Width, State.Height, State.Width, in BufferToRender);

			// Draw scrollbar
			if (State.ScrollbarVisible)
			{
				RenderContext.VTSetCursorPosition(State.X + State.Width + 1, State.Y + 1 + (int)Remap(ScrollbarPercentage, 0.0, 1.0, 0.0, (double)State.Height - 1));

				RenderContext.VTEnterColorContext(State.ScrollbarColor, ConsoleColor.Black.ToByte(), delegate ()
				{
					RenderContext.VTDrawChar(BoxChars.Vertical);
				});
			}

			// Draw cursor
			if (State.IsCursorInView() && State.CursorVisible && State.DoCursor)
			{
				RenderContext.VTSetCursorPosition(State.X + 1 + State.CursorX, State.Y + 1 + (State.CursorY - State.ViewY));
				
				RenderContext.VTInvert();
				RenderContext.VTDrawChar(State.CellUnderCursor.RenderingCharacter);	
				RenderContext.VTRevert();
			}
		}
		
		static double Remap(double value, double from1, double to1, double from2, double to2) => (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}
	
	public static Maybe<MenuOption> Menu(MenuState State, string InputHandle)
	{
		var MenuResult = Maybe<MenuOption>.Fail();
		
		var cki = GetKey(InputHandle);
		
		if (cki)
		{	
			if (State.Options.Count == 0)
				return Maybe<MenuOption>.Fail();
			
			switch (cki.Result.Key)
			{
				case ConsoleKey.UpArrow:
					if (State.SelectedOption == 0)
						State.SelectedOption = State.Options.Count - 1;
					else
						State.SelectedOption--;
					break;

				case ConsoleKey.DownArrow:
					if (State.SelectedOption == State.Options.Count - 1)
						State.SelectedOption = 0;
					else
						State.SelectedOption++;
					break;

				case ConsoleKey.Enter:
					State[State.SelectedOption].Action();
					
					MenuResult = Maybe<MenuOption>.Success(State[State.SelectedOption]);
					break;
			}
		}
	
		if (State.Options.Count == 0)
			return Maybe<MenuOption>.Fail();

		switch (State.TextAlignment)
		{
			case Alignment.Center:
				DrawCenterAligned();
				break;

			default:
			case Alignment.Left:
				DrawLeftAligned();
				break;
		}
		
		return MenuResult;

		void DrawLeftAligned()
		{
			for (int i = 0; i < State.Options.Count; i++) 
			{
				if (i == State.SelectedOption)
					DrawStyledOption(State.X, State.Y + i, State.Options[i]);
				else
				{
					RenderContext.VTSetCursorPosition(State.X, State.Y + i);
					RenderContext.VTEnterColorContext(State.Options[i].TextForeground, State.Options[i].TextBackground, delegate ()
					{
						RenderContext.VTDrawText(State.Options[i].Text);
					});
				}
			}
		}

		void DrawCenterAligned()
		{
			int LongestOptionLength = State.Options.Max(op => op.Text.Length);

			for (int i = 0; i < State.Options.Count; i++)
			{
				int CurrentX = State.X + (LongestOptionLength / 2) - (State.Options[i].Text.Length / 2);

				if (i == State.SelectedOption)
					DrawStyledOption(CurrentX, State.Y + i, State.Options[i]);
				else
				{
					RenderContext.VTSetCursorPosition(CurrentX, State.Y + i);
					RenderContext.VTEnterColorContext(State.Options[i].TextForeground, State.Options[i].TextBackground, delegate ()
					{
						RenderContext.VTDrawText(State.Options[i].Text);
					});
				}
			}
		}

		void DrawStyledOption(int X, int Y, MenuOption Option)
		{
			if (!State.DoStyle)
			{
				RenderContext.VTSetCursorPosition(X, Y);
				RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
				{
					RenderContext.VTDrawText(Option.Text);
				});
				return;
			}

			switch (State.SelectedOptionStyle)
			{
				case MenuStyle.Arrow:
					{
						RenderContext.VTSetCursorPosition(X, Y);
						RenderContext.VTEnterColorContext(Option.TextForeground, Option.TextBackground, delegate ()
						{
							RenderContext.VTDrawText(Option.Text);
							RenderContext.VTDrawChar(' ');
							RenderContext.VTDrawChar('<');
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
	}
	
	public static void TextEdit(TextEditorState State, string InputHandle)
	{
		while (KeyAvailable(InputHandle))
		{
			var cki = GetKey(InputHandle).Result;
			
			switch (cki.Key)
			{
				case ConsoleKey.UpArrow:
					
					break;
				
				case ConsoleKey.DownArrow:
					
					break;
				
				case ConsoleKey.LeftArrow:
					
					break;
				
				case ConsoleKey.RightArrow:
					
					break;
				
				case ConsoleKey.Enter:
					State.Newline();
					break;
				
				case ConsoleKey.Backspace:
					State.Backspace();
					break;
				
				case ConsoleKey.Delete:
					State.Delete();
					break;
				
				default:
					var c = cki.KeyChar;
					
					bool Valid = char.IsWhiteSpace(c) || char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c);
					
					if (Valid)
						State.AppendChar(c);
					break;
				
			}
		}
		
		State.sTextBox.Clear();
		
		foreach (var Line in State.Buffer)
			State.sTextBox.WriteLine(Line.ToString());
		
		State.UpdateCursor();
		
		ScrollableTextBox(State.sTextBox);
		
	}
	
	public static void BulletList(BulletListState State)
	{
		int YOffset = 0;

		for (int i = 0; i < State.Children.Count; i++)
			YOffset += State.Children.Values.ElementAt(i).Render(State.X, State.Y + i + YOffset, RenderContext, State.ListBackground, State.TabWidth);
	}
}
