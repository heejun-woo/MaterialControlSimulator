using MaterialControlSimulator.Plc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// HostRequestControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class HostRequestControl : NodeControl
    {
        public HostRequestControl()
        {
            InitializeComponent();

            MouseLeftButtonUp += (s, e) =>
            {
                if (IsAlarm)
                    RetryRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        public event EventHandler? RetryRequested;

        public bool IsAlarm
        {
            get => (bool)GetValue(IsAlarmProperty);
            set => SetValue(IsAlarmProperty, value);
        }

        public static readonly DependencyProperty IsAlarmProperty =
            DependencyProperty.Register(
                nameof(IsAlarm),
                typeof(bool),
                typeof(HostRequestControl),
                new PropertyMetadata(false));

        // PLC Address
        public int RequestAddress { get; set; }

        public int ResponseAddress { get; set; }

        // Host 응답값별 목적지
        public string Destination1 { get; set; } = "";
        public string Destination2 { get; set; } = "";
        public string Destination3 { get; set; } = "";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
