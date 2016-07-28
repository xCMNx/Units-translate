using System.ComponentModel;

namespace Core
{
    public interface IMapValueRecord : IMapRecordFull, INotifyPropertyChanged
    {
        string Translation
        {
            get;
            set;
        }

        bool WasLinked
        {
            get;
            set;
        }
    }
}