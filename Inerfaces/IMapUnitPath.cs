namespace Core
{
    /// <summary>
    /// Интерфейс метка для путей подключающих модули
    /// </summary>
    public interface IMapUnitPath: IMapRangeItem
    {
        string getValueFromCode(string code);
        string replaceValueInCode(string code, string value);
    }
}
