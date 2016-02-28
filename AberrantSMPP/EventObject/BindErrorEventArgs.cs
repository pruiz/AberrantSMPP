using System;

namespace AberrantSMPP.EventObjects
{
	public class BindErrorEventArgs : CommonErrorEventArgs
	{
		public BindErrorEventArgs(Exception exception)
			: base(exception)
		{
		}
	}
}
