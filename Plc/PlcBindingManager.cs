using MaterialControlSimulator.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControlSimulator.Plc
{
    public class PlcBindingManager
    {
        private readonly NodeRegistry _nodeRegistry;

        public PlcBindingManager(NodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry;
        }

        public PlcBindingInfo? Find(string address)
        {
            foreach (var node in _nodeRegistry.Nodes)
            {
                var binding = node.PlcBindings
                    .FirstOrDefault(x =>
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

        public object? GetValue(string address)
        {
            var info = Find(address);

            if (info == null)
                return null;

            var property = info.Node.GetType()
                .GetProperty(info.Binding.PropertyName);

            if (property == null || !property.CanRead)
                return null;

            var value = property.GetValue(info.Node);

            return ConvertToPlcType(
                value,
                info.Binding.DataType);
        }

        public bool SetValue(string address, object? value)
        {
            var info = Find(address);

            if (info == null)
                return false;

            var property = info.Node.GetType()
                .GetProperty(info.Binding.PropertyName);

            if (property == null || !property.CanWrite)
                return false;

            try
            {
                var convertedValue =
                    ConvertFromPlcType(
                        value,
                        info.Binding.DataType,
                        property.PropertyType);

                property.SetValue(
                    info.Node,
                    convertedValue);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private object? ConvertToPlcType(object? value, PlcDataType dataType)
        {
            if (value == null)
                return null;

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

                _ => value
            };
        }

        private object? ConvertFromPlcType(object? value, PlcDataType dataType, Type propertyType)
        {
            if (value == null)
            {
                if (Nullable.GetUnderlyingType(propertyType) != null ||
                    !propertyType.IsValueType)
                {
                    return null;
                }

                throw new InvalidOperationException(
                    $"'{propertyType.Name}' cannot be null.");
            }

            var targetType =
                Nullable.GetUnderlyingType(propertyType)
                ?? propertyType;

            return dataType switch
            {
                PlcDataType.Bool =>
                    Convert.ToBoolean(value),

                PlcDataType.UInt16 =>
                    Convert.ChangeType(
                        value,
                        targetType),

                PlcDataType.UInt32 =>
                    Convert.ChangeType(
                        value,
                        targetType),

                PlcDataType.Int16 =>
                    Convert.ChangeType(
                        value,
                        targetType),

                PlcDataType.Int32 =>
                    Convert.ChangeType(
                        value,
                        targetType),

                PlcDataType.Double =>
                    Convert.ChangeType(
                        value,
                        targetType),

                PlcDataType.String =>
                    Convert.ToString(value),

                _ => Convert.ChangeType(
                    value,
                    targetType)
            };
        }
    }
}
