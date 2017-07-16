using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Интерфейс разметки названия модуля
    /// </summary>
    public interface IMapUnitEntry : IMapBaseItem
    {
        string Path { get; set; }
        ICollection<IMapUnitLink> Links { get; }
        IMapData Data { get; set; }
        bool CaseSensitive { get; }
        string ToUnitString();
    }
}
