using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
