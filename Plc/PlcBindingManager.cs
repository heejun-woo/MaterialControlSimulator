using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Threading;

namespace MaterialControlSimulator.Plc
{
    public class PlcBindingManager
    {
        private readonly PlcMemory _memory;

        private readonly List<IPlcBindable> _targets = new();

        private readonly Dictionary<string, List<CachedBinding>> _bindings = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _lock = new();

        // =========================================================
        // Cached Binding
        // =========================================================

        private sealed class CachedBinding
        {
            public required IPlcBindable Target { get; init; }

            public required PlcBinding Binding { get; init; }

            public required PropertyInfo Property { get; init; }
        }

        // =========================================================
        // Constructor
        // =========================================================

        public PlcBindingManager(
            PlcMemory memory)
        {
            _memory = memory;
        }

        // =========================================================
        // REGISTER
        // =========================================================

        public void Register(
            IPlcBindable target)
        {
            if (target == null)
                return;

            lock (_lock)
            {
                if (_targets.Contains(target))
                    return;

                _targets.Add(target);
            }

            CacheTarget(target);
        }

        // =========================================================
        // UNREGISTER
        // =========================================================
        public void Unregister(
            IPlcBindable target)
        {
            if (target == null)
                return;

            lock (_lock)
            {
                _targets.Remove(
                    target);

                List<string> emptyKeys =
                    new();

                foreach (var pair
                         in _bindings)
                {
                    pair.Value.RemoveAll(
                        x =>
                            ReferenceEquals(
                                x.Target,
                                target));

                    if (pair.Value.Count == 0)
                    {
                        emptyKeys.Add(
                            pair.Key);
                    }
                }

                foreach (string key
                         in emptyKeys)
                {
                    _bindings.Remove(
                        key);
                }
            }
        }

        // =========================================================
        // CACHE TARGET
        // =========================================================

        private void CacheTarget(
            IPlcBindable target)
        {
            foreach (PlcBinding binding
                     in target.PlcBindings)
            {
                if (string.IsNullOrWhiteSpace(
                        binding.Address))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(
                        binding.PropertyName))
                {
                    continue;
                }

                PropertyInfo? property =
                    target
                        .GetType()
                        .GetProperty(
                            binding.PropertyName);

                if (property == null)
                {
                    Debug.WriteLine(
                        $"PLC Property Not Found : " +
                        $"{binding.Address} -> " +
                        $"{binding.PropertyName}");

                    continue;
                }

                var cached =
                    new CachedBinding
                    {
                        Target = target,
                        Binding = binding,
                        Property = property
                    };


                bool isFirstBinding = false;

                lock (_lock)
                {
                    string key =
                        NormalizeAddress(
                            binding.Address);

                    if (!_bindings.TryGetValue(
                            key,
                            out List<CachedBinding>? list))
                    {
                        list =
                            new List<CachedBinding>();

                        _bindings[key] =
                            list;

                        isFirstBinding = true;
                    }

                    list.Add(cached);
                }

                if (isFirstBinding)
                {
                    SyncBindingToMemory(
                        binding.Address);
                }
            }

        }

        // =========================================================
        // REBUILD CACHE
        //
        // PLC 주소 변경 후 호출
        // =========================================================

        public void RebuildCache()
        {
            List<IPlcBindable> targets;

            lock (_lock)
            {
                targets =
                    _targets.ToList();

                _bindings.Clear();
            }

            foreach (IPlcBindable target
                     in targets)
            {
                CacheTarget(
                    target);
            }

            Debug.WriteLine(
                $"PLC Binding Cache Count = " +
                $"{_bindings.Count}");
        }

        // =========================================================
        // FIND
        // =========================================================

        private CachedBinding? FindCached(
            string address)
        {
            string key =
                NormalizeAddress(
                    address);

            lock (_lock)
            {
                if (_bindings.TryGetValue(
                        key,
                        out List<CachedBinding>? list) &&
                    list.Count > 0)
                {
                    return list[0];
                }

                return null;
            }
        }

