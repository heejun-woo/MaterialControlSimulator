using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc
{
    public class VirtualPlcMemory
    {
        private readonly Dictionary<string, object> _memory = new();

        public object? GetValue(string address)
        {
            return _memory.TryGetValue(address, out var value)
                ? value
                : null;
        }

        public void SetValue(string address, object value)
        {
            _memory[address] = value;
        }

        public bool Contains(string address)
        {
            return _memory.ContainsKey(address);
        }

        public void Clear()
        {
            _memory.Clear();
        }
    }
}
