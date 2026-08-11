using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc
{
    public enum PlcDeviceType
    {
        B,
        W
    }

    public enum PlcDataType
    {
        Bool,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Float,
        Double,
        String
    }

    public class PlcBinding
    {
        public string PropertyName { get; set; } = "";

        public string Address { get; set; } = "";

        public PlcDeviceType DeviceType { get; set; }

        public PlcDataType DataType { get; set; }

        public int WordCount { get; set; } = 1;
        public static int GetWordCount(PlcDataType type)
        {
            return type switch
            {
                PlcDataType.Bool => 1,
                PlcDataType.Int16 => 1,
                PlcDataType.UInt16 => 1,
                PlcDataType.Int32 => 2,
                PlcDataType.UInt32 => 2,
                PlcDataType.Float => 2,
                PlcDataType.Double => 4,
                PlcDataType.String => 1, // 문자열은 별도 길이 설정 필요
                _ => 1
            };
        }
    }

}

