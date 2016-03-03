using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Core
{
    public interface IMapDataBase
    {
        /// <summary>
        /// Полный путь к размеченному файлу
        /// </summary>
        string FullPath { get; }
    }

    /// <summary>
    /// Интерфейс данных разметки
    /// </summary>
    public interface IMapData : IMapDataBase, INotifyPropertyChanged
    {
        /// <summary>
        /// Размеченные области
        /// </summary>
        IEnumerable<IMapItemRange> Items { get; }
        /// <summary>
        /// Путь к папке файла
        /// </summary>
        string Path { get; }
        /// <summary>
        /// Название файла без расширения
        /// </summary>
        string Name { get; }
        /// <summary>
        /// расширение файла
        /// </summary>
        string Ext { get; }
        /// <summary>
        /// содержимое файла
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Выполнена ли разметка
        /// </summary>
        bool IsMapped { get; }

        /// <summary>
        /// Возвращает области разметки попадающие в диапазон
        /// </summary>
        /// <param name="start">Начало диапазона поиска</param>
        /// <param name="end">Конец диапазона поиска</param>
        /// <returns></returns>
        IEnumerable<T> ItemsBetween<T>(int start, int end) where T : class, IMapItemRange;

        /// <summary>
        /// Возвращает области разметки по указанному смещению
        /// </summary>
        /// <param name="index">Смещение в тексте</param>
        /// <returns>Найденные области</returns>
        IList<T> ItemsAt<T>(int index) where T : class, IMapItemRange;

        /// <summary>
        /// Возвращает область разметки являющуюся значением по указанному смещению
        /// </summary>
        /// <param name="index">Смещение в тексте</param>
        /// <returns>Найденная область или null</returns>
        IMapValueItem ValueItemAt(int index);

        /// <summary>
        /// Вернет список разметок значения которых является переданный объект. Разметки сами должны сверять себя с объектами.
        /// </summary>
        /// <param name="obj">Искомый объект</param>
        /// <returns></returns>
        IEnumerable<T> GetItemsWithValue<T>(object obj) where T : class, IMapItemBase;

        /// <summary>
        /// Просит переразметить файл
        /// </summary>
        /// <param name="ifChanged">Только изменившийся</param>
        /// <param name="safe">Нужна ли синхронизация</param>
        void Remap(bool ifChanged, bool safe);

        /// <summary>
        /// Очищает список областей разметки
        /// </summary>
        void ClearItems();

        /// <summary>
        /// Сохраняем новый текст файла
        /// </summary>
        /// <param name="text">Новый текст</param>
        void SaveText(string text);
    }
}
