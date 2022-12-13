using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;


namespace HermaFx.Utils
{
	public class EnvironmentHelper
	{
		private static global::Common.Logging.ILog _Log = global::Common.Logging.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly Lazy<bool> _RunningOnMono = new Lazy<bool>(DetectMono);
		private static readonly Lazy<bool> _RunningOnMkbundle = new Lazy<bool>(DetectMkbundle);
		private static readonly Lazy<bool> _RunningOnWindows = new Lazy<bool>(DetectWindows);
		private static readonly Lazy<bool> _RunningOnDotNet = new Lazy<bool>(DetectDotNet);
		private static readonly Lazy<bool> _RunningOnNetFx = new Lazy<bool>(() => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"));
		private static readonly Lazy<bool> _RunningOnNetCore = new Lazy<bool>(() => RuntimeInformation.FrameworkDescription == ".NET Core");
		private static readonly Lazy<bool> _RunningOnNetNative = new Lazy<bool>(() => RuntimeInformation.FrameworkDescription == ".NET Native");

		static bool runningOnUnix, runningOnMacOS, runningOnLinux;
		volatile static bool initialized;
		readonly static object InitLock = new object();

		/// <summary>Gets a System.Boolean indicating whether running on a Windows platform.</summary>
		public static bool RunningOnWindows => _RunningOnWindows.Value;
		/// <summary>Gets a System.Boolean indicating wheter running under .NET 5+ runtime.</summary>
		public static bool RunningOnDotNet => _RunningOnDotNet.Value;
		/// <summary>Gets a System.Boolean indicating wheter running under .NET Framework runtime.</summary>
		public static bool RunningOnNetFx => _RunningOnNetFx.Value;
		/// <summary>Gets a System.Boolean indicating wheter running under .NET Core (1.0 ~ 3.1) runtime.</summary>
		public static bool RunningOnNetCore => _RunningOnNetCore.Value;
		/// <summary>Gets a System.Boolean indicating wheter running under .NET Native runtime.</summary>
		public static bool RunningOnNetNative => _RunningOnNetNative.Value;
		/// <summary> Gets a System.Boolean indicating whether running on the Mono runtime. </summary>
		public static bool RunningOnMono => _RunningOnMono.Value;
		/// <summary> Gets a System.Boolean indicating whether running on an mkbundle package under the Mono runtime. </summary>
		public static bool RunningOnMkbundle => _RunningOnMkbundle.Value;
		/// <summary> Gets a <see cref="System.Boolean"/> indicating whether running on a Unix platform. </summary>
		public static bool RunningOnUnix { get { Init(); return runningOnUnix; } }
		/// <summary>Gets a System.Boolean indicating whether running on an Linux platform.</summary>
		public static bool RunningOnLinux { get { Init(); return runningOnLinux; } }
		/// <summary>Gets a System.Boolean indicating whether running on a MacOS platform.</summary>
		public static bool RunningOnMacOS { get { Init(); return runningOnMacOS; } }
		/// <summary>Gets a System.Boolean indicating whether running on 64 bit OS.</summary>
		public static bool RunningIn64Bits { get { return IntPtr.Size == 8; } }

		#region Private Methods

		private static bool DetectWindows()
		{
			return
				System.Environment.OSVersion.Platform == PlatformID.Win32NT ||
				System.Environment.OSVersion.Platform == PlatformID.Win32S ||
				System.Environment.OSVersion.Platform == PlatformID.Win32Windows ||
				System.Environment.OSVersion.Platform == PlatformID.WinCE;
		}

		private static bool DetectDotNet()
		{
			return RuntimeInformation.FrameworkDescription.StartsWith(".NET ")
				&& RuntimeInformation.FrameworkDescription[5] > '0'
				&& RuntimeInformation.FrameworkDescription[5] < '9';
		}

		private static bool DetectMono()
		{
			// Detect the Mono runtime (code taken from http://mono.wikia.com/wiki/Detecting_if_program_is_running_in_Mono).
			Type t = Type.GetType("Mono.Runtime");
			return t != null;
		}

		private static bool DetectMkbundle()
		{
			if (!RunningOnMono)
				return false;

			// When running on mkbundle assembly location will only return assembly name instead of full path
			return !Assembly.GetExecutingAssembly().Location.Contains(Path.DirectorySeparatorChar);
		}

		private static void Init()
		{
			lock (InitLock)
			{
				if (!initialized)
				{
					initialized = true;

					if (!RunningOnWindows)
					{
						DetectUnix(ref runningOnUnix, ref runningOnLinux, ref runningOnMacOS);
					}
				}
			}
		}

		#region private static string DetectUnixKernel()

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct utsname
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string sysname;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string nodename;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string release;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string version;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string machine;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string extraJustInCase;
		}

		[DllImport("libc")]
		private static extern void uname(out utsname uname_struct);

		/// <summary>
		/// Detects the unix kernel by p/invoking uname (libc).
		/// </summary>
		/// <returns></returns>
		private static string DetectUnixKernel()
		{
			utsname uts = new utsname();
			uname(out uts);

			if (_Log.IsDebugEnabled)
			{
				_Log.Debug($"System: {uts.sysname}, {uts.nodename}, {uts.release}, {uts.version}, {uts.machine}");
			}

			return uts.sysname;
		}

		private static void DetectUnix(ref bool unix, ref bool linux, ref bool macos)
		{
			string kernel_name = DetectUnixKernel();
			switch (kernel_name)
			{
				case null:
				case "":
					throw new PlatformNotSupportedException("Unknown platform?!");

				case "Linux":
					linux = unix = true;
					break;

				case "Darwin":
					macos = unix = true;
					break;

				default:
					unix = true;
					break;
			}
		}
		#endregion

		#endregion
	}
}
