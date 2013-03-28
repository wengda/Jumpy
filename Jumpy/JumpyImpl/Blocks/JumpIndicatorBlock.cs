using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;

namespace JumpyImpl.Blocks
{
    internal class JumpIndicatorBlock : Canvas
    {
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private TextBlock _textBlock;
        private SnapshotPoint _point;
        private char _char;

        public SnapshotPoint Point
        {
            get { return _point; }
        }

        public char DisplayChar
        {
            get { return _char; }
        }

        public JumpIndicatorBlock(double left, double top, double width, double height, char displayChar, SnapshotPoint point)
        {
            _left = left;
            _top = top;
            _width = width;
            _height = height;
            _char = displayChar;
            _point = point;

            _textBlock = new TextBlock
            {
                Width = width,
                Height = height,
                Background = new SolidColorBrush(Colors.LightGray),
                Foreground = Brushes.Black,
                FontWeight = FontWeights.ExtraBlack,
                FontSize = 9.5,
                Text = displayChar.ToString()
            };

            SetLeft(_textBlock, _left);
            SetTop(_textBlock, _top);
            Children.Add(_textBlock);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            Rect rect = new Rect(_left, _top, _width, _height);
            dc.DrawRectangle(new SolidColorBrush(Colors.Aqua), new Pen(Brushes.Tomato, 2), rect);
        }
    }
}