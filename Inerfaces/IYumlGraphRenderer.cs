using System.Threading.Tasks;

namespace Core
{
    /// <summary>
    /// Interface for Yuml graph rendering.
    /// </summary>
    public interface IYumlGraphRenderer
    {
        string Name { get; }
        /// <summary>
        /// Renders the Yuml graph.
        /// </summary>
        /// <param name="yumlGraph">The yuml graph.</param>
        /// <returns>The Yuml as PNG image.</returns>
        Task<object> RenderYumlGraphAync(string yumlGraph);
    }
}
