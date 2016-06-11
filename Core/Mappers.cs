using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core
{
    public static class Mappers
    {
        static IMapper[] _List;
        /// <summary>
        /// Список парсеров
        /// </summary>
        public static IMapper[] List => _List;

        /// <summary>
        /// Словарь для быстрого поиска парсера для расширения
        /// </summary>
        public static Dictionary<string, IMapper> _ExtToMapperList = new Dictionary<string, IMapper>();
        /// <summary>
        /// Словарь для быстрого поиска парсера для расширения решения
        /// </summary>
        public static Dictionary<string, ISolutionReader> _SolutionExtToMapperList = new Dictionary<string, ISolutionReader>();

        public static string SolutionExts => string.Join("|", _SolutionExtToMapperList.Keys.Select(k => string.Format("{0}|*{0}", k)).ToArray());

        /// <summary>
        /// Парсер умеющий все расширения, его атрибут *
        /// </summary>
        static IMapper _Unique;

        static Mappers()
        {
            //загружаем либы из папки с парсерами
            var mappersLibs = Helpers.LoadLibraries(Path.Combine(Helpers.ProgramPath, "Parsers"), SearchOption.AllDirectories);
            //ищем парсеры
            var mapperTypes = Helpers.getModules(typeof(IMapper), mappersLibs);
            var mappers = new List<IMapper>();
            foreach (var mt in mapperTypes)
            {
                //парсеры должны содержать атрибут указывающий поддерживаемые расширения
                var attribute = mt.GetCustomAttribute<MapperFilter>();
                if (attribute != null && attribute.Extensions.Length > 0)
                {
                    //инициализируем парсер
                    var mapper = (IMapper)Activator.CreateInstance(mt);
                    mappers.Add(mapper);
                    //наполяем словарь связывающий расширение с парсером
                    foreach (var ext in attribute.Extensions)
                    {
                        _ExtToMapperList[ext.ToUpper()] = mapper;
                        if (_Unique == null && ext == "*")
                            //парсер умеющий всё
                            _Unique = mapper;
                    }
                }
            }
            _List = mappers.ToArray();

            //ищем парсеры решений
            var solutionReadersTypes = Helpers.getModules(typeof(ISolutionReader), mappersLibs);
            foreach (var mt in solutionReadersTypes)
            {
                var reader = (ISolutionReader)Activator.CreateInstance(mt);
                var attribute = mt.GetCustomAttribute<MapperSolutionFilter>();
                foreach (var ext in attribute.Extensions)
                    _SolutionExtToMapperList[ext.ToUpper()] = reader;
            }
        }

        /// <summary>
        /// Возвращает подходящий парсер
        /// </summary>
        /// <param name="Ext">Расширение</param>
        /// <returns>Парсер если найден подходящий или null</returns>
        public static IMapper FindMapper(string Ext)
        {
            Ext = Ext.ToUpper();
            IMapper m;
            //сначала посмотрим в словаре
            if (_ExtToMapperList.TryGetValue(Ext, out m))
                return m;
            //если нет, то спросим у уникума, умеет ли он в это расширение
            else if(_Unique != null && _Unique.IsExtAcceptable(Ext))
                return _Unique;
            //нет так нет
            return null;
        }

        /// <summary>
        /// Возвращает подходящий парсер решения
        /// </summary>
        /// <param name="Ext">Расширение</param>
        /// <returns>Парсер если найден подходящий или null</returns>
        public static ISolutionReader FindSolutionReader(string Ext)
        {
            Ext = Ext.ToUpper();
            ISolutionReader m;
            //сначала посмотрим в словаре
            if (_SolutionExtToMapperList.TryGetValue(Ext, out m))
                return m;
            //нет так нет
            return null;
        }
    }
}
