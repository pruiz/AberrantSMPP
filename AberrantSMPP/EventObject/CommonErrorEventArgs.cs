/* RoaminSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 *
 * This file is part of RoaminSMPP.
 *
 * RoaminSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * RoaminSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */


using System;

namespace AberrantSMPP.EventObjects 
{
	/// <summary>
	/// Event args class.  Holds the internal exception generated.
	/// </summary>
	public class CommonErrorEventArgs : System.EventArgs 
	{
		private Exception _exc;

		/// <summary>
		/// The thrown exception.
		/// </summary>
		public Exception ThrownException 
		{
			get 
			{
				return _exc;
			}
		}

		/// <summary>
		/// Creates an CommErrorEventArgs.
		/// </summary>
		/// <param name="exc">The exception that was generated.</param>
		public CommonErrorEventArgs(Exception exc)
		{
			_exc = exc;
		}
	}
}
