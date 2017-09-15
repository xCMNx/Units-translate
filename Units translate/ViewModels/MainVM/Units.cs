using Core;
using System.Linq;
using Ui;
using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Units_translate
{
    public partial class MainVM
    {
        public ObservableCollectionEx<IMapUnitEntry> UnitsList { get; } = new ObservableCollectionEx<IMapUnitEntry>();
        public List<IMapUnitEntry> MainUnitsList { get; } = new List<IMapUnitEntry>();
        string _yUml = string.Empty;
        public string yUml
        {
            get => _yUml;
            protected set
            {
                _yUml = value;
                NotifyPropertyChanged(nameof(yUml));
            }
        }

        public int UnitsShowedCount => UnitsList.Count;

        string _searchText = string.Empty;

        public string UnitSearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                updateSearchList();
                NotifyPropertyChanged(nameof(UnitSearchText));
            }
        }

        public ObservableCollectionEx<IMapUnitEntry> Entries = new ObservableCollectionEx<IMapUnitEntry>();

        public class RendererWrapper: Core.BindableBase
        {
            public object Result { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public ObservableCollectionEx<RendererWrapper> Tabs => new ObservableCollectionEx<RendererWrapper>();

        string _CustomValue = string.Empty;
        public string CustomValue
        {
            get => _CustomValue;
            protected set
            {
                _CustomValue = value;
                NotifyPropertyChanged(nameof(CustomValue));
            }
        }

        public Command UpdateUnitsListCommand { get; protected set; }
        public Command UpdateDiagramCommand { get; protected set; }
        ICommand _UnitsSortCommand;
        public ICommand UnitsSortCommand => _UnitsSortCommand;

        bool unitsOrder = false; 

        protected void UnitsCommands()
        {
            _UnitsSortCommand = new Command((prp) =>
            {
                var lst = UnitsList.OfType<UnitEntryWrapper>().ToArray();
                switch ((string)prp)
                {
                    case "Check":
                        lst = lst.OrderBy(e => e.Checked).ToArray();
                        break;
                    case "Value":
                        lst = lst.OrderBy(e => e.Value).ToArray();
                        break;
                    case "DependsCount":
                        lst = lst.OrderBy(e => e.DependsCount).ToArray();
                        break;
                }
                UnitsList.Reset(unitsOrder ? lst : lst.Reverse());
                NotifyPropertyChanged(nameof(UnitsShowedCount));
                unitsOrder = !unitsOrder;
            });
            UpdateUnitsListCommand = new Command(param => UpdateUnitsList());
            UpdateDiagramCommand = new Command(param => UpdateDiagram());
        }

        class UnitEntryWrapper: Core.BindableBase, IMapUnitEntry
        {
            public IMapUnitEntry Source { get; protected set; }
            bool _Checked = false;
            public bool Checked
            {
                get => _Checked;
                set
                {
                    _Checked = value;
                    NotifyPropertyChanged(nameof(Checked));
                }
            }

            public string Path { get => Source.Path; set => Source.Path = value; }

            public string Value => Source.Value;

            public int Start => Source.Start;

            public int End => Source.End;

            public int DependsCount => Source.Links.Count;

            public ICollection<IMapUnitLink> Links => Source.Links;

            IMapData IMapUnitEntry.Data { get => Source.Data; set => Source.Data = value; }

            public bool CaseSensitive => Source.CaseSensitive;

            public UnitEntryWrapper(IMapUnitEntry data)
            {
                Source = data;
            }

            public bool IsSameValue(object val) => Source.IsSameValue(val);

            public string ToUnitString() => Source.ToUnitString();
        }

        void updateSearchList()
        {
            UnitsList.Clear();
            try
            {
                var rgxp = new Regex(_searchText, RegexOptions.IgnoreCase, new TimeSpan(0, 0, 1));
                UnitsList.AddRange(MainUnitsList.Where(i => rgxp.IsMatch(i.Value)).ToList());
            }
            catch
            {

            }
            NotifyPropertyChanged(nameof(UnitsShowedCount));
        }

        public IEnumerable<IMapUnitEntry> GetUnitsEntries() => MappedData.Data.Where(data => data.Items != null).SelectMany(data => data.Items.OfType<IMapUnitEntry>());
        IList<IMapUnitEntry> getUnitsEntries() => GetUnitsEntries().ToArray();

        public void ShowDependsFor(IMapUnitEntry unit)
        {
            UnitsList.Clear();
            NotifyPropertyChanged(nameof(UnitsShowedCount));
            if (unit == null)
                return;
            var units = GetUnitsEntries().Where(u => u.Links.Any(l => unit.Value == l.Value)).ToArray();
            UnitsList.AddRange(units);
            NotifyPropertyChanged(nameof(UnitsShowedCount));
        }

        void UpdateUnitsList()
        {
            MainUnitsList.Clear();
            try
            {
                var lst = getUnitsEntries().Select(data=> new UnitEntryWrapper(data)).OfType<UnitEntryWrapper>().OrderBy(u => u.Value).ToArray();
                MainUnitsList.AddRange(lst);
                updateSearchList();
            }
            catch
            {

            }
        }

        void MakeCustomValue()
        {
            var sb = new StringBuilder();
            foreach (var entry in Entries)
                sb.AppendLine(entry.ToUnitString());
            CustomValue = sb.ToString();
        }

        public async void UpdateDiagram()
        {
            try
            {
                var lst = getUnitsEntries();
                var unitsList = new HashSet<IMapUnitEntry>();
                var sb = new StringBuilder();
                Func<IMapUnitEntry, ICollection<IMapUnitEntry>> getDepends = null;
                getDepends = (unit) =>
                {
                    var name = unit.CaseSensitive ? unit.Value : unit.Value.ToLower();
                    sb.AppendLine($"[{name}|{unit.Value}|{unit.Path}]");
                    unitsList.Add(unit);
                    var res = new HashSet<IMapUnitEntry>();
                    foreach (var lnk in unit.Links)
                    {
                        var cs = unit.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
                        sb.AppendLine($"[{name}]->[{(unit.CaseSensitive ? lnk.Value : lnk.Value.ToLower())}]");
                        foreach (var entry in lst)
                            if (string.Equals(entry.Value, lnk.Value, unit.CaseSensitive == entry.CaseSensitive ? cs : StringComparison.InvariantCulture))
                            {
                                res.Add(entry);
                                if (!unitsList.Contains(entry))
                                {
                                    unitsList.Add(entry);
                                    foreach (var u in getDepends(entry))
                                        res.Add(u);
                                }
                            }
                    }
                    return res;
                };
                var units = MainUnitsList.OfType<UnitEntryWrapper>().Where(unit => unit.Checked).Select(unit => unit.Source).ToArray();
                foreach (var unit in units)
                    foreach (var u in getDepends(unit))
                        unitsList.Add(u);
                yUml = sb.ToString();
                Entries.Clear();
                Entries.AddRange(unitsList);
                MakeCustomValue();
                //foreach (var r in Core.yUml.yUmlRenderers)
                //{
                //    object res = null;
                //    try
                //    {
                //        res = await r.RenderYumlGraphAync(yUml);
                //    }
                //    catch (Exception e)
                //    {
                //        res = e.Message;
                //    }
                //    Tabs.Add(new RendererWrapper() { Result = res, Name = r.Name });
                //}
            }
            catch(Exception e)
            {
                Helpers.ConsoleWrite(e.Message);
            }
        }

    }
}
