﻿using System;
using System.Collections.Generic;
using System.IO;
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
        public readonly static Dictionary<string, IMapper> ExtToMapperList = new Dictionary<string, IMapper>();
        /// <summary>
        /// Словарь для быстрого поиска парсера для расширения решения
        /// </summary>
        public readonly static KeyValuePair<ISolutionReader, MapperSolutionFilter>[] SolutionReaders = new KeyValuePair<ISolutionReader, MapperSolutionFilter>[0];

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
                        ExtToMapperList[ext.ToUpper()] = mapper;
                        if (_Unique == null && ext == "*")
                            //парсер умеющий всё
                            _Unique = mapper;
                    }
                }
            }
            _List = mappers.ToArray();

            //ищем парсеры решений
            var solutionReadersTypes = Helpers.getModules(typeof(ISolutionReader), mappersLibs);
            var solLst = new List<KeyValuePair< ISolutionReader, MapperSolutionFilter>>();
            foreach (var mt in solutionReadersTypes)
            {
                var attribute = mt.GetCustomAttribute<MapperSolutionFilter>();
                if (attribute != null)
                    solLst.Add(new KeyValuePair<ISolutionReader, MapperSolutionFilter>((ISolutionReader)Activator.CreateInstance(mt), attribute));
            }
            SolutionReaders = solLst.ToArray();
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
            if (ExtToMapperList.TryGetValue(Ext, out m))
                return m;
            //если нет, то спросим у уникума, умеет ли он в это расширение
            else if(_Unique != null && _Unique.IsExtAcceptable(Ext))
                return _Unique;
            //нет так нет
            return null;
        }
    }
}
