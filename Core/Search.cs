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
        public static ICollection<IMapRecord> Exec(string expr, SearchParams param, bool linkedOnly, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;
            var res = new List<IMapRecord>();
            try
            {
                var rgxp = new Regex(expr, CaseInsensitiveSearch ? RegexOptions.IgnoreCase : RegexOptions.None);
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
                foreach (IMapValueRecord it in MappedData._ValuesDictionary)
                    if (ct.IsCancellationRequested)
                        return res;
                    else if ((!linkedOnly || it.Data.Count > 0) && cmpr(it))
                        res.Add(it);
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Green);
            }
            return res;
        }

        /// <summary>
        /// Фильтр по методам, возвращает список соответствующий условиям вхождения в список переданных методов.
        /// </summary>
        /// <param name="methodsFilter">Словарь методов где значение метода указывает, строка должна попадать или не попадать в область метода.</param>
        /// <param name="lst">Фильтруемый список значений.</param>
        /// <returns>Список значений прошедших фильтрацию.</returns>
        public static ICollection<IMapRecord> MethodsFilter(IDictionary<IMapRecord, bool> methodsFilter, ICollection<IMapRecord> lst, CancellationToken ct)
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

            var res = new List<IMapRecord>();
            //осуществляем перебор всех значений
            foreach (IMapRecordFull r in lst)
            {
                if (ct.IsCancellationRequested)
                    return res;
                //зразу откинем файлы в которых нет методов которые должны быть по фильтру
                var fData = mData == null ? r.Data as IList<IMapData> : r.Data.Intersect(mData).ToArray();
                foreach (var d in fData)
                {
                    //флаг обозначающий, что значение прошло все фильтры
                    var found = false;
                    //получим все разметки нашего значения в файле
                    var rItems = d.GetItemsWithValue<IMapValueItem>(r);
                    //пройдемся по каждой
                    foreach (var itm in rItems)
                    {
                        //получим методы охватывающие значение
                        var mts = d.ItemsAt<IMapMethodItem>(itm.Start);
                        //выберем из них те, что есть в фильтре
                        var mtsF = mts.Select(m => methodsFilter.FirstOrDefault(mf => m.IsSameValue(mf.Key))).Where(k => k.Key != null).ToArray();
                        found = true;
                        //посмотрим должны ли быть вхождения в методы не охватывающие значение
                        foreach (var mf in methodsFilter.Except(mtsF))
                            found &= !mf.Value;
                        //и тоже самое в те, что охватывали
                        if (found)
                        {
                            foreach (var mf in mtsF)
                                found &= mf.Value;
                            if (found)
                                break;
                        }
                    }
                    //значение прошло фильтры
                    if (found)
                    {
                        res.Add(r);
                        break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Поиск в словаре. Использует регулярку
        /// </summary>
        /// <param name="expr">
        /// Искомое выражение, в начале можно указать параметры поиска в фармате и фильтры по методам #[filter]:?[params]:[expr]
        /// <para>?e:[expr]   - вырожению должна соответствовать строка</para>
        /// <para>?t:[expr]   - вырожению должен соответствовать перевод</para>
        /// <para>?et:[expr]  - вырожению должны соответствовать и строка и её перевод</para>
        /// <para>?:[expr]    - вырожению должны соответствовать или строка или перевод</para>
        /// </param>
        /// <returns>Список совпадающих записей словаря</returns>
        public static ICollection<IMapRecord> Exec(string expr, CancellationToken ct)
        {
            var lexpr = expr;

            var methodsFilter = new Dictionary<IMapRecord, bool>();
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

                return MethodsFilter(methodsFilter, Exec(lexpr.Substring(i + 1), param, !prm.Contains('a'), ct), ct);
            }

            return MethodsFilter(methodsFilter, Exec(lexpr, SearchParams.EngOrTrans, true, ct), ct);
        }
    }
}
