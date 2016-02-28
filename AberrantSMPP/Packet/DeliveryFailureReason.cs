namespace AberrantSMPP.Packet
{
	/// <summary>
	/// Enumerates the delivery failure types.
	/// </summary>
	public enum DeliveryFailureReason : byte
	{
		/// <summary>
		/// DestinationUnavailable
		/// </summary>
		DestinationUnavailable = 0x00,
		/// <summary>
		/// DestinationAddressInvalid
		/// </summary>
		DestinationAddressInvalid = 0x01,
		/// <summary>
		/// PermanentNetworkError
		/// </summary>
		PermanentNetworkError = 0x02,
		/// <summary>
		/// TemporaryNetworkError
		/// </summary>
		TemporaryNetworkError = 0x03
	}
}
