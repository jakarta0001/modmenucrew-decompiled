using System;
using System.Diagnostics;

namespace ModMenuCrew;

internal static class MemoryMonitor
{
	internal static bool _isMonitoring { get; private set; }

	internal static void CheckMemory()
	{
		try
		{
			Process currentProcess = Process.GetCurrentProcess();
			if (currentProcess.ProcessName.Contains("debugger") || currentProcess.ProcessName.Contains("monitor"))
			{
				_isMonitoring = true;
			}
		}
		catch
		{
		}
	}

	internal static void VerifyMemory()
	{
		GC.GetTotalMemory(forceFullCollection: false);
		_ = 536870912;
	}
}