        private List<CachedBinding> FindCachedAll(
            string address)
        {
            string key =
                NormalizeAddress(
                    address);

            lock (_lock)
            {
                if (_bindings.TryGetValue(
                        key,
                        out List<CachedBinding>? list))
                {
                    // 통신 Thread에서 순회 중
                    // Register/Unregister되어도 영향 없도록 복사
                    return list.ToList();
                }
            }

            return new List<CachedBinding>();
        }

        public bool HasBinding(
            string address)
        {
            return
                FindCached(address)
                != null;
        }

        public PlcDataType? GetDataType(
            string address)
        {
            return FindCached(
                address)?
                .Binding
                .DataType;
        }

        public int GetWordCount(
            string address)
        {
            CachedBinding? cached =
                FindCached(
                    address);

            if (cached == null)
                return 0;

            return Math.Max(
                1,
                cached.Binding.WordCount);
        }

        // =========================================================
        // SET VALUE
        //
        // Binding 있음
        //  -> Binding 타입 + WordCount 사용
        //
        // Binding 없음
        //  -> 실제 C# 타입으로 Memory Write
        // =========================================================
        public bool SetValue(
            string address,
            object? value)
        {
            if (value == null)
                return false;

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int deviceAddress))
            {
                return false;
            }

            List<CachedBinding> bindings =
                FindCachedAll(
                    address);

            // =====================================================
            // Binding 없음
            // =====================================================

            if (bindings.Count == 0)
            {
                return WriteUnboundValue(
                    device,
                    deviceAddress,
                    value,
                    null);
            }

            try
            {
                // =================================================
                // Memory는 한 번만 Write
                //
                // 같은 PLC 주소를 공유하므로 첫 Binding의
                // PLC DataType을 기준으로 한다.
                // =================================================

                CachedBinding primary =
                    bindings[0];

                object? plcValue =
                    ConvertFromPlcType(
                        value,
                        primary.Binding.DataType,
                        primary.Property.PropertyType);

                if (plcValue == null)
                    return false;

                if (device == 'B')
                {
                    _memory.WriteBit(
                        deviceAddress,
                        Convert.ToBoolean(
                            plcValue));
                }
                else if (device == 'W')
                {
                    WriteValueToMemory(
                        deviceAddress,
                        plcValue,
                        primary.Binding.DataType,
                        Math.Max(
                            1,
                            primary.Binding.WordCount));
                }

                // =================================================
                // 같은 주소에 연결된 모든 Property 갱신
                // =================================================

                foreach (CachedBinding cached
                         in bindings)
                {
                    if (!cached.Property.CanWrite)
                        continue;

                    try
                    {
                        object? converted =
                            ConvertFromPlcType(
                                value,
                                cached.Binding.DataType,
                                cached.Property.PropertyType);

                        SetTargetProperty(
                            cached,
                            converted,
                            address);
                    }
                    catch (Exception ex)
                    {
                        // 하나 실패했다고 다른 Binding까지
                        // 갱신을 중단하면 안 됨.
                        Debug.WriteLine(
                            $"PLC Binding SET ERROR " +
                            $"{address} -> " +
                            $"{cached.Property.Name}: " +
                            $"{ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC SetValue ERROR " +
                    $"{address}: {ex.Message}");

                return false;
            }
        }

        // =========================================================
        // SET VALUE + WORD COUNT
        //
        // Binding 없는 String 등에 사용
        // =========================================================

        public bool SetValue(
            string address,
            object? value,
            int wordCount)
        {
            if (value == null ||
                wordCount <= 0)
            {
                return false;
            }

            // Binding 있으면 Binding WordCount 우선
            if (HasBinding(address))
            {
                return SetValue(
                    address,
                    value);
            }

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int deviceAddress))
            {
                return false;
            }

            return WriteUnboundValue(
                device,
                deviceAddress,
                value,
                wordCount);
        }

        // =========================================================
        // GET VALUE
        //
        // Binding 있음
        // -> DataType / WordCount 자동
        //
        // Binding 없음
        // -> B bool / W ushort
        // =========================================================

        public object? GetValue(
            string address)
        {
            if (!TryParseAddress(
                    address,
                    out char device,
                    out int deviceAddress))
            {
                return null;
            }

            CachedBinding? cached =
                FindCached(
                    address);

            if (cached != null)
            {
                return ReadBoundValue(
                    device,
                    deviceAddress,
                    cached.Binding.DataType,
                    Math.Max(
                        1,
                        cached.Binding.WordCount));
            }

            if (device == 'B')
            {
                return _memory.ReadBit(
                    deviceAddress);
            }

            if (device == 'W')
            {
                return _memory.ReadWord(
                    deviceAddress);
            }

            return null;
        }

        // =========================================================
        // GENERIC GET
        //
        // GetValue<int>("W100")
        // =========================================================

        public T? GetValue<T>(
            string address)
        {
            try
            {
                object? value =
                    ReadMemoryValue(
                        address,
                        typeof(T),
                        null);

                if (value == null)
                    return default;

                return (T)value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC GetValue<{typeof(T).Name}> ERROR " +
                    $"{address}: {ex.Message}");

                return default;
            }
        }

        // =========================================================
        // GENERIC GET + WORD COUNT
        //
        // GetValue<string>("W100", 20)
        // =========================================================

        public T? GetValue<T>(
            string address,
            int wordCount)
        {
            if (wordCount <= 0)
                return default;

            try
            {
                object? value =
                    ReadMemoryValue(
                        address,
                        typeof(T),
                        wordCount);

                if (value == null)
                    return default;

                return (T)value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC GetValue<{typeof(T).Name}> ERROR " +
                    $"{address}: {ex.Message}");

                return default;
            }
        }

        // =========================================================
        // RAW WORDS
        // =========================================================

        public ushort[] GetWords(
            string address,
            int wordCount)
        {
            if (wordCount <= 0)
                return Array.Empty<ushort>();

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int startAddress) ||
                device != 'W')
            {
                return Array.Empty<ushort>();
            }

            ushort[] result =
                new ushort[wordCount];

            for (int i = 0;
                 i < wordCount;
                 i++)
            {
                result[i] =
                    _memory.ReadWord(
                        startAddress + i);
            }

            return result;
        }

        public bool SetWords(
            string address,
            ushort[] values)
        {
            if (values == null ||
                values.Length == 0)
            {
                return false;
            }

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int startAddress) ||
                device != 'W')
            {
                return false;
            }

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                _memory.WriteWord(
                    startAddress + i,
                    values[i]);
            }

            RefreshBindingsInRange(
                startAddress,
                values.Length);

            return true;
        }

        // =========================================================
        // BIT
        // =========================================================

        public bool ReadBit(
            int address)
        {
            return _memory.ReadBit(
                address);
        }

        public void WriteBit(
            int address,
            bool value)
        {
            // Memory는 한 번
            _memory.WriteBit(
                address,
                value);

            string plcAddress =
                $"B{address:X}";

            List<CachedBinding> bindings =
                FindCachedAll(
                    plcAddress);

            foreach (CachedBinding cached
                     in bindings)
            {
                if (!cached.Property.CanWrite)
                    continue;

                try
                {
                    object? converted =
                        ConvertFromPlcType(
                            value,
                            cached.Binding.DataType,
                            cached.Property.PropertyType);

                    SetTargetProperty(
                        cached,
                        converted,
                        plcAddress);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"PLC WriteBit ERROR " +
                        $"{plcAddress} -> " +
                        $"{cached.Property.Name}: " +
                        $"{ex.Message}");
                }
            }
        }

        // =========================================================
        // WORD
        // =========================================================

        public ushort ReadWord(
            int address)
        {
            return _memory.ReadWord(
                address);
        }

        public void WriteWord(
            int address,
            ushort value)
        {
            _memory.WriteWord(
                address,
                value);
        }

        // =========================================================
        // BINDING -> MEMORY
        // =========================================================

        public void SyncBindingToMemory(
            string address)
        {
            CachedBinding? cached =
                FindCached(
                    address);

            if (cached == null ||
                !cached.Property.CanRead)
            {
                return;
            }

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int deviceAddress))
            {
                return;
            }

            try
            {
                object? value =
                    GetTargetProperty(
                        cached);

                if (value == null)
                    return;

                object? converted =
                    ConvertToPlcType(
                        value,
                        cached.Binding.DataType);

                if (converted == null)
                    return;

                if (device == 'B')
                {
                    _memory.WriteBit(
                        deviceAddress,
                        Convert.ToBoolean(
                            converted));
                }
                else if (device == 'W')
                {
                    WriteValueToMemory(
                        deviceAddress,
                        converted,
                        cached.Binding.DataType,
                        Math.Max(
                            1,
                            cached.Binding.WordCount));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC Sync ERROR " +
                    $"{address}: {ex.Message}");
            }
        }

        public void SyncAllBindingsToMemory()
        {
            List<string> addresses;

            lock (_lock)
            {
                addresses =
                    _bindings.Keys.ToList();
            }

            foreach (string address
                     in addresses)
            {
                SyncBindingToMemory(
                    address);
            }
        }

        // =========================================================
        // MEMORY -> BINDING
        //
        // MC W Write 이후 호출
        // =========================================================

        public void RefreshBindingsInRange(
            int startAddress,
            int wordCount)
        {
            if (wordCount <= 0)
                return;

            int endAddress =
                startAddress +
                wordCount -
                1;

            List<(int Address, CachedBinding Cached)>
                targets = new();

            lock (_lock)
            {
                foreach (var pair
                         in _bindings)
                {
                    if (!TryParseAddress(
                            pair.Key,
                            out char device,
                            out int address))
                    {
                        continue;
                    }

                    if (device != 'W')
                        continue;

                    foreach (CachedBinding cached
                             in pair.Value)
                    {
                        int bindingWords =
                            Math.Max(
                                1,
                                cached.Binding.WordCount);

                        int bindingEnd =
                            address +
                            bindingWords -
                            1;

                        if (address <= endAddress &&
                            bindingEnd >= startAddress)
                        {
                            targets.Add(
                                (
                                    address,
                                    cached
                                ));
                        }
                    }
                }
            }

            foreach (var item
                     in targets.OrderBy(
                         x => x.Address))
            {
                RefreshBinding(
                    item.Address,
                    item.Cached);
            }
        }


        public void RefreshBinding(int address)
        {
            string plcAddress =
                $"W{address:X}";

            List<CachedBinding> bindings =
                FindCachedAll(
                    plcAddress);

            foreach (CachedBinding cached
                     in bindings)
            {
                RefreshBinding(
                    address,
                    cached);
            }
        }

        private void RefreshBinding(
            int address,
            CachedBinding cached)
        {
            if (!cached.Property.CanWrite)
                return;

            try
            {
                object? value =
                    ReadBoundValue(
                        'W',
                        address,
                        cached.Binding.DataType,
                        Math.Max(
                            1,
                            cached.Binding.WordCount));

                if (value == null)
                    return;

                object? converted =
                    ConvertFromPlcType(
                        value,
                        cached.Binding.DataType,
                        cached.Property.PropertyType);

                SetTargetProperty(
                    cached,
                    converted,
                    $"W{address:X}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC Refresh ERROR " +
                    $"W{address:X}: {ex.Message}");
            }
        }

        // =========================================================
        // TARGET PROPERTY GET / SET
        // =========================================================

        private object? GetTargetProperty(
            CachedBinding cached)
        {
            if (!cached.Property.CanRead)
                return null;

            if (cached.Target is DispatcherObject dispatcher &&
                !dispatcher.Dispatcher.CheckAccess())
            {
                return dispatcher.Dispatcher.Invoke(
                    () =>
                        cached.Property.GetValue(
                            cached.Target));
            }

            return cached.Property.GetValue(
                cached.Target);
        }

        private void SetTargetProperty(
            CachedBinding cached,
            object? value,
            string address)
        {
            if (!cached.Property.CanWrite)
                return;

            if (cached.Target is DispatcherObject dispatcher &&
                !dispatcher.Dispatcher.CheckAccess())
            {
                dispatcher.Dispatcher.BeginInvoke(
                    new Action(
                        () =>
                        {
                            try
                            {
                                cached.Property.SetValue(
                                    cached.Target,
                                    value);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(
                                    $"PLC UI SET ERROR " +
                                    $"{address}: " +
                                    $"{ex.Message}");
                            }
                        }));

                return;
            }

            cached.Property.SetValue(
                cached.Target,
                value);
        }

        // =========================================================
        // BOUND VALUE -> MEMORY
        // =========================================================

        private void WriteValueToMemory(
            int address,
            object value,
            PlcDataType dataType,
            int wordCount)
        {
            string typeName =
                dataType.ToString();

            if (typeName.Equals(
                    "Bool",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteWord(
                    address,
                    Convert.ToBoolean(value)
                        ? (ushort)1
                        : (ushort)0);

                return;
            }

            if (typeName.Equals(
                    "UInt16",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteWord(
                    address,
                    Convert.ToUInt16(value));

                return;
            }

            if (typeName.Equals(
                    "Int16",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteWord(
                    address,
                    unchecked(
                        (ushort)
                        Convert.ToInt16(value)));

                return;
            }

            if (typeName.Equals(
                    "UInt32",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteDWord(
                    address,
                    Convert.ToUInt32(value));

                return;
            }

            if (typeName.Equals(
                    "Int32",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteDWord(
                    address,
                    unchecked(
                        (uint)
                        Convert.ToInt32(value)));

                return;
            }

            if (typeName.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals(
                    "Single",
                    StringComparison.OrdinalIgnoreCase))
            {
                byte[] bytes =
                    BitConverter.GetBytes(
                        Convert.ToSingle(value));

                _memory.WriteDWord(
                    address,
                    BitConverter.ToUInt32(
                        bytes,
                        0));

                return;
            }

            if (typeName.Equals(
                    "Double",
                    StringComparison.OrdinalIgnoreCase))
            {
                byte[] bytes =
                    BitConverter.GetBytes(
                        Convert.ToDouble(value));

                _memory.WriteQWord(
                    address,
                    BitConverter.ToUInt64(
                        bytes,
                        0));

                return;
            }

            if (typeName.Equals(
                    "String",
                    StringComparison.OrdinalIgnoreCase))
            {
                WriteStringToMemory(
                    address,
                    Convert.ToString(value)
                        ?? string.Empty,
                    wordCount);

                return;
            }
        }

        // =========================================================
        // STRING
        //
        // WordCount 전체를 항상 덮어씀
        // =========================================================

        private void WriteStringToMemory(
            int address,
            string value,
            int wordCount)
        {
            wordCount =
                Math.Max(
                    1,
                    wordCount);

            int capacity =
                wordCount * 2;

            byte[] buffer =
                new byte[capacity];

            byte[] source =
                Encoding.ASCII.GetBytes(
                    value ?? string.Empty);

            int copyLength =
                Math.Min(
                    source.Length,
                    capacity);

            if (copyLength > 0)
            {
                Buffer.BlockCopy(
                    source,
                    0,
                    buffer,
                    0,
                    copyLength);
            }

            // 남은 buffer는 0
            // -> 이전 문자열 잔여 영역 제거

            for (int i = 0;
                 i < wordCount;
                 i++)
            {
                int index =
                    i * 2;

                ushort word =
                    (ushort)(
                        buffer[index] |
                        (buffer[index + 1] << 8));

                _memory.WriteWord(
                    address + i,
                    word);
            }
        }

        private string ReadStringFromMemory(
            int address,
            int wordCount)
        {
            wordCount =
                Math.Max(
                    1,
                    wordCount);

            byte[] bytes =
                new byte[
                    wordCount * 2];

            int output = 0;

            for (int i = 0;
                 i < wordCount;
                 i++)
            {
                ushort word =
                    _memory.ReadWord(
                        address + i);

                bytes[output++] =
                    (byte)(
                        word & 0xFF);

                bytes[output++] =
                    (byte)(
                        word >> 8);
            }

            int length =
                Array.IndexOf(
                    bytes,
                    (byte)0);

            if (length < 0)
            {
                length =
                    bytes.Length;
            }

            return Encoding.ASCII.GetString(
                bytes,
                0,
                length);
        }

        // =========================================================
        // READ BOUND VALUE
        // =========================================================

        private object? ReadBoundValue(
            char device,
            int address,
            PlcDataType dataType,
            int wordCount)
        {
            if (device == 'B')
            {
                return _memory.ReadBit(
                    address);
            }

            if (device != 'W')
                return null;

            string typeName =
                dataType.ToString();

            if (typeName.Equals(
                    "Bool",
                    StringComparison.OrdinalIgnoreCase))
            {
                return _memory.ReadWord(
                    address) != 0;
            }

            if (typeName.Equals(
                    "UInt16",
                    StringComparison.OrdinalIgnoreCase))
            {
                return _memory.ReadWord(
                    address);
            }

            if (typeName.Equals(
                    "Int16",
                    StringComparison.OrdinalIgnoreCase))
            {
                return unchecked(
                    (short)
                    _memory.ReadWord(
                        address));
            }

            if (typeName.Equals(
                    "UInt32",
                    StringComparison.OrdinalIgnoreCase))
            {
                return _memory.ReadDWord(
                    address);
            }

            if (typeName.Equals(
                    "Int32",
                    StringComparison.OrdinalIgnoreCase))
            {
                return unchecked(
                    (int)
                    _memory.ReadDWord(
                        address));
            }

            if (typeName.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals(
                    "Single",
                    StringComparison.OrdinalIgnoreCase))
            {
                uint raw =
                    _memory.ReadDWord(
                        address);

                return BitConverter.ToSingle(
                    BitConverter.GetBytes(
                        raw),
                    0);
            }

            if (typeName.Equals(
                    "Double",
                    StringComparison.OrdinalIgnoreCase))
            {
                ulong raw =
                    _memory.ReadQWord(
                        address);

                return BitConverter.ToDouble(
                    BitConverter.GetBytes(
                        raw),
                    0);
            }

            if (typeName.Equals(
                    "String",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ReadStringFromMemory(
                    address,
                    wordCount);
            }

            return null;
        }

        // =========================================================
        // UNBOUND SET
        // =========================================================

        private bool WriteUnboundValue(
            char device,
            int address,
            object value,
            int? wordCount)
        {
            try
            {
                if (device == 'B')
                {
                    _memory.WriteBit(
                        address,
                        Convert.ToBoolean(value));

                    return true;
                }

                if (device != 'W')
                    return false;

                // 8bit
                if (value is byte u8)
                {
                    _memory.WriteWord(
                        address,
                        u8);

                    return true;
                }

                if (value is sbyte i8)
                {
                    _memory.WriteWord(
                        address,
                        unchecked(
                            (ushort)i8));

                    return true;
                }

                // 16bit
                if (value is ushort u16)
                {
                    _memory.WriteWord(
                        address,
                        u16);

                    return true;
                }

                if (value is short i16)
                {
                    _memory.WriteWord(
                        address,
                        unchecked(
                            (ushort)i16));

                    return true;
                }

                // 32bit
                if (value is uint u32)
                {
                    _memory.WriteDWord(
                        address,
                        u32);

                    return true;
                }

                if (value is int i32)
                {
                    _memory.WriteDWord(
                        address,
                        unchecked(
                            (uint)i32));

                    return true;
                }

                if (value is float f32)
                {
                    byte[] bytes =
                        BitConverter.GetBytes(
                            f32);

                    _memory.WriteDWord(
                        address,
                        BitConverter.ToUInt32(
                            bytes,
                            0));

                    return true;
                }

                // 64bit
                if (value is ulong u64)
                {
                    _memory.WriteQWord(
                        address,
                        u64);

                    return true;
                }

                if (value is long i64)
                {
                    _memory.WriteQWord(
                        address,
                        unchecked(
                            (ulong)i64));

                    return true;
                }

                if (value is double f64)
                {
                    byte[] bytes =
                        BitConverter.GetBytes(
                            f64);

                    _memory.WriteQWord(
                        address,
                        BitConverter.ToUInt64(
                            bytes,
                            0));

                    return true;
                }

                // String
                if (value is string text)
                {
                    int count;

                    if (wordCount.HasValue)
                    {
                        count =
                            wordCount.Value;
                    }
                    else
                    {
                        int byteCount =
                            Encoding.ASCII
                                .GetByteCount(
                                    text);

                        count =
                            Math.Max(
                                1,
                                (byteCount + 2) / 2);
                    }

                    WriteStringToMemory(
                        address,
                        text,
                        count);

                    return true;
                }

                // Raw words
                if (value is ushort[] words)
                {
                    int count =
                        wordCount.HasValue
                            ? Math.Min(
                                wordCount.Value,
                                words.Length)
                            : words.Length;

                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        _memory.WriteWord(
                            address + i,
                            words[i]);
                    }

                    // 명시한 나머지 영역 초기화
                    if (wordCount.HasValue)
                    {
                        for (int i = count;
                             i < wordCount.Value;
                             i++)
                        {
                            _memory.WriteWord(
                                address + i,
                                0);
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC Unbound Set ERROR : " +
                    $"{ex.Message}");

                return false;
            }
        }

        // =========================================================
        // GENERIC MEMORY READ
        // =========================================================

        private object? ReadMemoryValue(
            string address,
            Type type,
            int? wordCount)
        {
            if (!TryParseAddress(
                    address,
                    out char device,
                    out int deviceAddress))
            {
                return null;
            }

            // bool
            if (type == typeof(bool))
            {
                if (device == 'B')
                {
                    return _memory.ReadBit(
                        deviceAddress);
                }

                if (device == 'W')
                {
                    return _memory.ReadWord(
                        deviceAddress) != 0;
                }

                return null;
            }

            if (device != 'W')
                return null;

            // 8bit
            if (type == typeof(byte))
            {
                return (byte)(
                    _memory.ReadWord(
                        deviceAddress)
                    & 0xFF);
            }

            if (type == typeof(sbyte))
            {
                return unchecked(
                    (sbyte)(
                        _memory.ReadWord(
                            deviceAddress)
                        & 0xFF));
            }

            // 16bit
            if (type == typeof(ushort))
            {
                return _memory.ReadWord(
                    deviceAddress);
            }

            if (type == typeof(short))
            {
                return unchecked(
                    (short)
                    _memory.ReadWord(
                        deviceAddress));
            }

            // 32bit
            if (type == typeof(uint))
            {
                return _memory.ReadDWord(
                    deviceAddress);
            }

            if (type == typeof(int))
            {
                return unchecked(
                    (int)
                    _memory.ReadDWord(
                        deviceAddress));
            }

            if (type == typeof(float))
            {
                uint raw =
                    _memory.ReadDWord(
                        deviceAddress);

                return BitConverter.ToSingle(
                    BitConverter.GetBytes(
                        raw),
                    0);
            }

            // 64bit
            if (type == typeof(ulong))
            {
                return _memory.ReadQWord(
                    deviceAddress);
            }

            if (type == typeof(long))
            {
                return unchecked(
                    (long)
                    _memory.ReadQWord(
                        deviceAddress));
            }

            if (type == typeof(double))
            {
                ulong raw =
                    _memory.ReadQWord(
                        deviceAddress);

                return BitConverter.ToDouble(
                    BitConverter.GetBytes(
                        raw),
                    0);
            }

            // String
            if (type == typeof(string))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "String GetValue requires WordCount.");
                }

                return ReadStringFromMemory(
                    deviceAddress,
                    wordCount.Value);
            }

            // ushort[]
            if (type == typeof(ushort[]))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "ushort[] GetValue requires WordCount.");
                }

                ushort[] result =
                    new ushort[
                        wordCount.Value];

                for (int i = 0;
                     i < result.Length;
                     i++)
                {
                    result[i] =
                        _memory.ReadWord(
                            deviceAddress + i);
                }

                return result;
            }

            // byte[]
            if (type == typeof(byte[]))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "byte[] GetValue requires WordCount.");
                }

                byte[] result =
                    new byte[
                        wordCount.Value * 2];

                int output = 0;

                for (int i = 0;
                     i < wordCount.Value;
                     i++)
                {
                    ushort word =
                        _memory.ReadWord(
                            deviceAddress + i);

                    result[output++] =
                        (byte)(
                            word & 0xFF);

                    result[output++] =
                        (byte)(
                            word >> 8);
                }

                return result;
            }

            throw new NotSupportedException(
                $"Unsupported PLC Type : " +
                $"{type.Name}");
        }

        // =========================================================
        // CONVERT PROPERTY -> PLC TYPE
        // =========================================================

        private object? ConvertToPlcType(
            object? value,
            PlcDataType dataType)
        {
            if (value == null)
                return null;

            string name =
                dataType.ToString();

            if (name.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Equals(
                    "Single",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToSingle(
                    value);
            }

            return dataType switch
            {
                PlcDataType.Bool =>
                    Convert.ToBoolean(value),

                PlcDataType.UInt16 =>
                    Convert.ToUInt16(value),

                PlcDataType.UInt32 =>
                    Convert.ToUInt32(value),

                PlcDataType.Int16 =>
                    Convert.ToInt16(value),

                PlcDataType.Int32 =>
                    Convert.ToInt32(value),

                PlcDataType.Double =>
                    Convert.ToDouble(value),

                PlcDataType.String =>
                    Convert.ToString(value),

                _ =>
                    value
            };
        }

        // =========================================================
        // CONVERT PLC -> PROPERTY TYPE
        // =========================================================

        private object? ConvertFromPlcType(
            object? value,
            PlcDataType dataType,
            Type propertyType)
        {
            if (value == null)
            {
                if (Nullable.GetUnderlyingType(
                        propertyType) != null ||
                    !propertyType.IsValueType)
                {
                    return null;
                }

                throw new InvalidOperationException(
                    $"{propertyType.Name} cannot be null.");
            }

            Type targetType =
                Nullable.GetUnderlyingType(
                    propertyType)
                ?? propertyType;

            if (dataType ==
                PlcDataType.String)
            {
                return Convert.ToString(
                    value);
            }

            string name =
                dataType.ToString();

            if (name.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Equals(
                    "Single",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ChangeType(
                    Convert.ToSingle(value),
                    targetType);
            }

            return Convert.ChangeType(
                value,
                targetType);
        }

        // =========================================================
        // ADDRESS
        //
        // B/W 주소는 HEX
        // =========================================================

        private static string NormalizeAddress(
            string address)
        {
            return address
                .Trim()
                .ToUpperInvariant();
        }

        private static bool TryParseAddress(
            string address,
            out char device,
            out int deviceAddress)
        {
            device = '\0';
            deviceAddress = 0;

            if (string.IsNullOrWhiteSpace(
                    address) ||
                address.Length < 2)
            {
                return false;
            }

            string normalized =
                NormalizeAddress(
                    address);

            device =
                normalized[0];

            if (device != 'B' &&
                device != 'W')
            {
                return false;
            }

            return int.TryParse(
                normalized[1..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out deviceAddress);
        }
    }
}




