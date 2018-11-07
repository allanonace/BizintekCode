﻿using System;
using Xml;

using RegType = MTUComm.MemoryMap.MemoryMap.RegType;

namespace MTUComm.MemoryMap
{
    public class MemoryRegister<T>
    {
        private enum CUSTOM_TYPE { EMPTY, METHOD, OPERATION, FORMAT }

        private const string STR_CUSTOM = "method";

        public Func<T> funcGet;
        public Action<T> funcSet;
        public string id { get; }
        public string description { get; }
        public RegType type { get; }
        public int address { get; }
        public int size { get; }
        public bool write { get; }
        public string custom { get; }
        private CUSTOM_TYPE customType;
        public bool used;

        // Value size ( number of consecutive bytes ) is also used for bit with bool type
        public int bit { get { return this.size; } }

        private bool _HasCustomMethod
        {
            get { return string.Equals ( this.custom.ToLower (), STR_CUSTOM ); }
        }

        private bool _HasCustomOperation
        {
            get { return ! this._HasCustomMethod      &&
                           this.type < RegType.CHAR &&
                         ! string.IsNullOrEmpty ( this.custom ); }
        }

        private bool _HasCustomFormat
        {
            get { return ! this._HasCustomMethod         &&
                           this.type == RegType.STRING &&
                         ! string.IsNullOrEmpty ( this.custom ); }
        }

        public bool HasCustomMethod
        {
            get { return this.customType == CUSTOM_TYPE.METHOD; }
        }

        public bool HasCustomOperation
        {
            get { return this.customType == CUSTOM_TYPE.OPERATION; }
        }

        public bool HasCustomFormat
        {
            get { return this.customType == CUSTOM_TYPE.FORMAT; }
        }

        public MemoryRegister (
            string id,
            RegType type,
            string description,
            int address,
            int size = MemRegister.DEF_SIZE,
            bool write = MemRegister.DEF_WRITE,
            string custom = "" )
        {
            this.id          = id;
            this.type        = type;
            this.description = description;
            this.address     = address;
            this.size        = size;
            this.write       = write;
            this.custom      = custom;

            if      ( this._HasCustomMethod    ) this.customType = CUSTOM_TYPE.METHOD;
            else if ( this._HasCustomOperation ) this.customType = CUSTOM_TYPE.OPERATION;
            else if ( this._HasCustomFormat    ) this.customType = CUSTOM_TYPE.FORMAT;
            else                                 this.customType = CUSTOM_TYPE.EMPTY;
        }

        public T Value
        {
            get { return (T)this.funcGet(); }
            set
            {
                // Register with read and write
                if ( this.write )
                    this.funcSet((T)value);

                // Register is readonly
                else
                {
                    // Register readonly
                    Console.WriteLine ( "Set " + id + ": Error - Can't write to this register" );
                    throw new MemoryRegisterNotAllowWrite ( MemoryMap.EXCEP_SET_READONLY + ": " + id );
                }
            }
        }
    }
}
