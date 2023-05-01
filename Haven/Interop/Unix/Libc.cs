using System.Runtime.InteropServices;

namespace Haven;

internal unsafe static class Libc
{
	[DllImport("libc", SetLastError = true)]
	internal static extern int write(int fd, byte* buf, uint len);
}
