using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace RelativeNumber
{
    [Export(typeof(EditorFormatDefinition))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Name(CurrentLineFormatDefinition.Name)]
    [UserVisible(true)]
    internal class CurrentLineFormatDefinition : ClassificationFormatDefinition
    {
        public const string Name = "RelativeNumber/CurrentLineNumber";

        public CurrentLineFormatDefinition()
        {
            this.DisplayName = "Relative Number - Current Line";
            this.ForegroundColor = null;
            this.BackgroundColor = null;
            this.IsBold = null;
        }
    }
}