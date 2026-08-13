using MaterialControlSimulator.Plc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MaterialControlSimulator.Controls
{
    public partial class CommunicationStatusControl :
        UserControl,
        IPlcBindable
    {
        public ObservableCollection<PlcBinding> PlcBindings { get; } = new();
        // =====================================================
        // B3808
        // true  = Communication ON
        // false = Communication OFF
        // =====================================================

        public bool IsCommunicationOn
        {
            get =>
                (bool)GetValue(
                    IsCommunicationOnProperty);

            set =>
                SetValue(
                    IsCommunicationOnProperty,
                    value);
        }

        public static readonly DependencyProperty
            IsCommunicationOnProperty =
                DependencyProperty.Register(
                    nameof(IsCommunicationOn),
                    typeof(bool),
                    typeof(CommunicationStatusControl),
                    new PropertyMetadata(
                        false,
                        OnCommunicationStatusChanged));

        public CommunicationStatusControl()
        {
            InitializeComponent();

            PlcBindings.Add(
                new PlcBinding
                {
                    Address = "B3808",

                    PropertyName =
                        nameof(IsCommunicationOn),

                    DataType =
                        PlcDataType.Bool,

                    WordCount = 1
                });

            UpdateStatus(false);
        }

        private static void OnCommunicationStatusChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control =
                (CommunicationStatusControl)d;

            control.UpdateStatus(
                (bool)e.NewValue);
        }

        private void UpdateStatus(
            bool isOn)
        {
            if (isOn)
            {
                StatusLamp.Fill =
                    Brushes.LimeGreen;

                StatusText.Text =
                    "COMM ON";
            }
            else
            {
                StatusLamp.Fill =
                    Brushes.IndianRed;

                StatusText.Text =
                    "COMM OFF";
            }
        }
    }
}