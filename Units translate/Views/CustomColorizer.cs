using System;
using System.Windows;
using System.Windows.Media;
using Core;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Units_translate.Views
{
    public class CustomColorizer : DocumentColorizingTransformer
    {
        static TextDecorationCollection decorStrikethrough = new TextDecorationCollection() { TextDecorations.Strikethrough };
        static TextDecorationCollection decorsUnderline = new TextDecorationCollection() { TextDecorations.Underline };
        public IMapData Data = null;

        static Action<VisualLineElement> ValueTypeToAction(IMapItemRange item)
        {
            if (item is IMapValueItem)
                if (item is IMapBackgroundColorRange)
                    return (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetForegroundBrush(Brushes.Blue);
                        var itv = item as IMapValueItem;
                        if (itv != null && string.IsNullOrWhiteSpace((MappedData.GetValueRecord(itv.Value) as IMapValueRecord).Translation))
                            element.TextRunProperties.SetTextDecorations(decorStrikethrough);
                        element.TextRunProperties.SetBackgroundBrush((item as IMapBackgroundColorRange).BackgroundColor);
                    };
                else
                    return (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetForegroundBrush(Brushes.Blue);
                        var itv = item as IMapValueItem;
                        if (itv != null && string.IsNullOrWhiteSpace((MappedData.GetValueRecord(itv.Value) as IMapValueRecord).Translation))
                            element.TextRunProperties.SetTextDecorations(decorStrikethrough);
                    };
            else if (item is IMapForeColorRange)
                if (item is IMapBackgroundColorRange)
                    return (VisualLineElement element) =>
                    {
                        element.TextRunProperties.SetForegroundBrush((item as IMapForeColorRange).ForegroundColor);
                        element.BackgroundBrush = (item as IMapBackgroundColorRange).BackgroundColor;
                    };
                else
                    return (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush((item as IMapForeColorRange).ForegroundColor);
            else if (item is IMapBackgroundColorRange)
                return (VisualLineElement element) => element.TextRunProperties.SetBackgroundBrush((item as IMapBackgroundColorRange).BackgroundColor);
            else
                return (VisualLineElement element) => element.TextRunProperties.SetForegroundBrush((item as IMapForeColorRange).ForegroundColor);
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (Data == null || Data.Items == null)
                return;
            int start = line.Offset;
            int end = line.EndOffset;
            var items = Data.ItemsBetween(start, end);
            foreach (var item in items)
                ChangeLinePart(Math.Max(start, item.Start), Math.Min(item.End, end), ValueTypeToAction(item));
        }
    }
}