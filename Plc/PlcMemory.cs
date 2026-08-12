using System;
using System.Collections.Concurrent;

namespace MaterialControlSimulator.Plc
{
    public class PlcMemory
    {
        private readonly ConcurrentDictionary<int, bool>
            _bits = new();

        private readonly ConcurrentDictionary<int, ushort>
            _words = new();

        // -------------------------
        // B Device
        // -------------------------

        public bool ReadBit(
            int address)
        {
            return _bits.TryGetValue(
                address,
                out bool value)
                && value;
        }

        public void WriteBit(
            int address,
            bool value)
        {
            _bits[address] = value;
        }

        // -------------------------
        // W Device
        // -------------------------

        public ushort ReadWord(
            int address)
        {
            return _words.TryGetValue(
                address,
                out ushort value)
                ? value
                : (ushort)0;
        }

        public void WriteWord(
            int address,
            ushort value)
        {
            _words[address] = value;
        }

        // -------------------------
        // DWORD
        // Wn     = Low Word
        // Wn + 1 = High Word
        // -------------------------

        public uint ReadDWord(
            int address)
        {
            uint low =
                ReadWord(address);

            uint high =
                ReadWord(address + 1);

            return
                low |
                (high << 16);
        }

        public void WriteDWord(
            int address,
            uint value)
        {
            WriteWord(
                address,
                (ushort)(
                    value & 0xFFFF));

            WriteWord(
                address + 1,
                (ushort)(
                    (value >> 16) & 0xFFFF));
        }

        // -------------------------
        // 64 bit
        // 4 Words
        // -------------------------

        public ulong ReadQWord(
            int address)
        {
            ulong w0 =
                ReadWord(address);

            ulong w1 =
                ReadWord(address + 1);

            ulong w2 =
                ReadWord(address + 2);

            ulong w3 =
                ReadWord(address + 3);

            return
                w0 |
                (w1 << 16) |
                (w2 << 32) |
                (w3 << 48);
        }

        public void WriteQWord(
            int address,
            ulong value)
        {
            WriteWord(
                address,
                (ushort)(
                    value & 0xFFFF));

            WriteWord(
                address + 1,
                (ushort)(
                    (value >> 16) & 0xFFFF));

            WriteWord(
                address + 2,
                (ushort)(
                    (value >> 32) & 0xFFFF));

            WriteWord(
                address + 3,
                (ushort)(
                    (value >> 48) & 0xFFFF));
        }

        // -------------------------
        // 초기화
        // -------------------------

        public void Clear()
        {
            _bits.Clear();
            _words.Clear();
        }

        public bool TryReadWord(int address,out ushort value)
        {
            return _words.TryGetValue(
            address,
            out value);
        }

        public bool TryReadBit(int address,out bool value)
        {
            return _bits.TryGetValue(
            address,
            out value);
        }
    }
}
