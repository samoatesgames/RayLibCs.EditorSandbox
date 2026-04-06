using ImGuiNET;
using Raylib_cs;
using RayLibCs.EditorSandbox.Gizmo;
using System.Numerics;

namespace RayLibCs.EditorSandbox.EntityComponentSystem
{
    internal class Skybox
    {
        private readonly Shader m_shader;
        private float m_timeOfDay = 0.7f;

        private readonly int m_timeOfDayLoc;
        private readonly int m_dayColorLoc;
        private readonly int m_nightColorLoc;
        private readonly int m_horizonColorLoc;
        private readonly int m_starLoc;
        private readonly int m_timeLoc;
        private readonly int m_cameraPosLoc;

        private Vector3 m_dayColor;
        private Vector3 m_nightColor;
        private Vector3 m_horizonColor;
        private float m_starIntensity = 0.35f;

        public Skybox()
        {
            if (!ShaderUtils.TryLoadShader("Data/Shaders/skybox.vert", "Data/Shaders/skybox.frag", out m_shader))
            {
                return;
            }

            m_timeOfDayLoc = Raylib.GetShaderLocation(m_shader, "timeOfDay");
            m_dayColorLoc = Raylib.GetShaderLocation(m_shader, "dayColor");
            m_nightColorLoc = Raylib.GetShaderLocation(m_shader, "nightColor");
            m_horizonColorLoc = Raylib.GetShaderLocation(m_shader, "horizonColor");
            m_starLoc = Raylib.GetShaderLocation(m_shader, "starIntensity");
            m_timeLoc = Raylib.GetShaderLocation(m_shader, "time");
            m_cameraPosLoc = Raylib.GetShaderLocation(m_shader, "cameraPos");

            m_dayColor = new Vector3(127 / 255.0f, 200 / 255.0f, 255 / 255.0f);
            m_nightColor = new Vector3(20 / 255.0f, 20 / 255.0f, 20 / 255.0f);
            m_horizonColor = new Vector3(1.0f, 0.5f, 0.2f);
        }

        public void Render(Camera3D camera)
        {
            Raylib.SetShaderValue(m_shader, m_cameraPosLoc, camera.Position, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(m_shader, m_dayColorLoc, m_dayColor, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(m_shader, m_nightColorLoc, m_nightColor, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(m_shader, m_horizonColorLoc, m_horizonColor, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(m_shader, m_starLoc, m_starIntensity, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(m_shader, m_timeLoc, Raylib.GetTime(), ShaderUniformDataType.Float);
            Raylib.SetShaderValue(m_shader, m_timeOfDayLoc, m_timeOfDay, ShaderUniformDataType.Float);

            Rlgl.DisableBackfaceCulling();
            Rlgl.DisableDepthMask();
            Raylib.BeginShaderMode(m_shader);
            Raylib.DrawCube(camera.Position, 1000f, 1000f, 1000f, Color.White);
            Raylib.EndShaderMode();
            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthMask();
        }

        public void DrawImGui()
        {
            if (ImGui.Begin("Skybox Config"))
            {
                ImGui.SliderFloat("Time Of Day", ref m_timeOfDay, 0.0f, 1.0f);
                var time = TimeOnly.FromTimeSpan(
                    TimeSpan.FromSeconds(m_timeOfDay * 86399)
                );
                ImGui.TextDisabled($"Time Of Day: {time}");

                ImGui.ColorEdit3("Day Color", ref m_dayColor);
                ImGui.ColorEdit3("Night Color", ref m_nightColor);
                ImGui.ColorEdit3("Horizon Color", ref m_horizonColor);

                ImGui.SliderFloat("Star Intensity", ref m_starIntensity, 0.0f, 1.0f);
            }
            ImGui.End();
        }
    }
}
