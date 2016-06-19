namespace Units_translate
{
    public interface IUpdateLockable
    {
        bool IsUpdating { get; }

        void BeginUpdate();
        void EndUpdate();
    }
}
