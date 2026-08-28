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
    /// SpeechBubbleControl.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class SpeechBubbleControl : UserControl
    {
        public enum BubbleTailPosition
        {
            BottomLeft,
            BottomCenter,
            BottomRight,

            TopLeft,
            TopCenter,
            TopRight,

            LeftCenter,
            RightCenter
        }

        public SpeechBubbleControl()
        {
            InitializeComponent();

            Loaded += (s, e) => UpdateTail();
        }

        #region 풍선꼬리
        public static readonly DependencyProperty
TailPositionProperty =
DependencyProperty.Register(
    nameof(TailPosition),
    typeof(BubbleTailPosition),
    typeof(SpeechBubbleControl),
    new PropertyMetadata(
        BubbleTailPosition.BottomCenter,
        OnTailPositionChanged));

        public BubbleTailPosition TailPosition
        {
            get =>
                (BubbleTailPosition)GetValue(
                    TailPositionProperty);

            set =>
                SetValue(
                    TailPositionProperty,
                    value);
        }
        private static void OnTailPositionChanged(
    DependencyObject d,
    DependencyPropertyChangedEventArgs e)
        {
            var control =
                (SpeechBubbleControl)d;

            control.UpdateTail();
        }

        private void UpdateTail()
        {
            if (Tail == null)
                return;

            Tail.RenderTransformOrigin =
                new Point(0.5, 0.5);

            switch (TailPosition)
            {
                case BubbleTailPosition.BottomLeft:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Left;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Bottom;

                    Tail.Margin =
                        new Thickness(20, 0, 0, 0);

                    Tail.RenderTransform =
                        Transform.Identity;

                    break;


                case BubbleTailPosition.BottomCenter:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Center;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Bottom;

                    Tail.Margin =
                        new Thickness(0);

                    Tail.RenderTransform =
                        Transform.Identity;

                    break;


                case BubbleTailPosition.BottomRight:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Right;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Bottom;

                    Tail.Margin =
                        new Thickness(0, 0, 20, 0);

                    Tail.RenderTransform =
                        Transform.Identity;

                    break;


                case BubbleTailPosition.TopLeft:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Left;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Top;

                    Tail.Margin =
                        new Thickness(20, 0, 0, 0);

                    Tail.RenderTransform =
                        new RotateTransform(180);

                    break;


                case BubbleTailPosition.TopCenter:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Center;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Top;

                    Tail.Margin =
                        new Thickness(0);

                    Tail.RenderTransform =
                        new RotateTransform(180);

                    break;


                case BubbleTailPosition.TopRight:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Right;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Top;

                    Tail.Margin =
                        new Thickness(0, 0, 20, 0);

                    Tail.RenderTransform =
                        new RotateTransform(180);

                    break;


                case BubbleTailPosition.LeftCenter:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Left;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Center;

                    Tail.Margin =
                        new Thickness(0);

                    Tail.RenderTransform =
                        new RotateTransform(90);

                    break;


                case BubbleTailPosition.RightCenter:

                    Tail.HorizontalAlignment =
                        HorizontalAlignment.Right;

                    Tail.VerticalAlignment =
                        VerticalAlignment.Center;

                    Tail.Margin =
                        new Thickness(0);

                    Tail.RenderTransform =
                        new RotateTransform(-90);

                    break;
            }
        } 
        #endregion

        // =====================================================
        // Text
        // =====================================================

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(SpeechBubbleControl),
                new PropertyMetadata("TEXT"));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        // =====================================================
        // Border Color
        // =====================================================

        public static readonly DependencyProperty
            BubbleBorderBrushProperty =
                DependencyProperty.Register(
                    nameof(BubbleBorderBrush),
                    typeof(Brush),
                    typeof(SpeechBubbleControl),
                    new PropertyMetadata(
                        Brushes.DeepSkyBlue));

        public Brush BubbleBorderBrush
        {
            get =>
                (Brush)GetValue(
                    BubbleBorderBrushProperty);

            set =>
                SetValue(
                    BubbleBorderBrushProperty,
                    value);
        }

        // =====================================================
        // Text Color
        // =====================================================

        public static readonly DependencyProperty
            TextBrushProperty =
                DependencyProperty.Register(
                    nameof(TextBrush),
                    typeof(Brush),
                    typeof(SpeechBubbleControl),
                    new PropertyMetadata(
                        Brushes.White));

        public Brush TextBrush
        {
            get =>
                (Brush)GetValue(
                    TextBrushProperty);

            set =>
                SetValue(
                    TextBrushProperty,
                    value);
        }

        // =====================================================
        // Text Size
        // =====================================================

        public static readonly DependencyProperty
            TextSizeProperty =
                DependencyProperty.Register(
                    nameof(TextSize),
                    typeof(double),
                    typeof(SpeechBubbleControl),
                    new PropertyMetadata(12.0));

        public double TextSize
        {
            get =>
                (double)GetValue(
                    TextSizeProperty);

            set =>
                SetValue(
                    TextSizeProperty,
                    value);
        }

    }
}
