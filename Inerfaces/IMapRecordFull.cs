namespace Core
{
    public interface IMapRecordFull : IMapRecord
    {
        ISortedObservableCollection<IMapData> Data
        {
            get;
        }

        int Count
        {
            get;
        }
    }
}
