using System.Runtime.InteropServices;

namespace HavenUI;

public static class WinMM
{
	[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
	public static extern uint TimeBeginPeriod(uint ms);
	
	[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
	public static extern uint TimeEndPeriod(uint ms);
}
