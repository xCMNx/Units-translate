namespace Core
{
    /// <summary>
    /// Интерфейс указывающий на то, что элемент разметки может хранить запись из хранилища значений
    /// </summary>
    public interface IMapOptimizableValueItem : IMapBaseItem
    {
        void SwapValueToMapRecord(IMapRecord record);
    }
}
