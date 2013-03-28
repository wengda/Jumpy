using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JumpyImpl.EventArguments;

namespace JumpyImpl.Blocks
{
    internal class InputBlock : Canvas
    {
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private TextBox _tbInput;

        public event EventHandler<JumpTextChangedEventArgs> JumpTextChanged;

        public InputBlock(double left, double top, double width, double height)
        {
            _left = left;
            _top = top;
            _width = width;
            _height = height;

            _tbInput = new TextBox
            {
                Width = width,
                Height = height,
                MaxLength = 1,
                FontSize = 10,
                Background = Brushes.Chocolate,
                BorderBrush = Brushes.DarkGray
            };
            _tbInput.TextChanged += tbInput_TextChanged;
            _tbInput.KeyUp += TbInput_KeyUp;

            SetLeft(_tbInput, _left);
            SetTop(_tbInput, _top);
            Children.Add(_tbInput);
        }

        void TbInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (JumpTextChanged == null)
                return;

            if (e.Key == Key.Escape || e.Key == Key.Home || e.Key == Key.End)
            {
                JumpTextChanged(this, new JumpTextChangedEventArgs((char)e.Key));
                e.Handled = true;
            }
        }

        public void SetFocus()
        {
            _tbInput.Focus();
        }

        private void tbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (JumpTextChanged != null)
            {
                if (tb.Text.Trim().Length == 0)
                    tb.Text = string.Empty;
                else
                    JumpTextChanged(this, new JumpTextChangedEventArgs(tb.Text.Trim()[0]));
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            Rect rect = new Rect(_left, _top, _width, _height);
            dc.DrawRectangle(Brushes.Aqua, new Pen(Brushes.Tomato, 2), rect);
        }
    }
}