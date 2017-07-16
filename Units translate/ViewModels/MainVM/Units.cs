using Core;
using System.Linq;
using Ui;
using System;
using System.Text;
using System.Collections.Generic;

namespace Units_translate
{
    public partial class MainVM
    {
        public ObservableCollectionEx<IMapUnitEntry> UnitsList { get; } = new ObservableCollectionEx<IMapUnitEntry>();
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

        protected void UnitsCommands()
        {
            UpdateUnitsListCommand = new Command(param => UpdateUnitsList());
            UpdateDiagramCommand = new Command(param => UpdateDiagram());
        }

        class UnitEntryWrapper: Core.BindableBase, IMapUnitEntry
        {
            public IMapUnitEntry Data { get; protected set; }
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

            public string Path { get => Data.Path; set => Data.Path = value; }

            public string Value => Data.Value;

            public int Start => Data.Start;

            public int End => Data.End;

            public ICollection<IMapUnitLink> Links => Data.Links;

            IMapData IMapUnitEntry.Data { get => Data.Data; set => Data.Data = value; }

            public bool CaseSensitive => Data.CaseSensitive;

            public UnitEntryWrapper(IMapUnitEntry data)
            {
                Data = data;
            }

            public bool IsSameValue(object val) => Data.IsSameValue(val);

            public string ToUnitString() => Data.ToUnitString();
        }

        void UpdateUnitsList()
        {
            UnitsList.Clear();
            try
            {
                var lst = MappedData.Data.SelectMany(data => data.Items == null ? new IMapUnitEntry[0] : data.Items.OfType<IMapUnitEntry>()).Select(data=> new UnitEntryWrapper(data)).ToArray();
                UnitsList.AddRange(lst);
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
                var lst = MappedData.Data.SelectMany(data => data.Items == null ? new IMapUnitEntry[0] : data.Items.OfType<IMapUnitEntry>()).ToArray();
                var entryes = new HashSet<string>(StringComparer.InvariantCulture);
                var sb = new StringBuilder();
                Func<IMapUnitEntry, ICollection<IMapUnitEntry>> getDepends = null;
                getDepends = (unit) =>
                {
                    var name = unit.CaseSensitive ? unit.Value : unit.Value.ToLower();
                    sb.AppendLine($"[{name}|{unit.Value}|{unit.Path}]");
                    var res = new HashSet<IMapUnitEntry>();
                    foreach (var lnk in unit.Links)
                    {
                        var lName = unit.CaseSensitive ? lnk.Value : lnk.Value.ToLower();
                        sb.AppendLine($"[{name}]->[{lName}]");
                        foreach (var entry in lst)
                            if (string.Equals(entry.Value, lName, entry.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
                            {
                                res.Add(entry);
                                if (!entryes.Contains(lName))
                                {
                                    entryes.Add(lName);
                                    foreach (var u in getDepends(entry))
                                        res.Add(u);
                                }
                            }
                    }
                    return res;
                };
                var units = UnitsList.Where(unit => (unit as UnitEntryWrapper).Checked).ToArray();
                var unitsList = new HashSet<IMapUnitEntry>();
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
