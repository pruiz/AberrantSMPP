using System;
using System.Threading;
using AberrantSMPP.Packet.Response;

namespace AberrantSMPP
{

    public partial class SMPPClient
    {
        private class RequestState
        {
            public readonly uint SequenceNumber;
            public readonly ManualResetEvent EventHandler;
            public SmppResponse Response { get; set; }

            public RequestState(uint seqno)
            {
                SequenceNumber = seqno;
                EventHandler = new ManualResetEvent(false);
                Response = null;
            }
        }
    }
}