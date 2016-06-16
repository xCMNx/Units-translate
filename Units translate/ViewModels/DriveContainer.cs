using System.Linq;

namespace Units_translate
{
    public class DriveContainer : PathPart
    {
        public override string Path => $"{Name}{System.IO.Path.DirectorySeparatorChar}";
        public override string FullPath => Path;
        public DriveContainer(string name) : base($"{name.First().ToString().ToUpper()}{System.IO.Path.VolumeSeparatorChar}")
        {
        }
    }
}
