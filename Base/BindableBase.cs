using System.ComponentModel;

namespace Core
{
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyPropertiesChanged(params string[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (var prop in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        public void Route(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(sender, e);
        }
    }
}
