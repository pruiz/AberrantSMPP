/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2010-2022 Pablo Ruiz García <pruiz@netway.org>
 *
 * This file is part of AberrantSMPP.
 *
 * AberrantSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * AberrantSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with AberrantSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using AberrantSMPP.Packet.Response;
using AberrantSMPP.Utility;
using Dawn;

namespace AberrantSMPP.Packet.Request
{
	public abstract class SmppRequest : Pdu
	{
		private TaskCompletionSource<SmppResponse> _taskSource;
		internal Task<SmppResponse> ResponseTask => _taskSource?.Task;
		internal bool ResponseTrackingEnabled => _taskSource != null;

		#region constructors
		
		/// <summary>
		/// Groups construction tasks for subclasses.  Sets source address TON to 
		/// international, source address NPI to ISDN, and source address to "".
		/// </summary>
		protected SmppRequest(): base()
		{}
		
		/// <summary>
		/// Creates a new MessageLcd6 for incoming PDUs.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes to decode.</param>
		protected SmppRequest(byte[] incomingBytes) : base(incomingBytes)
		{}
		#endregion constructors

		protected override void AppendPduData(ArrayList pdu)
		{
			// Do nothing..
		}

		internal Task<SmppResponse> EnableResponseTracking()
		{
			lock (this)
			{
				if (_taskSource == null)
				{
					_taskSource = new TaskCompletionSource<SmppResponse>();
				}
			}

			return _taskSource.Task;
		}

		internal void SetResponse(SmppResponse response)
		{
			Guard.Operation(ResponseTrackingEnabled, "Response Tracking not enabled?!");
			_taskSource.SetResult(response);
		}

        internal void CancelResponse()
        {
            Guard.Operation(ResponseTrackingEnabled, "Response Tracking not enabled?!");
            _taskSource.TrySetCanceled();
        }
    }
}
