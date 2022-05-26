using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
		static ConcurrentBag<string> SentMessages = new ConcurrentBag<string>();

		static void Main(string[] args)
		{
			var client = new SMPPClient("127.0.0.1", 2775);
			client.SystemId = "smppclient1";
			client.Password = "password";
			client.EnquireLinkInterval = 25;
			client.BindType = AberrantSMPP.Packet.Request.SmppBind.BindingType.BindAsTransceiver;
			client.NpiType = AberrantSMPP.Packet.Pdu.NpiType.ISDN;
			client.TonType = AberrantSMPP.Packet.Pdu.TonType.International;
			client.Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version3_4;

			client.OnAlert += (s, e) => Console.WriteLine("Alert: " + e.Response);
			client.OnBind += (s, e) => Console.WriteLine("OnBind: " + e.Response);
			client.OnBindResp += (s, e) => Console.WriteLine("OnBindResp: " + e.Response);
			client.OnCancelSm += (s, e) => Console.WriteLine("OnCancelSm: " + e.Response);
			client.OnCancelSmResp += (s, e) => Console.WriteLine("OnCancelResp: " + e.Response);
			client.OnClose += (s, e) => Console.WriteLine("OnClose: " + e.GetType());
			client.OnDataSm += (s, e) => Console.WriteLine("OnDataSm: " + e.Response);
			client.OnDataSmResp += (s, e) => Console.WriteLine("OnDataResp: " + e.Response);
			client.OnDeliverSm += (s, e) => Console.WriteLine("OnDeliverSm: " + e.Response);
			client.OnDeliverSmResp += (s, e) => Console.WriteLine("OnDeliverSmResp: " + e.Response);
			client.OnEnquireLink += (s, e) => Console.WriteLine("OnEnquireLink: " + e.Response);
			client.OnEnquireLinkResp += (s, e) => Console.WriteLine("OnEnquireLinkResp: " + e.Response);
			client.OnError += (s, e) => Console.WriteLine("OnError: " + e.ThrownException?.ToString());
			client.OnGenericNack += (s, e) => Console.WriteLine("OnGenericNack: " + e.Response);
			client.OnQuerySm += (s, e) => Console.WriteLine("OnQuerySm: " + e.Response);
			client.OnQuerySmResp += (s, e) => Console.WriteLine("OnQuerySmResp: " + e.Response);
			client.OnReplaceSm += (s, e) => Console.WriteLine("OnReplaceSm: " + e.Response);
			client.OnReplaceSmResp += (s, e) => Console.WriteLine("OnReplaceSmResp: " + e.Response);
			client.OnSubmitMulti += (s, e) => Console.WriteLine("OnSubmitMulti: " + e.Response);
			client.OnSubmitMultiResp += (s, e) => Console.WriteLine("OnSubmitMultiResp: " + e.Response);
			client.OnSubmitSm += (s, e) => Console.WriteLine("OnSubmitSm: " + e.Response);
			client.OnSubmitSmResp += client_OnSubmitSmResp;
			client.OnUnbind += (s, e) => Console.WriteLine("OnUnbind: " + e.Response);
			client.OnUnboundResp += (s, e) => Console.WriteLine("OnUnboundResp: " + e.Response);

			client.Connect();
			client.Bind();

				//var txt = new String('a', 200);
			//var txt = "X de mas de 160 caractereñ.. @€34567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890ABCDEFGHIJKL987654321";
			var txt = @"X de mas de 160 caractereñ.. @€abcdefghijklmnopqrstxyz!!!0987654321-ABCDE";
#if true
			var req = new SmppSubmitSm() {
			//var req = new SmppDataSm() {
				AlertOnMsgDelivery = 0x1,
				DataCoding = DataCoding.UCS2,
				SourceAddress = "WOP",
				DestinationAddress = "+34667484721",
				//DestinationAddress = "+34692471323",
				//DestinationAddress = "+34915550000",
				ValidityPeriod = "000000235959000R", // R == Time Relative to SMSC's time.
				//EsmClass = ...
				LanguageIndicator = LanguageIndicator.Unspecified,
				//PayloadType = Pdu.PayloadTypeType.WDPMessage,
				MessagePayload = new byte[] { 0x0A, 0x0A },
#if true
				ShortMessage = txt,
#else
				ShortMessage = new byte[] { 
					Encoding.ASCII.GetBytes("A")[0], Encoding.ASCII.GetBytes("U")[0], 0x20,			// A
					0x20, 0x24,			// Currency
					0x20, 0x1b, 0x65,	// Euro symbol
					0x20, 0x2d,			// -
					0x20, 0x1b, 0x3d,	// tilde (~)
					0x20, 0x7d,			// ñ
					0x20, 0x09			// Cedilla
				},
#endif
				//MsValidity = Pdu.MsValidityType.StoreIndefinitely,
				//NumberOfMessages
				PriorityFlag = Pdu.PriorityType.Highest,
				//PrivacyIndicator = Pdu.PrivacyType.Nonrestricted
				RegisteredDelivery = //Pdu.RegisteredDeliveryType.OnSuccessOrFailure,
					(Pdu.RegisteredDeliveryType)0x1e,
			};
#else
			var req = new SmppDataSm()
			{
				//var req = new SmppDataSm() {
				AlertOnMsgDelivery = 0x1,
				DataCoding = Pdu.DataCodingType.Latin1,
				SourceAddress = "EVICERTIA",
				DestinationAddress = "+34667484721",
				//DestinationAddress = "+34915550000",
				//EsmClass = ...
				LanguageIndicator = Pdu.LanguageType.Spanish,
				//PayloadType = Pdu.PayloadTypeType.WDPMessage,
				MessagePayload = new byte[] { 0x0A, 0x0A },
				//MsValidity = Pdu.MsValidityType.StoreIndefinitely,
				//NumberOfMessages
				//PrivacyIndicator = Pdu.PrivacyType.Nonrestricted
				RegisteredDelivery = //Pdu.RegisteredDeliveryType.OnSuccessOrFailure,
					(Pdu.RegisteredDeliveryType)0x1e,
				SetDpf = Pdu.DpfResultType.DPFSet,
			};
#endif
			//AberrantSMPP.Utility.PduUtil.SetMessagePayload(req, req.MessagePayload);
			foreach (var _ in Enumerable.Range(1, 100))
			{
				client.SendPdu(req);
			}
			
			while (false)
			{
				foreach (var id in SentMessages)
				{
					var q = new SmppQuerySm() { MessageId = id };
					client.SendPdu(req);
				}

				System.Threading.Thread.Sleep(1000);
			}
			System.Threading.Thread.Sleep(1000);
		}

		static void client_OnSubmitSmResp(object source, AberrantSMPP.EventObjects.SubmitSmRespEventArgs e)
		{
			Console.WriteLine("OnSubmitSmResp: " + e.Response);

			var res = e.Response as SmppSubmitSmResp;
			SentMessages.Add(res.MessageId.Trim());
		}
	}
}
