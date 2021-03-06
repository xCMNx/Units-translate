﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Core;

namespace Units_translate.Converters
{
    public class EntryColorConverter : IValueConverter
    {

        static Brush NotBindedTrans = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        static Brush NotBindedVal = new SolidColorBrush(Color.FromArgb(100, 255, 125, 0));
        static Brush UnbindedVal = new SolidColorBrush(Color.FromArgb(0, 255, 100, 0));
        static Brush NoTrans = new SolidColorBrush(Color.FromArgb(100, 30, 0, 255));
        static Brush Added = new SolidColorBrush(Color.FromArgb(100, 149, 255, 0));
        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            var item = o as ITranslationItem;
            if (string.IsNullOrWhiteSpace(item.Translation))
                return NoTrans;

            var rec = MappedData.GetValueRecord(item.Value) as IMapValueRecord;
            if (rec.Data.Count == 0)
                if (rec.WasLinked)
                    return UnbindedVal;
                else if (Translations.TranslatesDictionary.IndexOf(rec) >= 0)
                    return NotBindedTrans;
                else
                    return NotBindedVal;
            else if (Translations.TranslatesDictionary.IndexOf(rec) < 0)
                return Brushes.SkyBlue;

            return null;
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class RecordColorConverter : IValueConverter
    {

        static Brush NotBindedTrans = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        static Brush NotBindedVal = new SolidColorBrush(Color.FromArgb(100, 255, 125, 0));
        static Brush NoTrans = new SolidColorBrush(Color.FromArgb(100, 30, 0, 255));
        static Brush Added = new SolidColorBrush(Color.FromArgb(100, 149, 255, 0));
        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            var rec = o as IMapValueRecord;
            if (string.IsNullOrWhiteSpace(rec.Translation))
                return NoTrans;

            if (rec.Data.Count == 0)
                if (Translations.TranslatesDictionary.IndexOf(rec) >= 0)
                    return NotBindedTrans;
                else
                    return NotBindedVal;
            else if (Translations.TranslatesDictionary.IndexOf(rec) < 0)
                return Brushes.SkyBlue;

            return null;
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
