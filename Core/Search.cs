using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Core
{
    public static class Search
    {
        /// <summary>
        /// Делает поиск не чуствительным к регистру символов
        /// </summary>
        public static bool CaseInsensitiveSearch = true;

        [Flags]
        public enum SearchParams { EngOrTrans = 0, Eng = 1, Trans = 2, Both = 3 }

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">Искомое выражение</param>
        /// <param name="param">Параметры поиска</param>
        /// <param name="linkedOnly">Только связанные с разметкой</param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static ICollection<T> Exec<T>(string expr, SearchParams param, bool linkedOnly, bool acceptEmpty) where T : IMapRecord
        {
            if (!acceptEmpty && string.IsNullOrWhiteSpace(expr))
                return null;
            try
            {
                var rgxp = new Regex(expr, CaseInsensitiveSearch ? RegexOptions.IgnoreCase : RegexOptions.None, new TimeSpan(0,0,1));
                Func<IMapValueRecord, bool> cmpr = null;
                if (param.HasFlag(SearchParams.Eng))
                    if (param.HasFlag(SearchParams.Trans))
                        cmpr = it => rgxp.IsMatch(it.Value) && rgxp.IsMatch(it.Translation);
                    else
                        cmpr = it => rgxp.IsMatch(it.Value);
                else if (param.HasFlag(SearchParams.Trans))
                    cmpr = it => rgxp.IsMatch(it.Translation);
                else
                    cmpr = it => rgxp.IsMatch(it.Value) || rgxp.IsMatch(it.Translation);
                var lst = MappedData._ValuesDictionary.OfType<IMapValueRecord>();
                if (linkedOnly)
                    lst = lst.Where(it => it.Data.Count > 0);
                return lst.Where(cmpr).OfType<T>().ToArray();
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Green);
            }
            return null;
        }

        /// <summary>
        /// Фильтр по методам, возвращает список соответствующий условиям вхождения в список переданных методов.
        /// </summary>
        /// <param name="methodsFilter">Словарь методов где значение метода указывает, строка должна попадать или не попадать в область метода.</param>
        /// <param name="lst">Фильтруемый список значений.</param>
        /// <returns>Список значений прошедших фильтрацию.</returns>
        public static ICollection<IMapRecordFull> MethodsFilter(IDictionary<IMapRecordFull, bool> methodsFilter, ICollection<IMapRecordFull> lst, CancellationToken ct)
        {
            if (methodsFilter == null || methodsFilter.Count() == 0 || lst == null)
                return lst;

            //файлы в которых есть все нужные методы для фильтра
            ICollection<IMapData> mData = null;
            foreach (var itm in methodsFilter)
                if (itm.Value)
                {
                    var tmp = (itm.Key as IMapRecordFull).Data as ICollection<IMapData>;
                    mData = mData == null ? tmp : mData.Intersect(tmp).ToArray();
                }

            var res = new List<IMapRecordFull>();
            //осуществляем перебор всех значений
            foreach (IMapRecordFull r in lst)
            {
                if (ct.IsCancellationRequested)
                    return res;
                //сразу откинем файлы в которых нет методов которые должны быть по фильтру
                var fData = mData == null ? r.Data as IList<IMapData> : r.Data.Intersect(mData).ToArray();
                foreach (var d in fData)
                {
                    //получим все разметки нашего значения в файле
                    var rItems = d.GetItemsWithValue<IMapValueItem>(r);
                    //пройдемся по каждой
                    foreach (var itm in rItems)
                    {
                        //получим методы охватывающие значение
                        var mts = d.ItemsAt<IMapMethodItem>(itm.Start);
                        //выберем из них те, что есть в фильтре
                        var mtsF = mts.Select(m => methodsFilter.FirstOrDefault(mf => m.IsSameValue(mf.Key))).Where(k => k.Key != null).ToArray();
                        //посмотрим должны ли быть вхождения в методы не охватывающие значение
                        //и тоже самое в те, что охватывали
                        if(methodsFilter.Except(mtsF).All(mf => !mf.Value) && mtsF.All(mf => mf.Value))
                        {
                            //значение прошло фильтры
                            res.Add(r);
                            break;
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Выполняет выборку по кастомному фильтру
        /// </summary>
        /// <param name="expr">Фильтр</param>
        /// <param name="list">фильтруемый список записей</param>
        /// <param name="ct">токен отмены</param>
        /// <returns>отфильтрованый список</returns>
        public static ICollection<IMapRecordFull> ExtraFilter(string expr, ICollection<IMapRecordFull> list, CancellationToken ct)
        {
            try
            {
                //выберем все источники участвующие в с результатах и прогоним их через фильтр
                var data = new HashSet<IMapData>(new HashSet<IMapData>(list.SelectMany(i => i.Data)).Where(d => d.Filter(expr)));
                //вернем те результаты у которых хотябы один источник прош фильтр
                return list.Where(l => l.Data.Any(d => data.Contains(d))).ToArray();
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Green);
            }
            return null;
        }

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">
        /// Искомое выражение, в начале можно указать параметры поиска в фармате и фильтры по методам #[filter]:?[params]:[expr]
        /// <para>?e:[expr]   - выражению должна соответствовать строка</para>
        /// <para>?t:[expr]   - выражению должен соответствовать перевод</para>
        /// <para>?et:[expr]  - выражению должны соответствовать и строка и её перевод</para>
        /// </param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static ICollection<IMapRecordFull> Exec(string expr, CancellationToken ct)
        {
            var lexpr = expr;
            string fExpr = null;

            if (lexpr.StartsWith("<"))
            {
                var i = lexpr.IndexOf(":>");
                if (i < 0)
                    return null;

                fExpr = lexpr.Substring(1, i - 1);
                lexpr = lexpr.Substring(i + 2);
            }

            var methodsFilter = new Dictionary<IMapRecordFull, bool>();
            if (lexpr.StartsWith("#"))
            {
                var i = lexpr.IndexOf(':');
                if (i < 0)
                    return null;

                var prms = lexpr.Substring(1, i - 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in prms)
                {
                    var prm = p;
                    if (p[0] == '!')
                        prm = p.Substring(1);
                    var m = MappedData.GetMethodRecord(prm);
                    if (m == null)
                    {
                        Helpers.ConsoleWrite($"Запись {prm} не найдена.");
                        return null;
                    }
                    methodsFilter[m] = p[0] != '!';
                }
                lexpr = lexpr.Substring(i + 1);
            }

            ICollection<IMapRecordFull> filterResult = null;

            if (lexpr.StartsWith("?"))
            {
                var i = lexpr.IndexOf(':');
                if (i < 0)
                    return null;

                SearchParams param = SearchParams.EngOrTrans;
                var prm = lexpr.Substring(0, i).ToLower();
                if (prm.Contains('e'))
                    param |= SearchParams.Eng;
                if (prm.Contains('t'))
                    param |= SearchParams.Trans;

                filterResult = Exec<IMapRecordFull>(lexpr.Substring(i + 1), param, !prm.Contains('a'), prm.Contains('n'));
            }
            else
                filterResult = Exec<IMapRecordFull>(lexpr, SearchParams.EngOrTrans, true, false);

            if (fExpr != null)
                filterResult = ExtraFilter(fExpr, filterResult, ct);

            return MethodsFilter(methodsFilter, filterResult, ct);
        }
    }
}
