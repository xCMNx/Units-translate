using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core;
using System.Text;
using System.Windows.Media;

namespace regex
{
    [MapperFilter(new[] { "*" })]
    public class RegexMapper : IConfigurableMapper
    {
        Dictionary<string, Regex> _expressionsBindings;
        Dictionary<string, Regex> _data;

        RegexSettings _settings = new RegexSettings();
        public object SettingsControl { get { return _settings; } }

        public string Name { get { return "RegEx"; } }

        static IMapItemRange GroupToType(string g, int start, int end, int valueStart, int valueEnd, string value)
        {
            switch (g)
            {
                case "G": return new MapItemForeColorRangeBase(start, end, Brushes.Gray);
                case "S": return new MapItemRangedValueBase(value, start, end, valueStart, valueEnd);
                case "C": return new MapItemForeColorRangeBase(start, end, Brushes.Green);
                case "D": return new MapItemForeColorRangeBase(start, end, Brushes.LightBlue);
            }
            return null;
        }

        IMapItemRange FindGroup(Match m, IEnumerable<string> names)
        {
            foreach (var gn in names)
            {
                Group g = m.Groups[gn];
                if (g.Success)
                    return GroupToType(gn, m.Index, m.Index + m.Length, g.Index, g.Index + g.Length, g.Value);
            }
            return null;
        }

        static string[] GNames = new string[] { "G", "S", "C", "D" };

        public IEnumerable<IMapItemRange> GetMap(string Text, string Ext)
        {
            var res = new List<IMapItemRange>();
            var regex = _expressionsBindings[Ext.ToUpper()];
            var regex_names = regex.GetGroupNames();
            var ms = regex.Matches(Text);

            var names = GNames.Intersect(regex_names);
            foreach (Match m in ms)
            {
                var itm = FindGroup(m, names);
                if (itm != null)
                    res.Add(itm);
            }
            return res;
        }

        public bool IsExtAcceptable(string Ext)
        {
            return _expressionsBindings.ContainsKey(Ext.ToUpper());
        }

        public void Reset()
        {
            _data = new Dictionary<string, Regex>() {
                { ".PAS;.DPR", new Regex(@"(?ixmsn) uses.+?; | '(?<G>\{[a-fA-F\d]{8}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{4}-[a-fA-F\d]{12}\})' | \{(?<D>\$.*?)[\}\r\n] | {(?<C>.*?)} | //(?<C>.*?)$ | /\*(?<C>.*?)\*/ | \(\*(?<C>.*?)\*\) | (?:(?<S>\#[#\d]+')|')(?<S>([^']+)|(?:''|'\#[#\d]+'))+'") }
            };
            FixBindings();
            _settings.Settings.Text = string.Join("\r\n", _data.Select(it => string.Format("{0} ={1}", it.Key, it.Value)).ToArray());
        }

        void FixBindings()
        {
            _expressionsBindings = new Dictionary<string, Regex>();
            foreach (var k in _data)
                foreach (var ext in k.Key.ToUpper().Split(';'))
                    _expressionsBindings.Add(ext, k.Value);
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public void TryFix(string FilePath, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public IDictionary<IMapItemRange, int> GetCustomRanges(string Text, string Ext) => null;

        public RegexMapper()
        {
            Reset();
        }
    }
}
