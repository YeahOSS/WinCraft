using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WinCraft.Infrastructure;

namespace WinCraft.UI
{
    internal static class TextInputClipboard
    {
        private static bool _isRegistered;

        public static void Register()
        {
            if (_isRegistered)
                return;

            Register(typeof(TextBox));
            Register(typeof(RichTextBox));
            _isRegistered = true;
        }

        private static void Register(Type controlType)
        {
            EventManager.RegisterClassHandler(
                controlType,
                CommandManager.PreviewCanExecuteEvent,
                new CanExecuteRoutedEventHandler(OnPreviewCanExecute));
            EventManager.RegisterClassHandler(
                controlType,
                CommandManager.PreviewExecutedEvent,
                new ExecutedRoutedEventHandler(OnPreviewExecuted));
        }

        private static void OnPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy)
            {
                e.CanExecute = CanCopy(sender);
                e.Handled = true;
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                e.CanExecute = CanCut(sender);
                e.Handled = true;
            }
        }

        private static void OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy)
            {
                TryCopySelection(sender);
                e.Handled = true;
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                if (TryCopySelection(sender))
                    DeleteSelection(sender);

                e.Handled = true;
            }
        }

        private static bool CanCopy(object target)
        {
            return target switch
            {
                TextBox textBox => textBox.IsEnabled && textBox.SelectionLength > 0,
                RichTextBox richTextBox => richTextBox.IsEnabled && !richTextBox.Selection.IsEmpty,
                _ => false,
            };
        }

        private static bool CanCut(object target)
        {
            return target switch
            {
                TextBox textBox => textBox.IsEnabled && !textBox.IsReadOnly && textBox.SelectionLength > 0,
                RichTextBox richTextBox => richTextBox.IsEnabled && !richTextBox.IsReadOnly && !richTextBox.Selection.IsEmpty,
                _ => false,
            };
        }

        private static bool TryCopySelection(object target)
        {
            return target switch
            {
                TextBox textBox => TryCopyTextSelection(textBox),
                RichTextBox richTextBox => TryCopyRichTextSelection(richTextBox),
                _ => false,
            };
        }

        private static void DeleteSelection(object target)
        {
            switch (target)
            {
                case TextBox textBox:
                    textBox.SelectedText = string.Empty;
                    break;
                case RichTextBox richTextBox:
                    richTextBox.Selection.Text = string.Empty;
                    break;
            }
        }

        private static bool TryCopyTextSelection(TextBox textBox)
        {
            return PresentationSource.FromVisual(textBox) is HwndSource source &&
                   ClipboardAccess.TrySetText(source.Handle, textBox.SelectedText);
        }

        private static bool TryCopyRichTextSelection(RichTextBox richTextBox)
        {
            var selection = richTextBox.Selection;
            var dataObject = CreateRichTextDataObject(richTextBox, selection);
            var copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, isDragDrop: false);
            richTextBox.RaiseEvent(copyingEventArgs);
            if (copyingEventArgs.CommandCancelled)
                return false;

            return ClipboardAccess.TrySetDataObject(dataObject);
        }

        private static DataObject CreateRichTextDataObject(RichTextBox richTextBox, TextRange selection)
        {
            var dataObject = new DataObject();
            var text = selection.Text;
            if (!string.IsNullOrEmpty(text))
            {
                if (CanSetData(richTextBox, dataObject, DataFormats.Text))
                    dataObject.SetData(DataFormats.Text, text, true);

                if (CanSetData(richTextBox, dataObject, DataFormats.UnicodeText))
                    dataObject.SetData(DataFormats.UnicodeText, text, true);
            }

            if (ContainsImage(selection) && CanSetData(richTextBox, dataObject, DataFormats.XamlPackage))
                dataObject.SetData(DataFormats.XamlPackage, SaveSelection(selection, DataFormats.XamlPackage));

            if (CanSetData(richTextBox, dataObject, DataFormats.Rtf))
            {
                var rtf = SaveSelectionAsString(selection, DataFormats.Rtf);
                if (!string.IsNullOrEmpty(rtf))
                    dataObject.SetData(DataFormats.Rtf, rtf, true);
            }

            if (GetSelectedImage(selection) is BitmapSource image)
                dataObject.SetImage(image);

            if (CanSetData(richTextBox, dataObject, DataFormats.Xaml))
            {
                var xaml = SaveSelectionAsString(selection, DataFormats.Xaml);
                if (!string.IsNullOrEmpty(xaml))
                    dataObject.SetData(DataFormats.Xaml, xaml, false);
            }

            return dataObject;
        }

        private static bool CanSetData(RichTextBox richTextBox, DataObject dataObject, string format)
        {
            var eventArgs = new DataObjectSettingDataEventArgs(dataObject, format);
            richTextBox.RaiseEvent(eventArgs);
            return !eventArgs.CommandCancelled;
        }

        private static MemoryStream SaveSelection(TextRange selection, string format)
        {
            var stream = new MemoryStream();
            selection.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        private static string SaveSelectionAsString(TextRange selection, string format)
        {
            using var stream = SaveSelection(selection, format);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static BitmapSource GetSelectedImage(TextRange selection)
        {
            var start = MovePastElementBoundaries(selection.Start, LogicalDirection.Forward);
            if (start == null || start.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.EmbeddedElement)
                return null;

            var end = MovePastElementBoundaries(selection.End, LogicalDirection.Backward);
            if (end == null
                || end.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.EmbeddedElement
                || start.GetOffsetToPosition(end) != 1)
            {
                return null;
            }

            return start.GetAdjacentElement(LogicalDirection.Forward) is Image { Source: BitmapSource image }
                ? image
                : null;
        }

        private static bool ContainsImage(TextRange selection)
        {
            for (var pointer = selection.Start;
                 pointer != null && pointer.CompareTo(selection.End) < 0;
                 pointer = pointer.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement &&
                    pointer.GetAdjacentElement(LogicalDirection.Forward) is Image)
                {
                    return true;
                }
            }

            return false;
        }

        private static TextPointer MovePastElementBoundaries(TextPointer pointer, LogicalDirection direction)
        {
            while (pointer != null)
            {
                var context = pointer.GetPointerContext(direction);
                if (context != TextPointerContext.ElementStart && context != TextPointerContext.ElementEnd)
                    break;

                pointer = pointer.GetNextContextPosition(direction);
            }

            return pointer;
        }
    }
}
