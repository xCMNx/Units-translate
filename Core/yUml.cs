using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Core
{
    public static class yUml
    {
        public static List<IYumlGraphRenderer> yUmlRenderers = new List<IYumlGraphRenderer>();
        class TextRenderer : IYumlGraphRenderer
        {
            public string Name => "Code";

            public async Task<object> RenderYumlGraphAync(string yumlGraph)
            {
                await Task.Yield();
                return yumlGraph;
            }
            public override string ToString() => Name;
        }

        static yUml()
        {
            var yUmlRenderersLibs = Helpers.LoadLibraries(Path.Combine(Helpers.ProgramPath, "yUml"), SearchOption.AllDirectories);
            var yUmlRenderersTypes = Helpers.getModules(typeof(IYumlGraphRenderer), yUmlRenderersLibs);
            foreach (var mt in yUmlRenderersTypes)
                yUmlRenderers.Add(Activator.CreateInstance(mt) as IYumlGraphRenderer);
            yUmlRenderers.Add(new TextRenderer());
        }
    }
}
