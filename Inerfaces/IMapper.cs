using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    /// <summary>
    /// Атрибут для парсеров, указывающий какие расширения парсер умеет
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class MapperFilter : System.Attribute
    {
        public string[] Extensions;
        public MapperFilter(string[] extensions)
        {
            Extensions = extensions;
        }
    }

    public class MapperException : Exception { public MapperException(string msg) : base(msg) { } }
    /// <summary>
    /// Такое исключение должен генерить только маппер поддерживающий исправление файлов
    /// </summary>
    public class MapperFixableException : Exception { public MapperFixableException(string msg) : base(msg) { } }

    /// <summary>
    /// Собсно интерфейс парсера
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// название парсера
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Метод возвращающие найденные области в тексте
        /// </summary>
        /// <param name="Text">Текст для обработки</param>
        /// <param name="Ext">Расширение на случай если парсер умеет много типов и в процессе ему нужно уточнить, с чем он работает</param>
        /// <returns></returns>
        IEnumerable<IMapItemRange> GetMap(string Text, string Ext);
        /// <summary>
        /// Просим маппер исправить файл
        /// </summary>
        /// <param name="FilePath">Путь к файлу</param>
        /// <param name="encoding">Кодировка</param>
        /// <returns></returns>
        void TryFix(string FilePath, Encoding encoding);
        /// <summary>
        /// Поддерживает ли парсер данное расширение
        /// </summary>
        /// <param name="Ext">Расширение файла</param>
        bool IsExtAcceptable(string Ext);
    }

    /// <summary>
    /// Парсер с настройками
    /// </summary>
    public interface IConfigurableMapper : IMapper
    {
        /// <summary>
        /// Контрол настроек
        /// </summary>
        object SettingsControl { get; }
        /// <summary>
        /// Сброс настроек
        /// </summary>
        void Reset();
        /// <summary>
        /// Применение настроек
        /// </summary>
        void Save();
    }
}
