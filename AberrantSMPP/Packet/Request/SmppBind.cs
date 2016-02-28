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
using System.Text;
using System.Collections;
using AberrantSMPP.Utility;

namespace AberrantSMPP.Packet.Request
{
	/// <summary>
	/// Class for a bind request Pdu.
	/// </summary>
	public class SmppBind : SmppRequest
	{
		#region private fields
		
		private string _systemId = string.Empty;
		private string _password = string.Empty;
		private string _systemType = string.Empty;
		private string _addressRange = string.Empty;
		private SmppVersionType _interfaceVersion = Pdu.SmppVersionType.Version34;
		private TonType _addressTon = Pdu.TonType.International;
		private NpiType _addressNpi = Pdu.NpiType.Isdn;
		private BindingType _bindType = BindingType.BindAsTransceiver;
		
		#endregion private fields
		
		#region constants
		
		private const int IdLength = 15;
		private const int PassLength = 16;
		private const int TypeLength = 12;
		private const int RangeLength = 40;
		
		#endregion constants
		
		#region mandatory parameters
		protected override CommandId DefaultCommandId { get { return CommandId.BindTransmitter; } }

		/// <summary>
		/// The binding type: transmitter, receiver, or transceiver.
		/// </summary>
		public BindingType BindType
		{
			get
			{
				return _bindType;
			}
			set
			{
				switch(value)
				{
					case BindingType.BindAsReceiver:
					{
						CommandId = CommandId.BindReceiver;
						break;
					}
					case BindingType.BindAsTransceiver:
					{
						CommandId = CommandId.BindTransceiver;
						break;
					}
					case BindingType.BindAsTransmitter:
					{
						CommandId = CommandId.BindTransmitter;
						break;
					}
					default:
					{
						CommandId = CommandId.BindTransmitter;
						break;
					}	
				}
				
				_bindType = value;
			}
		}
		
		/// <summary>
		/// The ESME system requesting to bind with the SMSC.  Set to null for default value.
		/// </summary>
		public string SystemId
		{
			get
			{
				return _systemId;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= IdLength)
						_systemId = value;
					else
						throw new ArgumentOutOfRangeException("System ID must be <= "+ 
							IdLength + " characters).");
				}
				else
					_systemId = "";
			}
		}
		
		/// <summary>
		/// Used by the SMSC to authenticate the ESME requesting to bind.  
		/// Set to null for default value.
		/// </summary>
		public string Password
		{
			get
			{
				return _password;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= PassLength)
					{
						_password = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException("Password must be <= "+ PassLength + " characters).");
					}
				}
				else
				{
					_password = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// The type of ESME system requesting to bind with the SMSC.  
		/// Set to null for default value.
		/// </summary>
		public string SystemType
		{
			get
			{
				return _systemType;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= TypeLength)
					{
						_systemType = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException("System type must be <= "+ TypeLength + " characters).");
					}
				}
				else
				{
					_systemType = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// The ESME address range.  If not known, set to null.
		/// </summary>
		public string AddressRange
		{
			get
			{
				return _addressRange;
			}
			set
			{
				if(value != null)
				{
					if(value.Length <= RangeLength)
					{
						_addressRange = value;
					}
					else
					{
						throw new ArgumentOutOfRangeException("Address range must be <= "+ RangeLength + " characters).");
					}
				}
				else
				{
					_addressRange = string.Empty;
				}
			}
		}
		
		/// <summary>
		/// The version of the SMPP protocol supported by the ESME.
		/// </summary>
		public SmppVersionType InterfaceVersion
		{
			get
			{
				return _interfaceVersion;
			}
			set
			{
				_interfaceVersion = value;
			}
		}
		
		/// <summary>
		/// Indicates type of number of ESME address.
		/// </summary>
		public TonType AddressTon
		{
			get
			{
				return _addressTon;
			}
			set
			{
				_addressTon = value;
			}
		}
		
		/// <summary>
		/// Numbering plan indicator for ESME address.
		/// </summary>
		public NpiType AddressNpi
		{
			get
			{
				return _addressNpi;
			}
			set
			{
				_addressNpi = value;
			}
		}
		
		#endregion mandatory parameters
		
		#region enumerations
		
		/// <summary>
		/// Binding types for the SMPP bind request.
		/// </summary>
		public enum BindingType : uint
		{
			/// <summary>
			/// BindAsReceiver
			/// </summary>
			BindAsReceiver = 1,
			/// <summary>
			/// BindAsTransmitter
			/// </summary>
			BindAsTransmitter = 2,
			/// <summary>
			/// BindAsTransceiver
			/// </summary>
			BindAsTransceiver = 9
		}
		
		#endregion enumerations
		
		#region constructors
		
		/// <summary>
		/// Constructs a bind request.  Sets system ID, password, system type, and address 
		/// range to empty strings.  Sets interface version to v3.4, address TON to 
		/// international, address NPI to ISDN, and sets to bind as a transceiver.
		/// </summary>
		public SmppBind(): base()
		{
			BindType = BindingType.BindAsTransceiver;
		}
		
		/// <summary>
		/// Constructs a bind request.  Sets system ID, password, system type, and address 
		/// range to empty strings.  Sets interface version to v3.4, address TON to 
		/// international, address NPI to ISDN, and sets to bind as a transceiver.
		/// </summary>
		/// <param name="incomingBytes">The incoming bytes from an ESME.</param>
		public SmppBind(byte[] incomingBytes): base(incomingBytes)
		{
		}
		
		#endregion constructors
		
		protected override void AppendPduData(ArrayList pdu)
		{
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SystemId)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(Password)));
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(SystemType)));
			pdu.Add((byte)InterfaceVersion);
			pdu.Add((byte)AddressTon);
			pdu.Add((byte)AddressNpi);
			pdu.AddRange(SmppStringUtil.ArrayCopyWithNull(Encoding.ASCII.GetBytes(AddressRange)));
		}
		
		/// <summary>
		/// This decodes the submit_sm Pdu.  The Pdu has basically the same format as
		/// the submit_sm Pdu, but in this case it is a response.
		/// </summary>
		protected override void DecodeSmscResponse()
		{
			byte[] remainder = BytesAfterHeader;
			BindType = (SmppBind.BindingType)CommandId;
			SystemId = SmppStringUtil.GetCStringFromBody(ref remainder);
			Password = SmppStringUtil.GetCStringFromBody(ref remainder);
			SystemType = SmppStringUtil.GetCStringFromBody(ref remainder);
			InterfaceVersion =(SmppVersionType)remainder[0];
			AddressTon =(TonType)remainder[1];
			AddressNpi =(NpiType)remainder[2];
			AddressRange = SmppStringUtil.GetCStringFromBody(ref remainder, 3);
			TranslateTlvDataIntoTable(remainder);
		}
	}
}
