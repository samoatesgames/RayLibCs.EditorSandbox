using Raylib_cs;

namespace RayLibCs.EditorSandbox.EntityComponentSystem
{
    internal class ShaderUtils
    {
        public static bool TryLoadShader(string? vertexShaderPath, string? fragmentShaderPath, out Shader shader)
        {
            var vert = vertexShaderPath != null ? File.ReadAllText(vertexShaderPath) : null;
            var frag = fragmentShaderPath != null ? File.ReadAllText(fragmentShaderPath) : null;
            shader = Raylib.LoadShaderFromMemory(vert, frag);
            return shader.Id != 0;
        }
    }
}
