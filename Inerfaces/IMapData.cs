using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Core
{
    /// <summary>
    /// Интерфейс данных разметки
    /// </summary>
    public interface IMapData : IComparable, IEquatable<IMapData>, IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Размеченные области
        /// </summary>
        ICollection<IMapRangeItem> Items { get; }
        /// <summary>
        /// Cодержимое
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Выполнена ли разметка
        /// </summary>
        bool IsMapped { get; }
        /// <summary>
        /// Были ли изменения с последней разметки
        /// </summary>
        bool IsChanged { get; }

        /// <summary>
        /// Возвращает области разметки попадающие в диапазон
        /// </summary>
        /// <param name="offsetStart">Начало диапазона поиска</param>
        /// <param name="offsetEnd">Конец диапазона поиска</param>
        /// <returns></returns>
        ICollection<T> ItemsBetween<T>(int offsetStart, int offsetEnd) where T : class, IMapRangeItem;

        /// <summary>
        /// Возвращает области разметки по указанному смещению
        /// </summary>
        /// <param name="offset">Смещение в тексте</param>
        /// <returns>Найденные области</returns>
        ICollection<T> ItemsAt<T>(int offset) where T : IMapRangeItem;

        /// <summary>
        /// Возвращает область разметки являющуюся значением по указанному смещению
        /// </summary>
        /// <param name="offset">Смещение в тексте</param>
        /// <returns>Найденная область или null</returns>
        IMapValueItem ValueItemAt(int offset);

        /// <summary>
        /// Вернет список разметок значения которых является переданный объект. Разметки сами должны сверять себя с объектами.
        /// </summary>
        /// <param name="obj">Искомый объект</param>
        /// <returns></returns>
        ICollection<T> GetItemsWithValue<T>(object obj) where T : IMapBaseItem;

        /// <summary>
        /// Вернет количество разметок значения которых является переданный объект. Разметки сами должны сверять себя с объектами.
        /// </summary>
        /// <param name="obj">Искомый объект</param>
        /// <returns></returns>
        int GetItemsCountWithValue(object obj);

        /// <summary>
        /// Просит переразметить файл
        /// </summary>
        /// <param name="ifChanged">Только изменившийся</param>
        void Remap(bool ifChanged);

        /// <summary>
        /// Сохраняем новый текст файла
        /// </summary>
        /// <param name="text">Новый текст</param>
        void SaveText(string text);

        /// <summary>
        /// Фильтр источника, каждый источник сам обрабатывает фильтр и решает прошел он его или нет
        /// </summary>
        /// <param name="filter">Текстовое значение фильтра</param>
        /// <returns>True если прошел</returns>
        bool Filter(string filter);
    }
}
