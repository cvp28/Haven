using System.Runtime.InteropServices;

namespace Haven;

[StructLayout(LayoutKind.Sequential)]
internal struct COORD
{
    public short X;
    public short Y;

    public COORD(short X, short Y)
    {
        this.X = X;
        this.Y = Y;
    }
};

[StructLayout(LayoutKind.Explicit)]
internal struct CharUnion
{
    [FieldOffset(0)] public ushort UnicodeChar;
    [FieldOffset(0)] public byte AsciiChar;
}

[StructLayout(LayoutKind.Explicit)]
internal struct CharInfo
{
    [FieldOffset(0)] public CharUnion Char;
    [FieldOffset(2)] public short Attributes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SmallRect
{
    public short Left;
    public short Top;
    public short Right;
    public short Bottom;
}

internal class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool WriteConsoleOutputW(IntPtr hConsoleOutput, CharInfo[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SmallRect lpWriteRegion);
}