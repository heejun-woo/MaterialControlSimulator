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

namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// LocationControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LocationControl : NodeControl
    {
        public LocationControl()
        {
            InitializeComponent();
            SelectionChanged += OnSelectionChanged;
        }
        private void OnSelectionChanged(bool selected)
        {
            NodeBorder.BorderBrush = selected
                ? Brushes.Cyan
                : new SolidColorBrush(Color.FromRgb(83, 96, 108));

            NodeBorder.BorderThickness =
                selected
                    ? new Thickness(3)
                    : new Thickness(2);
        }
    }
}
