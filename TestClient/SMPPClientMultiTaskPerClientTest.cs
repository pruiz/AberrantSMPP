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
	internal class SMPPClientMultiTaskPerClientTest : SMPPClientTestsBase
	{
        public SMPPClientMultiTaskPerClientTest() : base(typeof(SMPPClientMultiTaskPerClientTest))
		{
            
        }
        protected override void Execute(int numberOfClients, int requestPerClient)
        {
            foreach (var client in _clients)
            {
                Task.Factory.StartNew(() =>
                {
                    Enumerable.Range(0, requestPerClient)
                        .AsParallel()
                        .ForAll((clientRequestId) =>
                        {
                            Task.Factory.StartNew(() =>
                            {
                                CreateAndSendSubmitSm(requestPerClient, client.Key, client.Value, clientRequestId);
                            });
                        });
                });
            }
        }
    }
}
