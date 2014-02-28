namespace RelativeNumber
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Export a <see cref="IWpfTextViewMarginProvider"/>, which returns an instance of the margin for the editor
    /// to use.
    /// </summary>
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

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            var isLineNumbersOn = (bool)textViewHost.TextView.Options.GetOptionValue("TextViewHost/LineNumberMargin");

            if (!isLineNumbersOn) return null;

            return new RelativeNumber(textViewHost.TextView, FormatMapService.GetEditorFormatMap(textViewHost.TextView), containerMargin);
        }
    }
}
