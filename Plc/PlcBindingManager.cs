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
        private readonly NodeRegistry _nodeRegistry;
        private readonly PlcMemory _memory;

        private readonly Dictionary<string, CachedBinding>
            _bindings =
                new(StringComparer.OrdinalIgnoreCase);

        private readonly object _bindingLock =
            new();

        // ============================================================
        // Cached Binding
        // ============================================================

        private sealed class CachedBinding
        {
            // ★ 여기 타입은 네 NodeRegistry.Nodes의 실제 타입에 맞춰야 함.
            //
            // 현재 object로 두면 PlcBindingInfo.Node에 넣을 때
            // 명시적 변환 문제가 생길 수 있으므로
            // Find()에서 기존 Node를 다시 사용하도록 처리함.

            public required object Node { get; init; }

            public required PlcBinding Binding { get; init; }

            public required PropertyInfo Property { get; init; }
        }

        // ============================================================
        // Constructor
        // ============================================================

        public PlcBindingManager(
            NodeRegistry nodeRegistry,
            PlcMemory memory)
        {
            _nodeRegistry = nodeRegistry;
            _memory = memory;

            // 여기서 RebuildCache 하지 않음.
            //
            // Node가 아직 로딩되지 않았을 가능성이 있기 때문.
            //
            // Node 로딩 완료 후:
            //
            // _plcBindingManager.RebuildCache();
        }

        // ============================================================
        // CACHE
        // ============================================================

        public void RebuildCache()
        {
            lock (_bindingLock)
            {
                _bindings.Clear();

                foreach (var node in _nodeRegistry.Nodes)
                {
                    foreach (var binding in node.PlcBindings)
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
                            node.GetType().GetProperty(
                                binding.PropertyName);

                        if (property == null)
                        {
                            Debug.WriteLine(
                                $"PLC Property Not Found : " +
                                $"{binding.Address} -> " +
                                $"{binding.PropertyName}");

                            continue;
                        }

                        _bindings[binding.Address] =
                            new CachedBinding
                            {
                                Node = node,
                                Binding = binding,
                                Property = property
                            };
                    }
                }

                Debug.WriteLine(
                    $"PLC Binding Cache Count = " +
                    $"{_bindings.Count}");
            }

            // Node 초기값을 PLC Memory에 반영
            SyncAllBindingsToMemory();
        }

        // ============================================================
        // FIND CACHE
        // ============================================================

        private CachedBinding? FindCached(
            string address)
        {
            lock (_bindingLock)
            {
                if (_bindings.TryGetValue(
                        address,
                        out CachedBinding? cached))
                {
                    return cached;
                }
            }

            // --------------------------------------------------------
            // Cache miss
            //
            // Node가 RebuildCache 이후 추가되었을 수도 있으므로
            // 최초 한 번 Registry에서 검색.
            // --------------------------------------------------------

            foreach (var node in _nodeRegistry.Nodes)
            {
                var binding =
                    node.PlcBindings.FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Address,
                                address,
                                StringComparison.OrdinalIgnoreCase));

                if (binding == null)
                    continue;

                PropertyInfo? property =
                    node.GetType().GetProperty(
                        binding.PropertyName);

                if (property == null)
                    return null;

                var newCached =
                    new CachedBinding
                    {
                        Node = node,
                        Binding = binding,
                        Property = property
                    };

                lock (_bindingLock)
                {
                    _bindings[address] =
                        newCached;
                }

                return newCached;
            }

            return null;
        }

        // ============================================================
        // FIND
        //
        // 기존 코드와 호환용
        // ============================================================

        public PlcBindingInfo? Find(
            string address)
        {
            // CachedBinding.Node 타입 문제를 피하면서
            // 기존 PlcBindingInfo 타입을 그대로 유지하기 위해
            // Registry에서 실제 node를 반환한다.
            //
            // 이 메서드는 MC Read loop에서 사용하면 안 됨.

            foreach (var node in _nodeRegistry.Nodes)
            {
                var binding =
                    node.PlcBindings.FirstOrDefault(
                        x =>
                            string.Equals(
                                x.Address,
                                address,
                                StringComparison.OrdinalIgnoreCase));

                if (binding != null)
                {
                    return new PlcBindingInfo
                    {
                        Node = node,
                        Binding = binding
                    };
                }
            }

            return null;
        }

        // ============================================================
        // GET DATA TYPE
        // ============================================================

        public PlcDataType? GetDataType(
            string address)
        {
            CachedBinding? cached =
                FindCached(address);

            return cached?.Binding.DataType;
        }

        // ============================================================
        // SET VALUE
        //
        // Binding 있음:
        //   Binding.DataType / WordCount 자동 사용
        //   Memory + Node Property 갱신
        //
        // Binding 없음:
        //   value의 실제 C# 타입으로 Memory Write
        // ============================================================

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

            CachedBinding? cached =
                FindCached(address);

            // --------------------------------------------------------
            // Binding 없음
            // --------------------------------------------------------

            if (cached == null)
            {
                return WriteUnboundValue(
                    device,
                    deviceAddress,
                    value,
                    null);
            }

            try
            {
                object? converted =
                    ConvertFromPlcType(
                        value,
                        cached.Binding.DataType,
                        cached.Property.PropertyType);

                if (converted == null)
                    return false;

                // ----------------------------------------------------
                // PLC Memory 먼저 갱신
                // ----------------------------------------------------

                if (device == 'B')
                {
                    _memory.WriteBit(
                        deviceAddress,
                        Convert.ToBoolean(converted));
                }
                else if (device == 'W')
                {
                    WriteValueToMemory(
                        deviceAddress,
                        converted,
                        cached.Binding.DataType,
                        cached.Binding.WordCount);
                }

                // Memory Write는 성공.
                //
                // Property가 쓰기 불가능해도 PLC Memory 값은 유지.
                if (!cached.Property.CanWrite)
                    return true;

                // ----------------------------------------------------
                // Node Property
                // ----------------------------------------------------

                SetNodeProperty(
                    cached,
                    converted,
                    address);

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

        // ============================================================
        // SET VALUE + WORD COUNT
        //
        // Binding이 없을 때 String / Array 등의
        // 고정 영역 크기를 지정할 수 있음.
        //
        // Binding이 있으면 Binding.WordCount가 우선.
        // ============================================================

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

            CachedBinding? cached =
                FindCached(address);

            // Binding이 있으면 Binding 설정 사용
            if (cached != null)
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

        // ============================================================
        // GET VALUE
        //
        // Binding 있음:
        //   Binding DataType / WordCount 기준 Memory Read
        //
        // Binding 없음:
        //   B -> bool
        //   W -> ushort
        // ============================================================

        // ============================================================
        // GET VALUE
        //
        // Binding 있음
        //   -> Binding.DataType / WordCount 자동 사용
        //
        // Binding 없음
        //   -> B = bool
        //   -> W = ushort
        // ============================================================

        public object? GetValue(
            string address)
        {
            try
            {
                if (!TryParseAddress(
                        address,
                        out char device,
                        out int deviceAddress))
                {
                    return null;
                }

                CachedBinding? cached =
                    FindCached(address);

                // ====================================================
                // Binding 있음
                // ====================================================

                if (cached != null)
                {
                    return ReadBoundValue(
                        device,
                        deviceAddress,
                        cached.Binding.DataType,
                        cached.Binding.WordCount);
                }

                // ====================================================
                // Binding 없음
                //
                // 타입 정보가 없으므로
                // 기본 PLC 단위로 반환
                // ====================================================

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
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC GetValue ERROR " +
                    $"{address}: {ex.Message}");

                return null;
            }
        }


        // ============================================================
        // GENERIC GET
        //
        // Binding이 있어도 T를 명시하면
        // 요청한 T 기준으로 Memory를 읽음.
        //
        // 예:
        //
        // GetValue<ushort>("W100")
        // GetValue<int>("W100")
        // GetValue<uint>("W100")
        // GetValue<float>("W100")
        // GetValue<double>("W100")
        // ============================================================

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


        // ============================================================
        // GENERIC GET + WORD COUNT
        //
        // String / Array처럼 크기가 필요한 경우.
        //
        // 예:
        //
        // GetValue<string>("W100", 20)
        // GetValue<ushort[]>("W100", 20)
        // ============================================================

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


        // ============================================================
        // GET VALUE + TYPE
        //
        // Generic을 사용하기 어려운 곳에서 사용 가능.
        //
        // 예:
        //
        // GetValue("W100", typeof(int))
        // GetValue("W100", typeof(string), 20)
        // ============================================================

        public object? GetValue(
            string address,
            Type type)
        {
            try
            {
                return ReadMemoryValue(
                    address,
                    type,
                    null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC GetValue ERROR " +
                    $"{address}: {ex.Message}");

                return null;
            }
        }


        public object? GetValue(
            string address,
            Type type,
            int wordCount)
        {
            if (wordCount <= 0)
                return null;

            try
            {
                return ReadMemoryValue(
                    address,
                    type,
                    wordCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC GetValue ERROR " +
                    $"{address}: {ex.Message}");

                return null;
            }
        }

  

        // ============================================================
        // READ MEMORY VALUE
        //
        // Binding 여부와 관계없이
        // 실제 PLC Memory를 지정된 C# Type으로 해석한다.
        // ============================================================

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

            // ========================================================
            // BOOL
            // ========================================================

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

            // B는 bool 이외 타입을 지원하지 않음
            if (device != 'W')
                return null;

            // ========================================================
            // BYTE
            // ========================================================

            if (type == typeof(byte))
            {
                ushort word =
                    _memory.ReadWord(
                        deviceAddress);

                return (byte)(
                    word & 0xFF);
            }

            // ========================================================
            // SBYTE
            // ========================================================

            if (type == typeof(sbyte))
            {
                ushort word =
                    _memory.ReadWord(
                        deviceAddress);

                return unchecked(
                    (sbyte)(word & 0xFF));
            }

            // ========================================================
            // UINT16
            // 1 Word
            // ========================================================

            if (type == typeof(ushort))
            {
                return _memory.ReadWord(
                    deviceAddress);
            }

            // ========================================================
            // INT16
            // 1 Word
            // ========================================================

            if (type == typeof(short))
            {
                return unchecked(
                    (short)_memory.ReadWord(
                        deviceAddress));
            }

            // ========================================================
            // UINT32
            // 2 Words
            // ========================================================

            if (type == typeof(uint))
            {
                return _memory.ReadDWord(
                    deviceAddress);
            }

            // ========================================================
            // INT32
            // 2 Words
            // ========================================================

            if (type == typeof(int))
            {
                return unchecked(
                    (int)_memory.ReadDWord(
                        deviceAddress));
            }

            // ========================================================
            // FLOAT
            // 2 Words
            // ========================================================

            if (type == typeof(float))
            {
                uint raw =
                    _memory.ReadDWord(
                        deviceAddress);

                return BitConverter.ToSingle(
                    BitConverter.GetBytes(raw),
                    0);
            }

            // ========================================================
            // UINT64
            // 4 Words
            // ========================================================

            if (type == typeof(ulong))
            {
                return _memory.ReadQWord(
                    deviceAddress);
            }

            // ========================================================
            // INT64
            // 4 Words
            // ========================================================

            if (type == typeof(long))
            {
                return unchecked(
                    (long)_memory.ReadQWord(
                        deviceAddress));
            }

            // ========================================================
            // DOUBLE
            // 4 Words
            // ========================================================

            if (type == typeof(double))
            {
                ulong raw =
                    _memory.ReadQWord(
                        deviceAddress);

                return BitConverter.ToDouble(
                    BitConverter.GetBytes(raw),
                    0);
            }

            // ========================================================
            // STRING
            //
            // 반드시 WordCount 필요
            // ========================================================

            if (type == typeof(string))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "String을 읽으려면 WordCount가 필요합니다.");
                }

                return ReadStringFromMemory(
                    deviceAddress,
                    wordCount.Value);
            }

            // ========================================================
            // ushort[]
            // ========================================================

            if (type == typeof(ushort[]))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "ushort[]를 읽으려면 WordCount가 필요합니다.");
                }

                ushort[] result =
                    new ushort[wordCount.Value];

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

            // ========================================================
            // short[]
            // ========================================================

            if (type == typeof(short[]))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "short[]를 읽으려면 WordCount가 필요합니다.");
                }

                short[] result =
                    new short[wordCount.Value];

                for (int i = 0;
                     i < result.Length;
                     i++)
                {
                    result[i] =
                        unchecked(
                            (short)_memory.ReadWord(
                                deviceAddress + i));
                }

                return result;
            }

            // ========================================================
            // byte[]
            //
            // WordCount 기준이므로
            // 결과 byte 수 = WordCount * 2
            // ========================================================

            if (type == typeof(byte[]))
            {
                if (!wordCount.HasValue ||
                    wordCount.Value <= 0)
                {
                    throw new ArgumentException(
                        "byte[]를 읽으려면 WordCount가 필요합니다.");
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
                $"지원하지 않는 PLC 타입: " +
                $"{type.Name}");
        }

        // ============================================================
        // GET WORDS
        // ============================================================

        public ushort[] GetWords(
            string address,
            int wordCount)
        {
            if (wordCount <= 0)
                return Array.Empty<ushort>();

            if (!TryParseAddress(
                    address,
                    out char device,
                    out int startAddress))
            {
                return Array.Empty<ushort>();
            }

            if (device != 'W')
                return Array.Empty<ushort>();

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

        // ============================================================
        // SET WORDS
        // ============================================================

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
                    out int startAddress))
            {
                return false;
            }

            if (device != 'W')
                return false;

            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                _memory.WriteWord(
                    startAddress + i,
                    values[i]);
            }

            // --------------------------------------------------------
            // 해당 범위에 Binding이 있다면 Node에도 반영
            // --------------------------------------------------------

            RefreshBindingsInRange(
                startAddress,
                values.Length);

            return true;
        }

        // ============================================================
        // READ BIT
        // ============================================================

        public bool ReadBit(
            int address)
        {
            return _memory.ReadBit(
                address);
        }

        // ============================================================
        // WRITE BIT
        // ============================================================

        public void WriteBit(
            int address,
            bool value)
        {
            _memory.WriteBit(
                address,
                value);

            string plcAddress =
                $"B{address:X}";

            CachedBinding? cached =
                FindCached(plcAddress);

            if (cached == null)
                return;

            if (!cached.Property.CanWrite)
                return;

            try
            {
                object? converted =
                    ConvertFromPlcType(
                        value,
                        cached.Binding.DataType,
                        cached.Property.PropertyType);

                SetNodeProperty(
                    cached,
                    converted,
                    plcAddress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC WriteBit ERROR " +
                    $"{plcAddress}: {ex.Message}");
            }
        }

        // ============================================================
        // READ WORD
        // ============================================================

        public ushort ReadWord(
            int address)
        {
            return _memory.ReadWord(
                address);
        }

        // ============================================================
        // WRITE WORD
        //
        // Raw Memory Write
        // ============================================================

        public void WriteWord(
            int address,
            ushort value)
        {
            _memory.WriteWord(
                address,
                value);
        }

        // ============================================================
        // SYNC ALL BINDINGS -> MEMORY
        // ============================================================

        public void SyncAllBindingsToMemory()
        {
            List<string> addresses;

            lock (_bindingLock)
            {
                addresses =
                    _bindings.Keys.ToList();
            }

            foreach (string address in addresses)
            {
                SyncBindingToMemory(
                    address);
            }
        }

        // ============================================================
        // SYNC ONE BINDING -> MEMORY
        // ============================================================

        public void SyncBindingToMemory(
            string address)
        {
            CachedBinding? cached =
                FindCached(address);

            if (cached == null)
                return;

            if (!cached.Property.CanRead)
                return;

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
                    GetNodeProperty(
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
                        Convert.ToBoolean(converted));

                    return;
                }

                if (device == 'W')
                {
                    WriteValueToMemory(
                        deviceAddress,
                        converted,
                        cached.Binding.DataType,
                        cached.Binding.WordCount);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC Sync ERROR " +
                    $"{address}: {ex.Message}");
            }
        }

        // ============================================================
        // MC WRITE 후 Binding 갱신
        // ============================================================

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

            List<(int Address, CachedBinding Binding)>
                affected =
                    new();

            lock (_bindingLock)
            {
                foreach (var pair in _bindings)
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

                    // ------------------------------------------------
                    // Binding 자체가 여러 Word일 수 있으므로
                    // 영역이 겹치는지도 확인
                    // ------------------------------------------------

                    int bindingWords =
                        Math.Max(
                            1,
                            pair.Value.Binding.WordCount);

                    int bindingEnd =
                        address +
                        bindingWords -
                        1;

                    bool overlap =
                        address <= endAddress &&
                        bindingEnd >= startAddress;

                    if (!overlap)
                        continue;

                    affected.Add(
                        (address, pair.Value));
                }
            }

            foreach (var item in
                     affected.OrderBy(
                         x => x.Address))
            {
                RefreshBinding(
                    item.Address,
                    item.Binding);
            }
        }

        // ============================================================
        // REFRESH ONE BINDING
        // ============================================================

        public void RefreshBinding(
            int address)
        {
            CachedBinding? cached =
                FindCached(
                    $"W{address:X}");

            if (cached == null)
                return;

            RefreshBinding(
                address,
                cached);
        }

        private void RefreshBinding(
            int address,
            CachedBinding cached)
        {
            if (!cached.Property.CanWrite)
                return;

            string plcAddress =
                $"W{address:X}";

            try
            {
                object? value =
                    ReadBoundValue(
                        'W',
                        address,
                        cached.Binding.DataType,
                        cached.Binding.WordCount);

                if (value == null)
                    return;

                object? converted =
                    ConvertFromPlcType(
                        value,
                        cached.Binding.DataType,
                        cached.Property.PropertyType);

                SetNodeProperty(
                    cached,
                    converted,
                    plcAddress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"PLC Refresh ERROR " +
                    $"{plcAddress}: {ex.Message}");
            }
        }

        // ============================================================
        // NODE PROPERTY GET
        // ============================================================

        private object? GetNodeProperty(
            CachedBinding cached)
        {
            if (!cached.Property.CanRead)
                return null;

            if (cached.Node is
                    DispatcherObject dispatcher &&
                !dispatcher.Dispatcher.CheckAccess())
            {
                return dispatcher.Dispatcher.Invoke(
                    () =>
                        cached.Property.GetValue(
                            cached.Node));
            }

            return cached.Property.GetValue(
                cached.Node);
        }

        // ============================================================
        // NODE PROPERTY SET
        // ============================================================

        private void SetNodeProperty(
            CachedBinding cached,
            object? value,
            string address)
        {
            if (!cached.Property.CanWrite)
                return;

            if (cached.Node is
                    DispatcherObject dispatcher &&
                !dispatcher.Dispatcher.CheckAccess())
            {
                // 통신 thread를 UI thread 때문에
                // 기다리게 하지 않음.
                dispatcher.Dispatcher.BeginInvoke(
                    new Action(
                        () =>
                        {
                            try
                            {
                                cached.Property.SetValue(
                                    cached.Node,
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
                cached.Node,
                value);
        }

        // ============================================================
        // WRITE BOUND VALUE -> MEMORY
        // ============================================================

        private void WriteValueToMemory(
            int address,
            object value,
            PlcDataType dataType,
            int wordCount)
        {
            string typeName =
                dataType.ToString();

            // BOOL
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

            // UINT16
            if (typeName.Equals(
                    "UInt16",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteWord(
                    address,
                    Convert.ToUInt16(value));

                return;
            }

            // INT16
            if (typeName.Equals(
                    "Int16",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteWord(
                    address,
                    unchecked(
                        (ushort)Convert.ToInt16(
                            value)));

                return;
            }

            // UINT32
            if (typeName.Equals(
                    "UInt32",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteDWord(
                    address,
                    Convert.ToUInt32(value));

                return;
            }

            // INT32
            if (typeName.Equals(
                    "Int32",
                    StringComparison.OrdinalIgnoreCase))
            {
                _memory.WriteDWord(
                    address,
                    unchecked(
                        (uint)Convert.ToInt32(
                            value)));

                return;
            }

            // FLOAT
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

            // DOUBLE
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

            // STRING
            if (typeName.Equals(
                    "String",
                    StringComparison.OrdinalIgnoreCase))
            {
                WriteStringToMemory(
                    address,
                    Convert.ToString(value)
                        ?? string.Empty,
                    Math.Max(
                        1,
                        wordCount));

                return;
            }

            throw new NotSupportedException(
                $"Unsupported PlcDataType : " +
                $"{dataType}");
        }

        // ============================================================
        // STRING WRITE
        //
        // 중요:
        // 무조건 WordCount 전체 영역을 덮어씀.
        // ============================================================

        private void WriteStringToMemory(
            int address,
            string value,
            int wordCount)
        {
            if (wordCount <= 0)
                return;

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

            // --------------------------------------------------------
            // buffer의 남는 부분은 전부 0.
            //
            // 따라서 짧은 문자열이나 ""를 넣어도
            // WordCount 전체가 깨끗하게 지워짐.
            // --------------------------------------------------------

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

        // ============================================================
        // STRING READ
        // ============================================================

        private string ReadStringFromMemory(
            int address,
            int wordCount)
        {
            if (wordCount <= 0)
                return string.Empty;

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

        // ============================================================
        // READ BOUND VALUE FROM MEMORY
        // ============================================================

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
                    (short)_memory.ReadWord(
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
                    (int)_memory.ReadDWord(
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
                    BitConverter.GetBytes(raw),
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
                    BitConverter.GetBytes(raw),
                    0);
            }

            if (typeName.Equals(
                    "String",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ReadStringFromMemory(
                    address,
                    Math.Max(
                        1,
                        wordCount));
            }

            return null;
        }

        // ============================================================
        // UNBOUND VALUE WRITE
        // ============================================================

        private bool WriteUnboundValue(
            char device,
            int address,
            object value,
            int? wordCount)
        {
            try
            {
                // ----------------------------------------------------
                // B
                // ----------------------------------------------------

                if (device == 'B')
                {
                    _memory.WriteBit(
                        address,
                        Convert.ToBoolean(value));

                    return true;
                }

                if (device != 'W')
                    return false;

                // ----------------------------------------------------
                // 8 BIT
                // ----------------------------------------------------

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

                // ----------------------------------------------------
                // 16 BIT
                // ----------------------------------------------------

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

                // ----------------------------------------------------
                // 32 BIT
                // ----------------------------------------------------

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

                // ----------------------------------------------------
                // 64 BIT
                // ----------------------------------------------------

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

                // ----------------------------------------------------
                // STRING
                // ----------------------------------------------------

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
                        // WordCount가 없으면
                        // 현재 문자열 + NULL이 들어갈 만큼만 확보.

                        int byteCount =
                            Encoding.ASCII.GetByteCount(
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

                // ----------------------------------------------------
                // ushort[]
                // ----------------------------------------------------

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

                    // WordCount를 명시했다면
                    // 남는 영역 0으로 초기화
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
                    $"PLC Unbound Write ERROR : " +
                    $"{ex.Message}");

                return false;
            }
        }


        // ============================================================
        // CONVERT TO PLC TYPE
        // ============================================================

        private object? ConvertToPlcType(
            object? value,
            PlcDataType dataType)
        {
            if (value == null)
                return null;

            string typeName =
                dataType.ToString();

            if (typeName.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals(
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

        // ============================================================
        // CONVERT FROM PLC TYPE
        // ============================================================

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
                    $"'{propertyType.Name}' " +
                    $"cannot be null.");
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

            string typeName =
                dataType.ToString();

            if (typeName.Equals(
                    "Float",
                    StringComparison.OrdinalIgnoreCase) ||
                typeName.Equals(
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

        // ============================================================
        // ADDRESS
        //
        // B/W 주소는 HEX 기준
        //
        // W200 -> 0x200
        // B3000 -> 0x3000
        // ============================================================

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

            device =
                char.ToUpperInvariant(
                    address[0]);

            if (device != 'B' &&
                device != 'W')
            {
                return false;
            }

            return int.TryParse(
                address[1..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out deviceAddress);
        }
    }
}



