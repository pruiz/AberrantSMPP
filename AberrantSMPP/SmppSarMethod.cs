using System;
using System.Collections.Generic;
using System.Text;

namespace AberrantSMPP
{
	/// <summary>
	/// SMS' long message segmentation methods
	/// </summary>
	public enum SmppSarMethod
	{
		/// <summary>
		/// Send message as a single PDU using MessagePayload field.
		/// </summary>
		SendAsPayload = 1,
		/// <summary>
		/// Send message as multiple segments with User Data Headers prepended.
		/// </summary>
		UserDataHeader = 2,
		/// <summary>
		/// Send message as multiple segments using SMPP segmentation properties.
		/// </summary>
		UseSmppSegmentation = 3
	}
}
