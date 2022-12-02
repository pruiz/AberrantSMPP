/* AberrantSMPP: SMPP communication library
 * Copyright (C) 2004, 2005 Christopher M. Bouzek
 * Copyright (C) 2010, 2011 Pablo Ruiz García <pruiz@crt0.net>
 *
 * This file is part of RoaminSMPP.
 *
 * RoaminSMPP is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, version 3 of the License.
 *
 * RoaminSMPP is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with RoaminSMPP.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace AberrantSMPP.Utility
{
    public class RetryIntervals
    {
        private static readonly TimeSpan[] _emptyIntervals = new TimeSpan[] { };
        private TimeSpan[] _intervals;
        private int _index;

        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Get the current interval of tye retry interval list. Zero 0 if disabled.
        /// </summary>
        public TimeSpan CurrentInterval => Enabled ? _intervals[_index] : TimeSpan.Zero;
        public TimeSpan[] AllIntervals => _intervals?.Reverse().ToArray() ?? _emptyIntervals;

        public RetryIntervals(IEnumerable<TimeSpan> intervals = null)
        {
            Update(intervals);
        }

        public void Update(IEnumerable<TimeSpan> intervals)
        {
            _intervals = intervals?.Reverse().ToArray() ?? _emptyIntervals;
            ResetIndex();
        }

        public void ResetIndex()
        {
            _index = _intervals.Length - 1;
        }

        internal TimeSpan GetNext()
        {
            if (_index > 0)
                _index--;
            return CurrentInterval;
        }
    }
}
