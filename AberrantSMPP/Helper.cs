using System;

namespace AberrantSMPP
{
	internal static class Helper
	{
		/// <summary>
		/// Run the specified @delegate avoiding throwing any exception (by catching & ignoring them if any).
		/// </summary>
		/// <param name="delegate">The @delegate.</param>
		public static void ShallowExceptions(Action @delegate)
		{
			try
			{
				@delegate();
			}
			catch
			{
			}
		}
	}
}
