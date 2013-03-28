using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace JumpyImpl
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public sealed class Connector : IVsTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("JumpyAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Caret, Before = PredefinedAdornmentLayers.DifferenceChanges)]
        //[Order(Before = PredefinedAdornmentLayers.Caret)]
        [TextViewRole(PredefinedTextViewRoles.Editable)]
        public AdornmentLayerDefinition commentLayerDefinition;

        [Import(typeof(IVsEditorAdaptersFactoryService))]
        internal IVsEditorAdaptersFactoryService editorFactory = null;

        [Import(typeof(IOutliningManagerService))]
        internal IOutliningManagerService OutliningManagerService = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = editorFactory.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            var keyBindingCommandFilter = new KeyBindingCommandFilter(textView);
            AddCommandFilter(textViewAdapter, keyBindingCommandFilter);

            IOutliningManager outliningManager = this.OutliningManagerService.GetOutliningManager((ITextView)textView);

            JumpAdornmentManager.Create(textView, keyBindingCommandFilter, outliningManager);

        }

        void AddCommandFilter(IVsTextView viewAdapter, KeyBindingCommandFilter commandFilter)
        {
            if (commandFilter._added == false)
            {
                //get the view adapter from the editor factory
                IOleCommandTarget next;
                int hr = viewAdapter.AddCommandFilter(commandFilter, out next);

                if (hr == VSConstants.S_OK)
                {
                    commandFilter._added = true;
                    //you'll need the next target for Exec and QueryStatus 
                    if (next != null)
                        commandFilter._nextTarget = next;
                }
            }
        }

        public static void Execute(IWpfTextViewHost host)
        {
            IWpfTextView view = host.TextView;
            //JumpAdornmentProvider provider = view.Properties.GetProperty<JumpAdornmentProvider>(typeof (JumpAdornmentProvider));
            JumpAdornmentManager manager =
                view.Properties.GetProperty<JumpAdornmentManager>(typeof(JumpAdornmentManager));

            manager.Execute();

        }


    }
}