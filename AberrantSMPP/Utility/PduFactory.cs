/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz García <pruiz@crt0.net>
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
 * GNU Lessert General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using System.Collections.Generic;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP.Utility
{
	/// <summary>
	/// Takes incoming packets from an input stream and generates
	/// PDUs based on the command field.
	/// </summary>
	public class PduFactory
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public PduFactory()
		{
		}
		
		/// <summary>
		/// Factory method to generate the PDU.
		/// </summary>
		/// <param name="incomingPDUs">The SMSC response.</param>
		/// <returns>The PDU.</returns>
		public Queue GetPduQueue(Queue<byte> incomingPDUs)
		{
			Queue packetQueue = new Queue();
			//get the first packet
			byte[] response = null;
			Pdu packet = null;
			int newLength = 0;
			//this needs to start at zero
			uint CommandLength = 0;
			
			//look for multiple PDUs in the response
			while(incomingPDUs.Count > 0)
			{
				//determine if we have another response PDU after this one
				newLength =(int)(incomingPDUs.Count - CommandLength);
				//could be empty data or it could be a PDU
				if(newLength > 0)
				{
					//get the next PDU
					response = PduUtil.TrimResponsePdu(incomingPDUs);
					//there could be none...
					if(response.Length > 0)
					{
						//get the command length and command ID
						CommandLength = Pdu.DecodeCommandLength(response);
						if(CommandLength > 0)
						{
							try
							{
								//process
								packet = Pdu.Parse(response);
								if (packet != null)
									packetQueue.Enqueue(packet);
							}
							catch (Exception ex)
							{
								Console.Error.WriteLine("PDU Parsing problem " + ex.ToString());
							}
						}
					}
					else
					{
						//kill it off and return
						break;
					}
				}
			}
			
			return packetQueue;
		}
	}
}
