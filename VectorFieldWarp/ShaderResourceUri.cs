using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorFieldWarp
{
    internal class ShaderResourceUri
    {
        public static Uri Get(string shaderName) => new Uri($"pack://application:,,,/VectorFieldWarp;component/Shaders/{shaderName}.cso", UriKind.Absolute);
    }
}
