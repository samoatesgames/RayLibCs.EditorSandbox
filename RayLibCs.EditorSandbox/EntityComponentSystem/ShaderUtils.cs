using ImGuiNET;
using Raylib_cs;
using System.Numerics;

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

        public static void ImGuiColorEdit(string name, ref Color color)
        {
            var vecColor = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            if (ImGui.ColorEdit4(name, ref vecColor))
            {
                color = new Color(vecColor.X, vecColor.Y, vecColor.Z, vecColor.W);
            }
        }
    }
}
