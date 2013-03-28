using System;

namespace JumpyImpl.EventArguments
{
    public class IndicateKeyPressedEventArgs : EventArgs
    {
        public Char KeyValue { get; set; }
    }
}