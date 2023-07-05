#define OLD_STYLE
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Core;
using Newtonsoft.Json;
using System;
using System.Windows;

namespace Json
{
    public class Entry : ITranslationItem
    {
        string _Eng;
        string _Trans;

        public string Value => _Eng;
        public string Translation => _Trans;

        public Entry(string eng, string trans)
        {
            _Eng = eng;
            _Trans = trans;
        }
        public Entry() { }
    }


    [ContainerFilter("json container", "json")]
    public class JsonContaier : ITranslationContainer
    {
        static JsonSerializer serializer = new JsonSerializer();

        public ICollection<ITranslationItem> Load(string filePath, Encoding encoding)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath, Encoding.UTF8));
                var data2 = data.Select(p => new Entry(p.Key, p.Value));
                return data2.ToArray();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка загрузки файла переводов", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ITranslationItem>();
            }
        }

        public bool Save(string filePath, Encoding encoding, ICollection<ITranslationItem> items)
        {
            var data = new Dictionary<string, string>();
            foreach(var p in items) data.Add(p.Value, p.Translation);
            string output = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, output, Encoding.UTF8);
            return true;
        }

        static JsonContaier()
        {
        }
    }
}
