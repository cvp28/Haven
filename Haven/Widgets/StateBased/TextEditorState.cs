using System.Text;

namespace HavenUI;

public class TextEditorState
{
	// Coords and Dimensions are piggy-backed off of the textbox - which represents the entire displayable portion of the widget
	public int X => sTextBox.X;
	public int Y => sTextBox.Y;
	
	public int Width => sTextBox.Width;
	public int Height => sTextBox.Height;
	
	public int CurrentLineIndex = 0;
	public int CurrentBufferIndex = 0;
	
	internal List<StringBuilder> Buffer = new();
	
	internal int CursorX = 0;
	internal int CursorY = 0;
	
	internal ScrollableTextBoxState sTextBox;
	
	public TextEditorState(int X, int Y, int Width, int Height)
	{
		sTextBox = new(X, Y, Width, Height);
		
		// Start with one empty line
		Buffer.Add(new());
	}
	
	internal void AppendChar(char Character)
	{
		Buffer[CurrentLineIndex].Insert(CurrentBufferIndex, Character);
		CurrentBufferIndex++;
	}
	
	internal void Newline()
	{
		if (CurrentLineIndex == Buffer.Count - 1)
		{
			Buffer.Add(new());
			CurrentLineIndex++;
			CurrentBufferIndex = 0;
		}
		else
		{
			Buffer.Insert(CurrentLineIndex, new());
			CurrentLineIndex++;
			CurrentBufferIndex = 0;
		}
	}
	
	internal void Delete()
	{
		if (CurrentBufferIndex == Buffer[CurrentLineIndex].Length - 1)
			return;
		
		Buffer[CurrentLineIndex].Remove(CurrentBufferIndex, 1);
	}
	
	internal void Backspace()
	{
		if (CurrentBufferIndex == 0)
		{
			
			
			return;
		}
		
		Buffer[CurrentLineIndex].Remove(CurrentBufferIndex - 1, 1);
		CurrentBufferIndex--;
	}
	
	internal void UpdateCursor()
	{
		int OffsetX = CurrentBufferIndex % Width;
		int OffsetY = CurrentLineIndex;
		
		for (int i = 0; i < CurrentLineIndex + 1; i++)
			OffsetY += Buffer[i].Length / Width;
		
		sTextBox.CursorX = OffsetX;
		sTextBox.CursorY = OffsetY;
	}
	
	private (int X, int Y) GetCoordsFromOffset(int Offset, int X = 0, int Y = 0)
	{
		bool YOutOfBounds = Y < 0 || Y >= sTextBox.BufferHeight;
		
		if (YOutOfBounds)
			return (0, 0);
		
		int TempX = X;
		int TempY = Y;
		
		for (int i = 0; i < Offset; i++)
			if (TempX == sTextBox.Width - 1)
			{
				TempX = 0;
				TempY++;
			}
			else
			{
				TempX++;
			}
		
		return (TempX, TempY);
	}
}
