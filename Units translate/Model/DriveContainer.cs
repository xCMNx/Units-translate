using System.Linq;

namespace Units_translate
{
    public class DriveContainer : PathPart
    {
        public override string FullPath => $"{Name}{System.IO.Path.DirectorySeparatorChar}";
        public DriveContainer(string name) : base($"{name.First().ToString().ToUpper()}{System.IO.Path.VolumeSeparatorChar}")
        {
        }
    }
}
