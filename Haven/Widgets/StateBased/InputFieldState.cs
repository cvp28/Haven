using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace HavenUI;

public partial class InputFieldState
{
	public int X { get; set; } = 0;
	public int Y { get; set; } = 0;
	
	public string Prompt { get; set; } = "";
	public string EmptyMessage = string.Empty;
	
	public byte EmptyMessageForeground { get; set; } = VTColor.Argent;
	
	internal int CurrentBufferIndex = 0;
	internal StringBuilder Buffer = new();
	
	public int BufferLength => Buffer.Length;
	
	public byte CursorForeground { get; set; } = VTColor.Black;
	public byte CursorBackground { get; set; } = VTColor.White;
	
	public List<string> History { get; set; } = new();
	public int CurrentHistoryIndex { get; internal set; } = 0;
	
	public int CursorX => (X + Prompt.Length + CurrentBufferIndex) % Haven.WindowWidth;
	public int CursorY => ((X + Prompt.Length + CurrentBufferIndex) / Haven.WindowWidth) + Y;
	
	public Action<string> OnInputReady { get; set; }
	public Func<string, bool> OnCharInput { get; set; }
	
	public bool DrawCursor { get; set; } = true;
	public int CursorBlinkIntervalMs { get; set; } = 500;
	
	public bool HighlightingEnabled { get; set; } = false;
	public bool HistoryEnabled { get; set; } = true;
	
	public Action<IEnumerable<Token>> OnHighlight { get; set; }
	public Func<Token, IEnumerable<Token>, IEnumerable<string>> OnRetrieveSuggestions { get; set; }
	
	public InputFilter Filter { get; set; } = InputFilter.None;
	
	[GeneratedRegex("\"(.*?)\"|([\\S]*)", RegexOptions.IgnoreCase)]
	internal static partial Regex TokenizerRegex();
	
	internal List<Token> CurrentTokens = new();
	internal List<Token> TempTokens = new();
	internal List<int> SnapPoints = new();
	internal bool InternalDrawCursor = true;
	internal bool InAutoCompleteMode = false;
	internal bool InHistoryMode = false;
	internal string[] AutoCompleteSuggestions;
	internal int CurrentCompletionsIndex = 0;
	internal string BufferBackup;
	internal Stopwatch CursorTimer = new();
	
	public InputFieldState()
	{
		CursorTimer.Start();
		
		Task.Run(delegate
		{
			OuterLoop:
			while (CursorTimer.ElapsedMilliseconds < CursorBlinkIntervalMs)
				Thread.Sleep(1);
			
			InternalDrawCursor = !InternalDrawCursor;
			CursorTimer.Restart();
			
			if (this is not null)
				goto OuterLoop;
		});
	}
	
	public void Clear()
	{	
		Buffer.Clear();
		CurrentBufferIndex = 0;
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
}
