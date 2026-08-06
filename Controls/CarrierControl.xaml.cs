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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MaterialControlSimulator.Controls
{
    /// <summary>
    /// CarrierControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CarrierControl : UserControl
    {
        public static readonly DependencyProperty IdProperty =
           DependencyProperty.Register(
               nameof(Id),
               typeof(string),
               typeof(CarrierControl));

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public INode? CurrentNode { get; set; }

        public CarrierControl()
        {
            InitializeComponent();
            Loaded += CarrierControl_Loaded;
        }

        private void CarrierControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Carriers.Register(this);
        }

        public async Task MoveToAsync(Point target, double speed)
        {
            Point current = GetCurrentPosition();

            double distance = Math.Sqrt(
                Math.Pow(target.X - current.X, 2) +
                Math.Pow(target.Y - current.Y, 2));


            double seconds = distance / speed;


            await AnimateAsync(
                current,
                target,
                TimeSpan.FromSeconds(seconds));
        }


        private Point GetCurrentPosition()
        {
            double x = Canvas.GetLeft(this);
            double y = Canvas.GetTop(this);

            if (double.IsNaN(x))
                x = 0;

            if (double.IsNaN(y))
                y = 0;

            return new Point(x, y);
        }


        private Task AnimateAsync(Point from, Point to, TimeSpan duration)
        {
            var tcs = new TaskCompletionSource<bool>();

            int completed = 0;


            void Complete()
            {
                completed++;

                if (completed == 2)
                {
                    Canvas.SetLeft(this, to.X);
                    Canvas.SetTop(this, to.Y);

                    BeginAnimation(Canvas.LeftProperty, null);
                    BeginAnimation(Canvas.TopProperty, null);

                    tcs.TrySetResult(true);
                }
            }


            var xAnimation = new DoubleAnimation
            {
                From = from.X,
                To = to.X,
                Duration = duration
            };


            var yAnimation = new DoubleAnimation
            {
                From = from.Y,
                To = to.Y,
                Duration = duration
            };


            xAnimation.Completed += (s, e) => Complete();
            yAnimation.Completed += (s, e) => Complete();


            BeginAnimation(
                Canvas.LeftProperty,
                xAnimation);


            BeginAnimation(
                Canvas.TopProperty,
                yAnimation);


            return tcs.Task;
        }
    }
}
