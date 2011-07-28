using System;
using System.Reactive.Concurrency;

namespace EPS.Concurrency
{
	internal static class LocalScheduler
	{
		//simply used to insulate us from the .NET 4 / SL 4 differences
		public static IScheduler Default
		{
			get {
#if SILVERLIGHT
				return Scheduler.ThreadPool;
#else
				return Scheduler.TaskPool;
#endif
			}
		}
	}
}