using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace TextDifferenceBenchmarking.Utilities
{
	/// <summary>
	/// Global inlining options. Helps temporarily disable inlining for better profiler output.
	/// </summary>
	internal static class InliningOptions
	{
#if PROFILING
        public const MethodImplOptions ShortMethod = MethodImplOptions.NoInlining;
#else
		public const MethodImplOptions ShortMethod = MethodImplOptions.AggressiveInlining;
#endif
		public const MethodImplOptions ColdPath = MethodImplOptions.NoInlining;
	}
}
