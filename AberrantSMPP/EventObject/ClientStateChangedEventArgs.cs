namespace AberrantSMPP.EventObjects
{
    /// <summary>
    /// Class to provide notifications on client state changed.
    /// </summary>
    public class ClientStateChangedEventArgs : SmppEventArgs
    {
        public SMPPClient.States OldState { get; }
        public SMPPClient.States NewState { get; }

        /// <summary>
        /// Sets up the SmppEventArgs.
        /// </summary>
        public ClientStateChangedEventArgs(SMPPClient.States oldState, SMPPClient.States newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}