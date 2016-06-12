using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Core
{
    public static class Translations
    {
        internal static SortedObservableCollection<IMapRecord> _TranslatesDictionary = new SortedObservableCollection<IMapRecord>() { Comparer = MapRecordComparer.Comparer };
        /// <summary>
        /// Словарь переводов
        /// </summary>
        public static SortedObservableCollection<IMapRecord> TranslatesDictionary => _TranslatesDictionary;

        static KeyValuePair<Type, ContainerFilter>[] _List;
        /// <summary>
        /// Список парсеров
        /// </summary>
        public static KeyValuePair<Type, ContainerFilter>[] List => _List;

        public static string Filter => string.Join("|", Core.Translations.List.Select(tc => $"{tc.Value.Name}|{string.Join(";", tc.Value.Extensions)}"));

        /// <summary>
        /// Очищает переводы
        /// </summary>
        public static void Clear()
        {
            _TranslatesDictionary.Clear();
            SourcesCount = 0;
            OriginalData.Clear();
            foreach (IMapValueRecord it in MappedData._ValuesDictionary)
                it.Translation = string.Empty;
        }

        static HashSet<ITranslationItem> OriginalData = new HashSet<ITranslationItem>(TranslationItemEqualityComparer.EqualityComparer);
        static int SourcesCount = 0;
        public static Encoding encoding = Encoding.GetEncoding(Helpers.Default_Encoding);

        public static bool IsValueOriginal(string value) => OriginalData.Contains(new BaseTranslationItem() { Value = value });

        public static bool IsTranslatesChanged()
        {
            if (SourcesCount > 1)
                return true;
            var newEntr = GetEntries().ToDictionary(e => e.Value);
            return OriginalData.Any(e =>
            {
                IMapValueRecord tr;
                return !newEntr.TryGetValue(e.Value, out tr) || tr.Translation != e.Translation;
            });
        }


        /// <summary>
        /// Загружает новые данные переводов
        /// </summary>
        /// <param name="path">Путь к файлу переводов</param>
        /// <param name="containerType">Тип контейнера</param>
        /// <param name="onTranslationConflict">
        /// Вызывается при конфликтах перевода.
        /// Получает текущую запись и конликтующий перевод.
        /// </param>
        public static void LoadTranslations(string path, Type containerType, Action<IMapRecordFull, string> onTranslationConflict)
        {
            var container = Activator.CreateInstance(containerType) as ITranslationContainer;
            var data = container.Load(path, encoding).OrderBy(itm => itm.Value).ToArray();

            var lst = new SortedItems<IMapRecordFull>() { Comparer = MapRecordComparer.Comparer };

            int repeatCnt = 0;
            int conflictsCnt = 0;
            foreach (var item in data)
            {
                var recItem = (IMapValueRecord)MappedData.GetValueRecord(item.Value);
                if (lst.Contain(recItem))
                {
                    if (string.Equals(recItem.Translation, item.Translation))
                        repeatCnt++;
                    else
                    {
                        conflictsCnt++;
                        Helpers.ConsoleWrite("Конфликтующая запись перевода:", ConsoleColor.Yellow);
                        Helpers.ConsoleWrite(recItem.Value, ConsoleColor.White);
                        Helpers.ConsoleWrite(recItem.Translation, ConsoleColor.White);
                        Helpers.ConsoleWrite(item.Translation, ConsoleColor.Gray);
                        if (onTranslationConflict != null)
                            onTranslationConflict(recItem, item.Translation);
                    }
                    continue;
                }
                recItem.Translation = item.Translation;
                lst.Add(recItem);
            }

            Helpers.ConsoleWrite(string.Format("Повторяющихся записей: {0}\r\nДублирующих записей: {1}", repeatCnt, conflictsCnt), ConsoleColor.Yellow);
            foreach (IMapValueRecord itm in lst)
                OriginalData.Add(new BaseTranslationItem(itm.Value, itm.Translation));
            _TranslatesDictionary.Reset(lst);
        }

        /// <summary>
        /// Сохраняет данные переводов
        /// Если переводы были загружены ранее, то параметры файла будут взяты из оригинала
        /// </summary>
        /// <param name="path">Путь к файлу переводов</param>
        /// <param name="containerType">Тип контейнера</param>
        /// <param name="addnew">Добвить строки с переводом отсутствующие в оригинале</param>
        /// <param name="removeempty">Удалить строки на которые нет ссылок</param>
        public static void SaveTranslations(string path, Type containerType, bool addnew, bool removeempty)
        {
            var lst = new List<ITranslationItem>();
            foreach (IMapValueRecord it in _TranslatesDictionary)
                if (!removeempty || it.Data.Count > 0)
                    lst.Add(new BaseTranslationItem(it.Value, it.Translation));
            if (addnew)
                foreach (IMapValueRecord it in MappedData._ValuesDictionary.Except(_TranslatesDictionary))
                    if (it.Data.Count > 0 && !string.IsNullOrWhiteSpace(it.Translation))
                        lst.Add(new BaseTranslationItem(it.Value, it.Translation));

            SaveTranslations(path, containerType, lst);
        }

        public static void SaveTranslations(string path, Type containerType, ICollection<ITranslationItem> items)
        {
            var container = Activator.CreateInstance(containerType) as ITranslationContainer;
            if (container.Save(path, encoding, items))
            {
                SourcesCount = 0;
                foreach (var itm in items)
                    OriginalData.Add(itm);
            }
        }

        /// <summary>
        /// Возвращает список значений из загружунных переводов, и дополняет его отсутствующими значениями имеющими перевод
        /// </summary>
        /// <returns></returns>
        public static HashSet<IMapValueRecord> GetEntries()
        {
            var lst = new HashSet<IMapValueRecord>();
            foreach (IMapValueRecord it in _TranslatesDictionary)
                lst.Add(it);
            foreach (IMapValueRecord it in MappedData._ValuesDictionary.Except(_TranslatesDictionary))
                if (!string.IsNullOrWhiteSpace(it.Translation))
                    lst.Add(it);
            return lst;
        }

        static KeyValuePair<Type, ContainerFilter>? GetPair(Type cType)
        {
            //контейнеры должны содержать атрибут указывающий поддерживаемые расширения
            var attribute = cType.GetCustomAttribute<ContainerFilter>();
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Name) && attribute.Extensions.Length > 0)
                return new KeyValuePair<Type, ContainerFilter>(cType, attribute);
            return null;
        }

        public static int IndexOfContainer(string Name)
        {
            for (var idx = 0; idx < _List.Length; idx++)
                if (_List[idx].Value.Name == Name)
                    return idx;
            return -1;
        }

        static Translations()
        {
            //загружаем либы из папки с контейнерами
            var containerLibs = Helpers.LoadLibraries(Path.Combine(Helpers.ProgramPath, "Containers"), SearchOption.AllDirectories);
            //ищем контейнеры
            var containerTypes = Helpers.getModules(typeof(ITranslationContainer), containerLibs);
            var containers = new List<KeyValuePair<Type, ContainerFilter>?>() { GetPair(typeof(UTTranslationsContainer)) };
            foreach (var mt in containerTypes)
                containers.Add(GetPair(mt));
            _List = containers.Where(c => c.HasValue).Select(c => c.Value).ToArray();
        }
    }
}
