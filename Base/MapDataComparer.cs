using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Компарер умеющий сравнивать размеченные данные
    /// Сравнивает полные пути
    /// </summary>
    public class MapDataComparer<T> : Comparer<T> where T : IMapDataBase
    {
        public override int Compare(T m1, T m2) => string.Compare(m1.FullPath, m2.FullPath, StringComparison.OrdinalIgnoreCase);
        public static readonly MapDataComparer<T> Comparer = new MapDataComparer<T>();
    }
}