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
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.EventObjects 
{
	/// <summary>
	/// Class that defines the submit_sm_resp event.
	/// </summary>
	public class SubmitSmRespEventArgs : SmppEventArgs<SmppSubmitSmResp> 
	{
		/// <summary>
		/// Sets up the SubmitSmRespEventArgs.
		/// </summary>
		/// <param name="response">The SmppSubmitSmResp.</param>
		internal SubmitSmRespEventArgs(SmppSubmitSmResp response)
			: base(response)
		{
		}
	}
}
