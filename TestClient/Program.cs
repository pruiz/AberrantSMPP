using System;
using System.Collections.Generic;
using AberrantSMPP;
using AberrantSMPP.EventObjects;
using AberrantSMPP.Packet;
using AberrantSMPP.Packet.Request;

namespace TestClient
{
	class Program
	{
	    private static readonly IList<string> SentMessages = new List<string>();

		static void Main(string[] args)
		{
		    try
		    {
                Gateway("username", "password");
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
	        client.Host = "smpp.smsgateway.com";
	        client.Port = 8445; 
	        client.SystemId = user;
	        client.Password = pass;

	        client.EnquireLinkInterval = 25;
	        client.BindType = SmppBind.BindingType.BindAsTransceiver;
	        client.NpiType = Pdu.NpiType.Isdn;
	        client.TonType = Pdu.TonType.International;
	        client.Version = Pdu.SmppVersionType.Version34;
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
	        client.OnSubmitSmResp += client_OnSubmitSmResp;
	        client.OnUnbind += (s, e) => Console.WriteLine("OnUnbind: " + e.ResponsePdu);
	        client.OnUnboundResp += (s, e) => Console.WriteLine("OnUnboundResp: " + e.ResponsePdu);

	        client.Bind();

            var txt = @"Lorem ipsum dolor sit amet, nonumy iisque appetere usu te. His liber dolores expetenda ea, ut usu harum percipitur, invenire voluptaria sed an. Ut quis nominavi qui, vim at alienum intellegat. In mel purto suscipiantur, at odio adolescens sea. Duo ludus animal ea, eum torquatos reformidans eu, sale dolores urbanitas at est.";

            MessageCoding coding;
            // split the text, get the byte arrays
            byte[][] byteMessagesArray = SmsMessageHelper.SplitMessage(txt, out coding);

            // esm_class parameter must be set if we are sending a contactenated message
            var esmClass = (byte)(byteMessagesArray.Length > 1 ? 0x40 : 0x0);

            // submit all messages
            for (int i = 0; i < byteMessagesArray.Length; i++)
            {
                var req = new SmppSubmitSm()
                {
                    DataCoding = DataCoding.OctetUnspecifiedA,// we are sending binary data. despite what the original text was
                    SourceAddress = "34915550000",
                    DestinationAddress = "61437600343",
                    ShortMessage = byteMessagesArray[i],
                    LanguageIndicator = LanguageIndicator.English,
                    RegisteredDelivery = Pdu.RegisteredDeliveryType.OnSuccessOrFailure,
                    EsmClass = esmClass,
                    ValidityPeriod = "000000235959000R", // R == Time Relative to SMSC's time.
                    PriorityFlag = Pdu.PriorityType.Highest, 
                };
                 
                Console.WriteLine("Sending message...: " + txt);
                client.SendPdu(req);
            }
              
	        client.Unbind();
	    }

	    static void client_OnSubmitSmResp(object source, SubmitSmRespEventArgs e)
		{
			Console.WriteLine("OnSubmitSmResp: " + e.ResponsePdu);

			var res = e.ResponsePdu;
			SentMessages.Add(res.MessageId.Trim());
		}
	}
}
