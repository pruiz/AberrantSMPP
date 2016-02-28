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
		/// <param name="incomingPdUs">The SMSC response.</param>
		/// <returns>The PDU.</returns>
		public Queue GetPduQueue(byte[] incomingPdUs)
		{
			Queue packetQueue = new Queue();
			//get the first packet
			byte[] response = null;
			Pdu packet = null;
			int newLength = 0;
			//this needs to start at zero
			uint commandLength = 0;
			
			//look for multiple PDUs in the response
			while(incomingPdUs.Length > 0)
			{
				//determine if we have another response PDU after this one
				newLength =(int)(incomingPdUs.Length - commandLength);
				//could be empty data or it could be a PDU
				if(newLength > 0)
				{
					//get the next PDU
					response = Pdu.TrimResponsePdu(incomingPdUs);
					//there could be none...
					if(response.Length > 0)
					{
						//get the command length and command ID
						commandLength = Pdu.DecodeCommandLength(response);
						//trim the packet down so we can look for more PDUs
						long length = incomingPdUs.Length - commandLength;
						byte[] newRemainder = new byte[length];
						Array.Copy(incomingPdUs, commandLength, newRemainder, 0, length);
						incomingPdUs = newRemainder;
						newRemainder = null;
						if(commandLength > 0)
						{
							//process
							packet = GetPdu(response);
							if(packet != null)
								packetQueue.Enqueue(packet);
						}
					}
					else
					{
						//kill it off and return
						incomingPdUs = new Byte[0];
					}
				}
			}
			
			return packetQueue;
		}
		
		/// <summary>
		/// Gets a single PDU based on the response bytes.
		/// </summary>
		/// <param name="response">The SMSC response.</param>
		/// <returns>The PDU corresponding to the bytes.</returns>
		private Pdu GetPdu(byte[] response)
		{
			var commandId = Pdu.DecodeCommandId(response);

			Pdu packet;
			switch(commandId)
			{
				case CommandId.AlertNotification:
					packet = new SmppAlertNotification(response);
					break;
				case CommandId.BindReceiverResp:
				case CommandId.BindTransceiverResp:
				case CommandId.BindTransmitterResp:
					packet = new SmppBindResp(response);
					break;
				case CommandId.CancelSmResp:
					packet = new SmppCancelSmResp(response);
					break;
				case CommandId.DataSmResp:
					packet = new SmppDataSmResp(response);
					break;
				case CommandId.DeliverSm:
					packet = new SmppDeliverSm(response);
					break;
				case CommandId.EnquireLink:
					packet = new SmppEnquireLink(response);
					break;
				case CommandId.EnquireLinkResp:
					packet = new SmppEnquireLinkResp(response);
					break;
				case CommandId.Outbind:
					packet = new SmppOutbind(response);
					break;
				case CommandId.QuerySmResp:
					packet = new SmppQuerySmResp(response);
					break;
				case CommandId.ReplaceSmResp:
					packet = new SmppReplaceSmResp(response);
					break;
				case CommandId.SubmitMultiResp:
					packet = new SmppSubmitMultiResp(response);
					break;
				case CommandId.SubmitSmResp:
					packet = new SmppSubmitSmResp(response);
					break;
				case CommandId.UnbindResp:
					packet = new SmppUnbindResp(response);
					break;
				case CommandId.GenericNack:
					packet = new SmppGenericNack(response);
					break;
				default:
					packet = null;
					break;
			}

			return packet;
		}
	}
}
