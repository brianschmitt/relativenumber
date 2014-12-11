namespace RelativeNumber
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Outlining;

    /// <summary>
    /// A class detailing the margin's visual definition including both size and content.
    /// </summary>
    internal class RelativeNumber : Canvas, IWpfTextViewMargin
    {
        internal const string MarginName = "RelativeNumber";
        private IWpfTextView textView;
        private IEditorFormatMap formatMap;
        private IWpfTextViewMargin containerMargin;
        private IOutliningManager outliningManager;
        private bool isDisposed;
        private bool HasFocus = false;

        private int lastCursorLine = -1;

        public RelativeNumber(IWpfTextView textView, IEditorFormatMap formatMap, IWpfTextViewMargin containerMargin, IOutliningManager outliningManager)
        {
            this.textView = textView;
            this.formatMap = formatMap;
            this.containerMargin = containerMargin;
            this.outliningManager = outliningManager;

            this.ClipToBounds = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);

            textView.Caret.PositionChanged += OnCaretPositionChanged;
            textView.Options.OptionChanged += OnOptionChanged;
            textView.LayoutChanged += OnLayoutChanged;
            textView.ViewportHeightChanged += (sender, args) => ApplyNumbers();
            formatMap.FormatMappingChanged += (sender, args) => ApplyNumbers();
            textView.ViewportWidthChanged += OnViewportWidthChanged;
            textView.GotAggregateFocus += GotFocusHandler;
            textView.LostAggregateFocus += LostFocusHandler;
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
            var currentCursorLine = CursorLineNumber;

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

        private void GotFocusHandler(object sender, EventArgs e)
        {
            HasFocus = true;
            ApplyNumbers();
        }

        private void LostFocusHandler(object sender, EventArgs e)
        {
            HasFocus = false;
            ApplyNumbers();
        }

        private int CursorLineNumber
        {
            get
            {
                return textView.Caret.ContainingTextViewLine.Start.GetContainingLine().LineNumber + 1;
            }
        }

        private void ApplyNumbers()
        {
            // Toggle visibility
            var isLineNumberOn = (bool)textView.Options.GetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName);
            this.Visibility = isLineNumberOn ? Visibility.Visible : Visibility.Hidden;

            // Get the visual styles
            var lineNumberColorScheme = formatMap.GetProperties("Line Number");
            var backColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.BackgroundBrushId];
            var foreColor = (SolidColorBrush)lineNumberColorScheme[EditorFormatDefinition.ForegroundBrushId];
            var fontFamily = textView.FormattedLineSource.DefaultTextProperties.Typeface.FontFamily;
            var fontSize = textView.FormattedLineSource.DefaultTextProperties.FontRenderingEmSize * (textView.ZoomLevel / 100);

            // Setup line indexes
            var currentCursorLineNumber = CursorLineNumber;
            var viewTotalLines = textView.TextViewLines.Count;
            var totalLineCount = textView.TextSnapshot.LineCount;
            var numberCharactersLineCount = (totalLineCount == 0) ? 1 : (int)Math.Log10(totalLineCount) + 1 + 1;

            var formattedWidth = CalculateWidth(FormatNumber(numberCharactersLineCount, totalLineCount), fontFamily, fontSize);
            this.Width = isLineNumberOn ? formattedWidth : 0.0;
            this.Background = backColor;

            // Bail when line numbers are off
            if (!isLineNumberOn) return;

            Children.Clear();

            var lineNumbers = BuildLineNumbers(currentCursorLineNumber, textView.VisualSnapshot.LineCount);
            var viewPortFirstLine = textView.TextSnapshot.GetLineNumberFromPosition(textView.TextViewLines.FirstVisibleLine.Start);
            var viewPortLastLine = textView.TextSnapshot.GetLineNumberFromPosition(textView.TextViewLines.LastVisibleLine.End);
            var cursorAbove = currentCursorLineNumber < viewPortFirstLine;
            var cursorBelow = currentCursorLineNumber > viewPortLastLine;

            int offset;
            if (cursorAbove)
            {
                var hiddenLines = CountHiddenLines(textView.Caret.Position.BufferPosition, textView.TextViewLines.FirstVisibleLine.Start);
                offset = viewPortFirstLine - hiddenLines - 1;
            }
            else if (cursorBelow)
            {
                var hiddenLines = CountHiddenLines(textView.TextViewLines.FirstVisibleLine.Start, textView.Caret.Position.BufferPosition);
                offset = viewPortFirstLine + hiddenLines - 1;
            }
            else
            {
                viewPortFirstLine = viewPortFirstLine == 0 ? 1 : viewPortFirstLine;
                var cursorViewPortLineIndex = currentCursorLineNumber - viewPortFirstLine;
                var hiddenLines = CountHiddenLines(textView.TextViewLines.FirstVisibleLine.Start, textView.Caret.ContainingTextViewLine.Start);
                offset = currentCursorLineNumber - cursorViewPortLineIndex + hiddenLines - 1;
            }

            offset = offset < 0 ? 0 : offset;

            var previousLineNumber = -1;
            var counter = 0;
            for (var i = 0; i < viewTotalLines; i++)
            {
                var width = numberCharactersLineCount;
                var currentLoopLine = textView.TextSnapshot.GetLineFromPosition(textView.TextViewLines[i].Start);
                var currentLoopLineNumber = currentLoopLine.LineNumber;

                int? displayNumber;
                if (previousLineNumber == currentLoopLineNumber)
                {
                    // line wrapped
                    displayNumber = null;
                }
                else if (currentLoopLineNumber + 1 == currentCursorLineNumber || !HasFocus)
                {
                    var indx = offset + counter;
                    displayNumber = lineNumbers.Count >= indx ? lineNumbers[indx] : lineNumbers[lineNumbers.Count];
                    width = HasFocus ? numberCharactersLineCount * -1 : numberCharactersLineCount;
                    counter += 1;
                }
                else
                {
                    // cursor line - display real line number
                    var indx = offset + counter;
                    displayNumber = lineNumbers[indx];
                    counter += 1;
                }

                var lineNumber = ConstructLineNumber(displayNumber, width, fontFamily, fontSize, foreColor);
                previousLineNumber = currentLoopLineNumber;

                var top = (textView.TextViewLines[i].TextTop - textView.ViewportTop) * (textView.ZoomLevel / 100);
                SetTop(lineNumber, top);
                Children.Add(lineNumber);
            }
        }

        private IList<int> BuildLineNumbers(int currentLineNumber, int maxLineNumber)
        {
            var list = new List<int>();
            if (HasFocus)
            {
                var beforCursor = Enumerable.Range(1, currentLineNumber - 1).Reverse();
                var afterCursor = Enumerable.Range(1, maxLineNumber);

                list.AddRange(beforCursor);
                list.Add(currentLineNumber);
                list.AddRange(afterCursor);
            }
            else
            {
                list.AddRange(Enumerable.Range(1, maxLineNumber));
            }

            return list;
        }

        private int CountHiddenLines(int start, int end)
        {
            if (start > end) return 0;
            
            // outliningManager will be null when outlining is not supported, such as in a git diff view
            if (outliningManager == null) return 0;

            var hiddenLines = outliningManager.GetCollapsedRegions(new SnapshotSpan(textView.TextSnapshot, start, end - start), true);
            var hiddenLineCount = 0;
            foreach (var hiddenLine in hiddenLines)
            {
                var span = hiddenLine.Extent.GetSpan(textView.TextBuffer.CurrentSnapshot);
                var strtLine = textView.TextSnapshot.GetLineNumberFromPosition(span.Start);
                var endLine = textView.TextSnapshot.GetLineNumberFromPosition(span.End);
                hiddenLineCount = hiddenLineCount + (endLine - strtLine);
            }
            return hiddenLineCount;
        }

        private void HideVSLineNumbers()
        {
            var lineNumberMargin = containerMargin.GetTextViewMargin(PredefinedMarginNames.LineNumber) as IWpfTextViewMargin;
            if (lineNumberMargin == null) return;
            lineNumberMargin.VisualElement.Visibility = Visibility.Hidden;
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