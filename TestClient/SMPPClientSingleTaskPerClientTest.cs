using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;
using AberrantSMPP;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using AberrantSMPP.Exceptions;
using AberrantSMPP.Packet;
using System.Threading;
using AberrantSMPP.EventObjects;

namespace TestClient
{
	internal class SMPPClientSingleTaskPerClientTest : SMPPClientTestsBase
	{
        public SMPPClientSingleTaskPerClientTest() : base(typeof(SMPPClientSingleTaskPerClientTest))
		{

		}

        protected override void Execute(int requestPerClient)
        {
            foreach (var client in _clients)
            {
                Task.Factory.StartNew(() =>
                {
                    foreach (var clientRequestId in Enumerable.Range(0, requestPerClient))
                    {
                        CreateAndSendSubmitSm(requestPerClient, client.Key, client.Value, clientRequestId);
                    }
                });
            }
        }
    }
}
