/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2010-2022 Pablo Ruiz García <pruiz@netway.org>
 *
 * This file is part of AberrantSMPP.
 *
 * AberrantSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * AberrantSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with AberrantSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace AberrantSMPP.EventObjects
{
    /// <summary>
    /// Class to provide notifications on client state changed.
    /// </summary>
    public class ClientStateChangedEventArgs : SmppEventArgs
    {
        public SMPPClient.States OldState { get; }
        public SMPPClient.States NewState { get; }

        /// <summary>
        /// Sets up the SmppEventArgs.
        /// </summary>
        public ClientStateChangedEventArgs(SMPPClient.States oldState, SMPPClient.States newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }
}