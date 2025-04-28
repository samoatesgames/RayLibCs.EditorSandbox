using Raylib_cs;
using RayLibCs.EditorSandbox.Gizmo;
using System.Numerics;

namespace RayLibCs.EditorSandbox.EntityComponentSystem
{
    public class RenderEntity
    {
        private Model m_model;
        private Transform m_transform;

        public bool IsSelected { get; set; }

        public RenderEntity(string modelPath, string albedoTexturePath)
        {
            var texture = Raylib.LoadTexture(albedoTexturePath);
            Raylib.GenTextureMipmaps(ref texture);
            Raylib.SetTextureFilter(texture, TextureFilter.Anisotropic16X);

            m_model = Raylib.LoadModel(modelPath);
            Raylib.SetMaterialTexture(ref m_model, 0, MaterialMapIndex.Albedo, ref texture);

            m_transform = RayGizmo.GizmoIdentity();
        }

        public void Render(Color renderTint)
        {
            Raylib.DrawModel(m_model, Vector3.Zero, 1.0f, renderTint);
        }

        public bool IntersectsRay(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            var doesIntersect = false;
            unsafe
            {
                for (var m = 0; m < m_model.MeshCount; m++)
                {
                    var meshHitInfo = Raylib.GetRayCollisionMesh(ray, m_model.Meshes[m], m_model.Transform);
                    if (meshHitInfo.Hit && meshHitInfo.Distance < distance)
                    {
                        distance = meshHitInfo.Distance;
                        doesIntersect = true;
                    }
                }

                return doesIntersect;
            }
        }

        public void UpdateTransform(Transform transform)
        {
            m_transform = transform;
            m_model.Transform = RayGizmo.GizmoToMatrix(m_transform);
        }

        public Transform GetTransform()
        {
            return m_transform;
        }
    }
}
