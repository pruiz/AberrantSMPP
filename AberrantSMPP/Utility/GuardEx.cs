using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Dawn;

namespace AberrantSMPP.Utility
{
    public static class GuardEx
    {
        private static class Messages
        {
            public static string State(string caller) => caller == null
                ? "Operation is not valid due to the current state of the object."
                : caller + " call is not valid due to the current state of the object.";
        }

        [DebuggerStepThrough]
        public static void Against(bool invalid, string message = null, [CallerMemberName] string caller = null)
        {
            if (invalid)
                throw Guard.Fail((Exception)new InvalidOperationException(message ?? GuardEx.Messages.State(caller)));
        }
    }
}