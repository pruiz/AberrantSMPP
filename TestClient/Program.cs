using System;
using System.Collections.Generic;
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
			var client = new SMPPCommunicator();
			client.Host = "127.0.0.1";
			client.Port = 2775;
			client.SystemId = "test1";
			client.Password = "TEST1";
			client.EnquireLinkInterval = 25;
			client.BindType = AberrantSMPP.Packet.Request.SmppBind.BindingType.BindAsTransceiver;
			client.NpiType = AberrantSMPP.Packet.Pdu.NpiType.ISDN;
			client.TonType = AberrantSMPP.Packet.Pdu.TonType.International;
			client.Version = AberrantSMPP.Packet.Pdu.SmppVersionType.Version3_4;

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
			client.OnSubmitSmResp += new SMPPCommunicator.SubmitSmRespEventHandler(client_OnSubmitSmResp);
			client.OnUnbind += (s, e) => Console.WriteLine("OnUnbind: " + e.ResponsePdu);
			client.OnUnboundResp += (s, e) => Console.WriteLine("OnUnboundResp: " + e.ResponsePdu);

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
			client.SendPdu(req);

			while (true)
			{
				foreach (var id in SentMessages)
				{
					//var q = new SmppQuerySm() { MessageId = id };
					//client.SendPdu(q);
				}

				System.Threading.Thread.Sleep(1000);
			}
		}

		static void client_OnSubmitSmResp(object source, AberrantSMPP.EventObjects.SubmitSmRespEventArgs e)
		{
			Console.WriteLine("OnSubmitSmResp: " + e.ResponsePdu);

			var res = e.ResponsePdu as SmppSubmitSmResp;
			SentMessages.Add(res.MessageId.Trim());
		}
	}
}
