namespace RelativeNumber
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// A class detailing the margin's visual definition including both size and content.
    /// </summary>
    internal class RelativeNumber : Canvas, IWpfTextViewMargin
    {
        internal const string MarginName = "RelativeNumber";
        private IWpfTextView textView;
        private IEditorFormatMap formatMap;
        private IWpfTextViewMargin containerMargin;
        private bool isDisposed;

        private int lastCursorLine = -1;

        public RelativeNumber(IWpfTextView textView, IEditorFormatMap formatMap, IWpfTextViewMargin containerMargin)
        {
            this.textView = textView;
            this.formatMap = formatMap;
            this.containerMargin = containerMargin;

            this.ClipToBounds = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            textView.Caret.PositionChanged += OnCaretPositionChanged;
            textView.Options.OptionChanged += OnOptionChanged;
            textView.LayoutChanged += OnLayoutChanged;
            textView.ViewportHeightChanged += (sender, args) => ApplyNumbers();
            formatMap.FormatMappingChanged += (sender, args) => ApplyNumbers();
            textView.ViewportWidthChanged += OnViewportWidthChanged;

            HideVSLineNumbers();
        }

        private void OnViewportWidthChanged(object sender, EventArgs e)
        {
            HideVSLineNumbers();
            ApplyNumbers();
        }

        private void OnOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (e.OptionId == DefaultTextViewHostOptions.LineNumberMarginName)
            {
                HideVSLineNumbers();
                ApplyNumbers();
            }
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            var currentCursorLine = CursorLineIndex;

            if (lastCursorLine != currentCursorLine)
            {
                lastCursorLine = currentCursorLine;
                ApplyNumbers();
            }
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (e.VerticalTranslation || e.NewOrReformattedLines.Count > 0)
            {
                ApplyNumbers();
            }
        }

        private int CursorLineIndex
        {
            get
            {
                return textView.TextSnapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition.Position);
            }
        }

        private void ApplyNumbers()
        {
            // Toggle visibility
            var isLineNumberOn = (bool)textView.Options.GetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName);
            this.Visibility = isLineNumberOn ? Visibility.Visible : Visibility.Hidden;

            // Bail when line numbers are off
            if (!isLineNumberOn) return;

            Children.Clear();

            // Get the visual styles
            var lineNumberColorScheme = formatMap.GetProperties("Line Number");
            var backColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.BackgroundBrushId];
            var foreColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.ForegroundBrushId];
            var fontFamily = textView.FormattedLineSource.DefaultTextProperties.Typeface.FontFamily;
            var fontSize = textView.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize * (textView.ZoomLevel / 100);

            // Setup line indexes
            var currentCursorLineIndex = CursorLineIndex;
            var viewTotalLines = textView.TextViewLines.Count;
            var totalLineCount = textView.TextSnapshot.LineCount;
            var numberCharactersLineCount = (totalLineCount == 0) ? 1 : (int)Math.Log10(totalLineCount) + 1 + 1;

            var formattedWidth = CalculateWidth(FormatNumber(numberCharactersLineCount, totalLineCount), fontFamily, fontSize);
            this.Width = isLineNumberOn ? formattedWidth : 0.0;
            this.Background = backColor;

            var previousLineNumberIndex = -1;
            for (var i = 0; i < viewTotalLines; i++)
            {
                var width = numberCharactersLineCount;
                var currentLineNumberIndex = textView.TextSnapshot.GetLineNumberFromPosition(textView.TextViewLines[i].Start);

                int? displayNumber;
                if (previousLineNumberIndex == currentLineNumberIndex)
                {
                    displayNumber = null;
                }
                else if (currentLineNumberIndex == currentCursorLineIndex)
                {
                    displayNumber = currentCursorLineIndex + 1;
                    width = numberCharactersLineCount * -1;
                }
                else
                    displayNumber = Math.Abs(currentLineNumberIndex - currentCursorLineIndex);

                var lineNumber = ConstructLineNumber(displayNumber, width, fontFamily, fontSize, foreColor);
                previousLineNumberIndex = currentLineNumberIndex;

                var top = (textView.TextViewLines[i].TextTop - textView.ViewportTop) * (textView.ZoomLevel / 100);
                SetTop(lineNumber, top);
                Children.Add(lineNumber);
            }
        }

        private void HideVSLineNumbers()
        {
            IWpfTextViewMargin lineNumberMargin = containerMargin.GetTextViewMargin(PredefinedMarginNames.LineNumber) as IWpfTextViewMargin;
            if (lineNumberMargin == null) return;
            lineNumberMargin.VisualElement.Visibility = System.Windows.Visibility.Hidden;
            lineNumberMargin.VisualElement.Width = 0.0;
            lineNumberMargin.VisualElement.MinWidth = 0.0;
            lineNumberMargin.VisualElement.MaxWidth = 0.0;
            lineNumberMargin.VisualElement.UpdateLayout();
        }

        private static string FormatNumber(int width, int? lineNumber)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0," + width + "}", lineNumber);
        }

        private static Label ConstructLineNumber(int? displayNumber, int width, FontFamily fontFamily, double fontSize, Brush foreColor)
        {
            var label = new Label
            {
                FontFamily = fontFamily,
                FontSize = fontSize,
                Foreground = foreColor,
                Content = FormatNumber(width, displayNumber),
                Padding = new Thickness(0, 0, 0, 0)
            };
            return label;
        }

        private double CalculateWidth(string displayNumber, FontFamily fontFamily, double fontSize)
        {
            var formattedText = new FormattedText(
                displayNumber,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily.Source),
                fontSize,
                Brushes.Black);

            return formattedText.Width;
        }

        public FrameworkElement VisualElement
        {
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return this.MarginSize;
            }
        }

        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == MarginName) ? this : null;
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(MarginName);
        }
    }
}