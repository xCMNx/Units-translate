﻿using System;
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

        static Action<VisualLineElement> ValueTypeToAction(IMapRangeItem item)
        {
            if (item is IMapMethodItem)
                return (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.BlueViolet);
                };
            if (item is IMapUnitEntry)
                return (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.OrangeRed);
                };
            if (item is IMapUnitLink)
                return (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.Orange);
                };
            if (item is IMapUnitPath)
                return (VisualLineElement element) =>
                {
                    element.TextRunProperties.SetForegroundBrush(Brushes.Red);
                };
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
            var items = Data.ItemsBetween<IMapRangeItem>(start, end);
            foreach (var item in items)
            {
                var cur = item;
                while(cur != null)
                {
                    ChangeLinePart(Math.Max(start, cur.Start), Math.Min(cur.End, end), ValueTypeToAction(cur));
                    cur = (cur as IMapSubrange)?.Subrange;
                }
            }
        }
    }
}
