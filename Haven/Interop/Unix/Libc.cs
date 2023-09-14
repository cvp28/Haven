using System.Runtime.InteropServices;

namespace HavenUI;

internal unsafe static class Libc
{
	[DllImport("libc", SetLastError = true, EntryPoint = "write")]
	internal static extern int Write(int fd, byte* buf, uint len);
	
	[DllImport("libc", EntryPoint = "sched_setscheduler")]
	internal static extern int SetScheduler(int pid, int policy, ref sched_param param);
	
	[DllImport("libc", EntryPoint = "sched_getscheduler")]
	internal static extern int GetScheduler(int pid);
	
	[DllImport("libc", EntryPoint = "sched_get_priority_min")]
	internal static extern int SchedulerGetPriorityMin(int policy);
	
	// Scheduling policies taken directly from https://github.com/torvalds/linux/blob/master/include/uapi/linux/sched.h#L112
	internal const int SCHED_NORMAL		= 0;
	internal const int SCHED_FIFO		= 1;
	internal const int SCHED_RR			= 2;
	internal const int SCHED_BATCH		= 3;
	internal const int SCHED_IDLE		= 5;
	internal const int SCHED_DEADLINE	= 6;
	
	internal struct sched_param
	{
		internal int sched_priority;
	}
}
