using Raylib_cs;
using System.Numerics;

namespace RayLibCs.EditorSandbox.EntityComponentSystem
{
    internal class Skybox
    {
        private readonly Model m_skyModel;

        public Skybox()
        {
            if (!ShaderUtils.TryLoadShader("Data/Shaders/skybox.vert", "Data/Shaders/skybox.frag", out var skyboxShader))
            {
                return;
            }
            var skyMesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
            m_skyModel = Raylib.LoadModelFromMesh(skyMesh);
            Raylib.SetShaderValue(
                skyboxShader,
                Raylib.GetShaderLocation(skyboxShader, "environmentMap"),
                (int)MaterialMapIndex.Cubemap,
                ShaderUniformDataType.Int
            );
            Raylib.SetMaterialShader(ref m_skyModel, 0, ref skyboxShader);

            var skyboxImage = Raylib.LoadImage("Data/Textures/skybox.png");
            var skyCube = Raylib.LoadTextureCubemap(skyboxImage, CubemapLayout.AutoDetect);
            Raylib.SetMaterialTexture(ref m_skyModel, 0, MaterialMapIndex.Cubemap, ref skyCube);
            Raylib.UnloadImage(skyboxImage);
        }

        public void Render()
        {

            Rlgl.DisableBackfaceCulling();
            Rlgl.DisableDepthMask();
            Raylib.DrawModel(m_skyModel, Vector3.Zero, 1.0f, Color.White);
            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthMask();
        }
    }
}
