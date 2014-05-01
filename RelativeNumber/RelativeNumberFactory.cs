namespace RelativeNumber
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Outlining;

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(RelativeNumber.MarginName)]
    [Order(Before = PredefinedMarginNames.LeftSelection)]
    [MarginContainer(PredefinedMarginNames.Left)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class MarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal IEditorFormatMapService FormatMapService = null;

        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            return new RelativeNumber(
                textViewHost.TextView, 
                FormatMapService.GetEditorFormatMap(textViewHost.TextView), 
                containerMargin,
                OutliningManagerService.GetOutliningManager(textViewHost.TextView));
        }
    }
}
