using MaterialControlSimulator.Controls.HMI;
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

namespace MaterialControlSimulator
{
    /// <summary>
    /// EquipmentPlcWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class EquipmentPlcWindow : Window
    {
        private readonly EquipmentPlc plc;

        private readonly List<PlcBitGridItem> _bitItems = new List<PlcBitGridItem>();
        private readonly List<PlcWordGridItem> _wordItems = new List<PlcWordGridItem>();

        private readonly DispatcherTimer _refreshTimer;

        public EquipmentPlcWindow(EquipmentPlcConfig config)
        {
            InitializeComponent();
            plc = CreateEquipmentPlc(config);

            PlcNameText.Text = plc.Name;
            PortText.Text = plc.Port.ToString();

            #region Value Grid 관련
            InitializeGrid(0x3800);

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };

            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            Closed += (s, e) =>
            {
                _refreshTimer.Stop();
            };
            #endregion

            InitializeHmi();

            plc.Start();


        }

        private void InitializeHmi()
        {
            if (plc is LS_PKG_EL1_LEAK_CHECKER_PLC LS_EL1)
            {
                var hmi = new LS_PKG_EL1_LEAK_CHECKER(LS_EL1);
                HmiContainer.Children.Add(hmi);
            }
        }

        private EquipmentPlc CreateEquipmentPlc(EquipmentPlcConfig config)
        {
            switch (config.Type)
            {
                case "LS_PKG_EL1":
                    return new LS_PKG_EL1_LEAK_CHECKER_PLC(config);

                default:
                    return new EquipmentPlc(config);
            }
        }


        #region Value Grid 관련
        private void InitializeGrid(int StartAddress)
        {
            for (int i = StartAddress; i <= StartAddress + 4098; i++)
            {
                string bAddress = $"B{i:X}";

                _bitItems.Add(
                    new PlcBitGridItem
                    {
                        Address = bAddress,
                        Value = plc.BindingManager.GetValue<bool>(bAddress)
                    });

                string wAddress = $"W{i:X}";

                _wordItems.Add(
                    new PlcWordGridItem
                    {
                        Address = wAddress,
                        Value = plc.BindingManager.GetValue<ushort>(wAddress)
                    });
            }

            BitGrid.ItemsSource = _bitItems;
            WordGrid.ItemsSource = _wordItems;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            foreach (var item in _bitItems)
            {
                item.Value = plc.BindingManager.GetValue<bool>(item.Address);
            }

            foreach (var item in _wordItems)
            {
                item.Value = plc.BindingManager.GetValue<ushort>(item.Address);
            }
        }

        private void BitGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not PlcBitGridItem item)
                return;

            if (e.EditingElement is not CheckBox checkBox)
                return;

            bool value = checkBox.IsChecked == true;

            plc.BindingManager.SetValue(item.Address, value);
        }

        private void WordGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not PlcWordGridItem item)
                return;

            if (e.EditingElement is not TextBox textBox)
                return;

            if (!ushort.TryParse(textBox.Text, out ushort value))
                return;

            plc.BindingManager.SetValue(item.Address, value);
        }
        #endregion
    }
}

