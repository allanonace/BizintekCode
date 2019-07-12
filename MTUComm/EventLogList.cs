﻿using System;
using System.Collections.Generic;

using LogFilterMode = Lexi.Lexi.LogFilterMode;
using LogEntryType  = Lexi.Lexi.LogEntryType;

namespace MTUComm
{
    public class EventLogList
    {
        #region Constants

        public enum EventLogQueryResult
        {
            NextRead,
            LastRead,
            Busy,
            Empty
        }

        private const int  BYTE_RESULT  = 2;
        private const byte HAS_DATA     = 0x00;
        private const byte HAS_NOT_DATA = 0x01;

        #endregion

        #region Attributes

        private List<EventLog> entries;
        private DateTime dateStart;
        private DateTime dateEnd;
        private LogFilterMode filterMode;
        private LogEntryType entryType;

        #endregion

        #region Properties

        public DateTime DateStart
        {
            get { return this.dateStart; }
        }

        public DateTime DateEnd
        {
            get { return this.dateEnd; }
        }

        public LogFilterMode FilterMode
        {
            get { return this.filterMode; }
        }

        public LogEntryType EntryType
        {
            get { return this.entryType; }
        }

        public int Count
        {
            get { return this.entries.Count; }
        }

        public EventLog[] Entries
        {
            get { return this.entries.ToArray (); }
        }

        public int TotalEntries
        {
            get
            {
                if ( this.entries.Count > 0 )
                    return this.entries[ 0 ].TotalEntries;
                return -1;
            }
        }

        #endregion

        #region Initialization

        public EventLogList (
            DateTime start,
            DateTime end,
            LogFilterMode filterMode,
            LogEntryType entryType )
        {
            this.entries    = new List<EventLog> ();
            this.dateStart  = start;
            this.dateEnd    = end;
            this.filterMode = filterMode;
            this.entryType  = entryType;
        }

        #endregion

        public ( EventLogQueryResult Result, int Index ) TryToAdd (
            byte[] response )
        {
            EventLog evnt = null;
        
            switch ( response[ BYTE_RESULT ] )
            {
                // ACK without log entry
                case byte val when val != 0x00:
                    return ( ( val == HAS_NOT_DATA ) ? EventLogQueryResult.Empty : EventLogQueryResult.Busy, -1 );

                // ACK with log entry
                case HAS_DATA:
                    evnt = new EventLog ( response );
                    if ( this.entries.Count >= evnt.Index )
                         this.entries[ evnt.Index - 1 ] = evnt;
                    else this.entries.Add ( evnt );
                    break;
            }

            return ( ( this.entries[ this.entries.Count - 1 ].IsLast ) ?
                EventLogQueryResult.LastRead : EventLogQueryResult.NextRead, evnt.Index );
        }
    }
}
