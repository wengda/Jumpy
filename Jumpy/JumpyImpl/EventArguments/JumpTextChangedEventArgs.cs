using System;

namespace JumpyImpl.EventArguments
{
    internal class JumpTextChangedEventArgs : EventArgs
    {
        public char SearchingChar { get; private set; }
        public JumpTextChangedEventArgs(char searchingChar)
        {
            this.SearchingChar = searchingChar;
        }
    }
}