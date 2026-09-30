using MaterialControlSimulator.Plc.EquipmentPlcWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MaterialControlSimulator.Controls.HMI
{
    /// <summary>
    /// StockerHmiControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LS_PKG_EL1_LEAK_CHECKER : UserControl
    {
        public LS_PKG_EL1_LEAK_CHECKER_PLC PLC;
        private readonly DispatcherTimer _timer;

        public LS_PKG_EL1_LEAK_CHECKER(LS_PKG_EL1_LEAK_CHECKER_PLC _plc)
        {
            InitializeComponent();
            PLC = _plc;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateCommStatus();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateHmiStatus();
        }

        #region UpdateHmiStatus
        private void UpdateHmiStatus()
        {
            UpdateCommStatus();
            UpdateAutoMode();
            UpdateEquipmentStatus();
        }

        private void UpdateCommStatus()
        {
            bool value = PLC.BindingManager.GetValue<bool>("B3808");

            CommStatusText.Text =
                value ? "● ONLINE" : "● OFFLINE";

            CommStatusText.Foreground =
                value ? Brushes.LimeGreen : Brushes.Red;
        }

        private void UpdateAutoMode()
        {
            bool value = PLC.BindingManager.GetValue<bool>("B380B");

            ModeStatusText.Text = value ? "AUTO" : "MANUAL";
            ModeStatusText.Foreground = value ? Brushes.LimeGreen : Brushes.White;
        }

        private void UpdateEquipmentStatus()
        {
            ushort status = PLC.BindingManager.GetValue<ushort>("W3810");

            switch (status)
            {
                case 1:
                    RunStatusText.Text = "RUN";
                    RunStatusText.Foreground = Brushes.LimeGreen;
                    break;

                case 2:
                    RunStatusText.Text = "WAIT";
                    RunStatusText.Foreground = Brushes.Yellow;
                    break;

                case 4:
                    RunStatusText.Text = "TROUBLE";
                    RunStatusText.Foreground = Brushes.Red;
                    break;

                case 8:
                    RunStatusText.Text = "USER STOP";
                    RunStatusText.Foreground = Brushes.Blue;
                    break;

                default:
                    RunStatusText.Text = "UNKNOWN";
                    RunStatusText.Foreground = Brushes.Gray;
                    break;
            }
        } 
        #endregion
    }
}
