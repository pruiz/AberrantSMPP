using System;

namespace AberrantSMPP
{
    public partial class SMPPClient
    {
        public enum States
        {
            Invalid = -1,
            Inactive = 0,
            Connected,
            Binding,
            Bound,
            Unbinding
        }
        
        public class StateChangedEvent
        {
            public States OldState { get; }
            public States NewState { get; }

            /// <summary>
            /// Sets up the SmppEventArgs.
            /// </summary>
            public StateChangedEvent(States oldState, States newState)
            {
                OldState = oldState;
                NewState = newState;
            }
        }
    }
}