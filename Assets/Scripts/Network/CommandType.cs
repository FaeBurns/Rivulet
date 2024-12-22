using System;

namespace Network
{
    public enum CommandType : byte
    {
        RESERVED = 0x00,
        NEGOTIATE_STREAM = 0x01,
        EXIT = Byte.MaxValue,
    }
}