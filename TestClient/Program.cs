using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using AberrantSMPP;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;
using AberrantSMPP.Packet.Response;

namespace TestClient
{
	class Program
	{
		static IList<string> SentMessages = new List<string>();

		static void Main(string[] args)
		{
		    try
		    {
                Gateway("telnetworklcr", "coNN3ct370603");
            }
		    catch (Exception ex)
		    {
		        Console.WriteLine(ex.Message);
		    }
            try
            {
                Gateway("dtnw", "OmeGa370603");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press any key to exit");
		    Console.ReadKey();
		}

	    private static void Gateway(string user, string pass)
	    {
	        var client = new SmppCommunicator();
	        client.Host = "smpp.silverstreet.com";
	        client.Port = 8445;
	        //client.SystemId = "dtnw";
	        //client.Password = "OmeGa370603";
	        client.SystemId = user;
	        client.Password = pass;

	        client.EnquireLinkInterval = 25;
	        client.BindType = AberrantSMPP.Packet.Request.SmppBind.BindingType.BindAsTransceiver;
	        client.NpiType = AberrantSMPP.Packet.Pdu.NpiType.Isdn;
	        client.TonType = AberrantSMPP.Packet.Pdu.TonType.International;
	        client.Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version34;
	        client.UseSsl = true;

	        client.OnAlert += (s, e) => Console.WriteLine("Alert: " + e.ResponsePdu);
	        client.OnBind += (s, e) => Console.WriteLine("OnBind: " + e.ResponsePdu);
	        client.OnBindResp += (s, e) => Console.WriteLine("OnBindResp: " + e.ResponsePdu);
	        client.OnCancelSm += (s, e) => Console.WriteLine("OnCancelSm: " + e.ResponsePdu);
	        client.OnCancelSmResp += (s, e) => Console.WriteLine("OnCancelResp: " + e.ResponsePdu);
	        client.OnClose += (s, e) => Console.WriteLine("OnClose: " + e.GetType());
	        client.OnDataSm += (s, e) => Console.WriteLine("OnDataSm: " + e.ResponsePdu);
	        client.OnDataSmResp += (s, e) => Console.WriteLine("OnDataResp: " + e.ResponsePdu);
	        client.OnDeliverSm += (s, e) => Console.WriteLine("OnDeliverSm: " + e.ResponsePdu);
	        client.OnDeliverSmResp += (s, e) => Console.WriteLine("OnDeliverSmResp: " + e.ResponsePdu);
	        client.OnEnquireLink += (s, e) => Console.WriteLine("OnEnquireLink: " + e.ResponsePdu);
	        client.OnEnquireLinkResp += (s, e) => Console.WriteLine("OnEnquireLinkResp: " + e.ResponsePdu);
	        client.OnError += (s, e) => Console.WriteLine("OnError: " + e.ThrownException.Message);
	        client.OnGenericNack += (s, e) => Console.WriteLine("OnGenericNack: " + e.ResponsePdu);
	        client.OnQuerySm += (s, e) => Console.WriteLine("OnQuerySm: " + e.ResponsePdu);
	        client.OnQuerySmResp += (s, e) => Console.WriteLine("OnQuerySmResp: " + e.ResponsePdu);
	        client.OnReplaceSm += (s, e) => Console.WriteLine("OnReplaceSm: " + e.ResponsePdu);
	        client.OnReplaceSmResp += (s, e) => Console.WriteLine("OnReplaceSmResp: " + e.ResponsePdu);
	        client.OnSubmitMulti += (s, e) => Console.WriteLine("OnSubmitMulti: " + e.ResponsePdu);
	        client.OnSubmitMultiResp += (s, e) => Console.WriteLine("OnSubmitMultiResp: " + e.ResponsePdu);
	        client.OnSubmitSm += (s, e) => Console.WriteLine("OnSubmitSm: " + e.ResponsePdu);
	        client.OnSubmitSmResp += new SmppCommunicator.SubmitSmRespEventHandler(client_OnSubmitSmResp);
	        client.OnUnbind += (s, e) => Console.WriteLine("OnUnbind: " + e.ResponsePdu);
	        client.OnUnboundResp += (s, e) => Console.WriteLine("OnUnboundResp: " + e.ResponsePdu);

	        client.Bind();

	        //var txt = new String('a', 200);
	        //var txt = "X de mas de 160 caractereñ.. @€34567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890ABCDEFGHIJKL987654321";
	        var txt = @"This is a test messasge from: " + user;
	        var req = new SmppSubmitSm()
	        {
	            //var req = new SmppDataSm() {
	            AlertOnMsgDelivery = 0x1,
	            DataCoding = DataCoding.Ucs2,
	            SourceAddress = "TELNETWORKS",
	            DestinationAddress = "61401700077",
	            //DestinationAddress = "+34692471323",
	            //DestinationAddress = "+34915550000",
	            ValidityPeriod = "000000235959000R", // R == Time Relative to SMSC's time.
	            //EsmClass = ...
	            LanguageIndicator = LanguageIndicator.English,
	            //PayloadType = Pdu.PayloadTypeType.WDPMessage,
	            MessagePayload = new byte[] {0x0A, 0x0A},
	            ShortMessage = txt,
	            //MsValidity = Pdu.MsValidityType.StoreIndefinitely,
	            //NumberOfMessages
	            PriorityFlag = Pdu.PriorityType.Highest,
	            //PrivacyIndicator = Pdu.PrivacyType.Nonrestricted
	            RegisteredDelivery = //Pdu.RegisteredDeliveryType.OnSuccessOrFailure,
	                (Pdu.RegisteredDeliveryType) 0x1e,
	        };

	        //AberrantSMPP.Utility.PduUtil.SetMessagePayload(req, req.MessagePayload);
	        Console.WriteLine("Sending message...: " + txt);
	        client.SendPdu(req);

	        //while (true)
	        //{
	        foreach (var id in SentMessages)
	        {
	            Console.WriteLine(id);
	            //var q = new SmppQuerySm() { MessageId = id };
	            //client.SendPdu(q);
	        }

	        //	System.Threading.Thread.Sleep(1000);
	        //}
	        client.Unbind();
	    }

	    static void client_OnSubmitSmResp(object source, AberrantSMPP.EventObjects.SubmitSmRespEventArgs e)
		{
			Console.WriteLine("OnSubmitSmResp: " + e.ResponsePdu);

			var res = e.ResponsePdu as SmppSubmitSmResp;
			SentMessages.Add(res.MessageId.Trim());
		}
	}
}
