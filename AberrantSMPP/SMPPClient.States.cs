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
    }
}