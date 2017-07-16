using Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace yuml.me
{
    /// <summary>
    /// Renders Yuml graphs using 'https://yuml.me/'.
    /// </summary>
    public class YumlGraphRenderer : IYumlGraphRenderer
    {
        public string Name => "yuml.me";

        /// <summary>
        /// Renders the Yuml graph.
        /// </summary>
        /// <param name="yumlGraph">The yuml graph.</param>
        /// <returns>The Yuml as PNG image.</returns>
        public async Task<object> RenderYumlGraphAync(string yumlGraph)
        {
            if (yumlGraph == null)
            {
                throw new ArgumentNullException("yumlGraph");
            }

            var requestContent = new KeyValuePair<string, string>("dsl_text", yumlGraph);

            HttpClient client = new HttpClient();
            var response = await client.PostAsync(
                "https://yuml.me/diagram/plain/class/",
                new FormUrlEncodedContent(Enumerable.Repeat(requestContent, 1))
            );

            response.EnsureSuccessStatusCode();

            string imageId = await response.Content.ReadAsStringAsync();

            byte[] image = await client.GetByteArrayAsync("https://yuml.me/" + imageId);

            return new Bitmap(new MemoryStream(image));
        }

        public override string ToString() => Name;
    }
}
