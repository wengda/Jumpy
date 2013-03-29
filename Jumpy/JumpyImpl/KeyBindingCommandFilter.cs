using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows; 
using JumpyImpl.EventArguments;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace JumpyImpl
{
    internal class KeyBindingCommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _textView;
        internal IOleCommandTarget _nextTarget;
        internal bool _added;
        internal bool _adorned;
        public bool Intercept { get; set; }
        public JumpAdornmentManager AdornmentManager { get; set; }

        public event EventHandler<IndicateKeyPressedEventArgs> KeyPressed;

        public KeyBindingCommandFilter(IWpfTextView textView)
        {
            _textView = textView;
            _adorned = false;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _nextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (Intercept)
            {
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
                    {
                        var typedCharValue = (ushort)Marshal.GetObjectForNativeVariant(pvaIn);
                        char typedChar = (char)typedCharValue;
                        if (AdornmentManager.ValidChars.Contains(char.ToUpperInvariant(typedChar))
                            || typedCharValue == 32 //space
                            )
                        {
                            if (KeyPressed != null)
                                KeyPressed(this, new IndicateKeyPressedEventArgs() { KeyValue = typedChar });
                        }

                        return 0;
                    }
                    if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
                    {
                        if (KeyPressed != null)
                            KeyPressed(this, new IndicateKeyPressedEventArgs() { KeyValue = 'a' });
                        return 0;
                    }
                    if (nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
                    {
                        if (KeyPressed != null)
                            KeyPressed(this, new IndicateKeyPressedEventArgs() { KeyValue = char.MinValue });
                        return 0;
                    }
                }
                return 0;
            }

            return _nextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
    }
}