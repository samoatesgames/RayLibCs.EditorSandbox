using ImGuiNET;
using Raylib_cs;
using RayLibCs.EditorSandbox.EntityComponentSystem;
using RayLibCs.EditorSandbox.Gizmo;
using rlImGui_cs;
using System.Numerics;

namespace RayLib.EditorTesting
{
    internal class Program
    {
        static void Main()
        {
            var screenWidth = 1280;
            var screenHeight = 720;

            Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
            Raylib.InitWindow(screenWidth, screenHeight, "RayLibCs - Editor Sandbox");
            Raylib.SetTraceLogLevel(TraceLogLevel.All);

            rlImGui.Setup();

            Camera3D camera;
            camera.Position = new Vector3(7.5f, 5.5f, 5.0f);
            camera.Target = new Vector3(0, 1.5f, 0);
            camera.Up = new Vector3(0.0f, 1.0f, 0.0f);
            camera.FovY = 45.0f;
            camera.Projection = CameraProjection.Perspective;

            var gridSize = 400.0f;
            var gridMesh = Raylib.GenMeshPlane(gridSize, gridSize, 10, 10);
            var gridModel = Raylib.LoadModelFromMesh(gridMesh);

            if (!TryLoadShader("Data/Shaders/grid.vert", "Data/Shaders/grid.frag", out var gridShader))
            {
                return;
            }
            Raylib.SetMaterialShader(ref gridModel, 0, ref gridShader);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_GridHalfPlaneSize"), gridSize * 0.5f, ShaderUniformDataType.Float);

            var gridBias = 1.0f;
            var gridDivides = 10.0f;
            var gridLineWidth = 0.5f;
            var gridMajorLineWidth = 1.0f;
            var gridBaseColor = new Vector4(0, 0, 0, 0.5f);
            var gridLineColor = new Vector4(1, 1, 1, 1);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_GridBias"), gridBias, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_GridDiv"), gridDivides, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_LineWidth"), gridLineWidth, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_MajorLineWidth"), gridMajorLineWidth, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_BaseColor"), gridBaseColor, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_LineColor"), gridLineColor, ShaderUniformDataType.Vec4);



            var outlineSize = 4;
            var outlineColor = new Vector4(1, 0.63f, 0, 1);
            if (!TryLoadShader(null, "Data/Shaders/outline.frag", out var outlineShader))
            {
                return;
            }
            Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "color"), outlineColor, ShaderUniformDataType.Vec4);
            Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "size"), outlineSize, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "width"), screenWidth, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "height"), screenHeight, ShaderUniformDataType.Int);

            RenderTexture2D renderTexture = Raylib.LoadRenderTexture(screenWidth, screenHeight);

            var renderEntities = new List<RenderEntity>();
            for (var i = -1; i <= 1; ++i)
            {
                var crate = new RenderEntity("Data/Models/crate_model.obj", "Data/Textures/crate_texture.jpg");
                crate.UpdateTransform(new Transform
                {
                    Rotation = Quaternion.Identity,
                    Scale = Vector3.One,
                    Translation = new Vector3(2.5f * i, 0.0f, 0.0f)
                });
                renderEntities.Add(crate);
            }

            var gizmoMode = RayGizmo.GizmoFlags.GIZMO_TRANSLATE;
            var gizmoTransform = RayGizmo.GizmoIdentity();

            while (!Raylib.WindowShouldClose())
            {
                if (Raylib.IsMouseButtonDown(MouseButton.Right))
                {
                    Raylib.UpdateCamera(ref camera, CameraMode.Free);
                    Raylib.SetMousePosition(Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight() / 2);
                }

                if (Raylib.IsKeyPressed(KeyboardKey.One))
                {
                    gizmoMode = RayGizmo.GizmoFlags.GIZMO_TRANSLATE;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Two))
                {
                    gizmoMode = RayGizmo.GizmoFlags.GIZMO_ROTATE;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Three))
                {
                    gizmoMode = RayGizmo.GizmoFlags.GIZMO_SCALE;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Four))
                {
                    gizmoMode = RayGizmo.GizmoFlags.GIZMO_ALL;
                }
                
                if (Raylib.GetScreenWidth() != screenWidth || Raylib.GetScreenHeight() != screenHeight)
                {
                    screenWidth = Raylib.GetScreenWidth();
                    screenHeight = Raylib.GetScreenHeight();
                    Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "width"), screenWidth, ShaderUniformDataType.Int);
                    Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "height"), screenHeight, ShaderUniformDataType.Int);

                    Raylib.UnloadRenderTexture(renderTexture);
                    renderTexture = Raylib.LoadRenderTexture(screenWidth, screenHeight);
                }

                var selectedEntities = renderEntities
                    .Where(x => x.IsSelected)
                    .ToArray();

                if (selectedEntities.Any())
                {
                    foreach (var renderEntity in selectedEntities)
                    {
                        renderEntity.UpdateTransform(gizmoTransform);
                    }
                }

                Raylib.SetShaderValue(gridShader, 
                    Raylib.GetShaderLocation(gridShader, "cameraPos"), 
                    camera.Position, 
                    ShaderUniformDataType.Vec3);
                Raylib.SetShaderValueMatrix(gridShader, 
                    Raylib.GetShaderLocation(gridShader, "modelWorldTransform"), 
                    gridModel.Transform);

                gridModel.Transform = RayGizmo.GizmoToMatrix(new Transform
                {
                    Rotation = Quaternion.Identity,
                    Scale = Vector3.One,
                    Translation = camera.Position with { Y = 0.0f }
                });

                Raylib.BeginDrawing();
                {
                    Raylib.ClearBackground(Color.DarkGray);

                    // Main Render
                    Raylib.BeginMode3D(camera);
                    {
                        Raylib.BeginBlendMode(BlendMode.Alpha);
                        {
                            foreach (var renderEntity in renderEntities)
                            {
                                renderEntity.Render(Color.White);
                            }

                            Raylib.BeginShaderMode(gridShader);
                            {
                                Raylib.DrawModel(gridModel, Vector3.Zero, 1.0f, Color.White);
                            }
                            Raylib.EndShaderMode();
                        }
                        Raylib.EndBlendMode();

                        
                    }
                    Raylib.EndMode3D();

                    if (selectedEntities.Any())
                    {
                        // Render selected object in black
                        Raylib.BeginTextureMode(renderTexture);
                        {
                            Raylib.ClearBackground(Color.White);

                            Raylib.BeginMode3D(camera);
                            {
                                foreach (var renderEntity in selectedEntities)
                                {
                                    renderEntity.Render(Color.Black);
                                }
                            }
                            Raylib.EndMode3D();
                        }
                        Raylib.EndTextureMode();

                        // Render outline
                        Raylib.BeginShaderMode(outlineShader);
                        {
                            Raylib.DrawTextureRec(renderTexture.Texture,
                                new Rectangle(0, 0, renderTexture.Texture.Width, -renderTexture.Texture.Height),
                                Vector2.Zero,
                                Color.White);
                        }
                        Raylib.EndShaderMode();

                        // Render gizmo
                        Raylib.BeginMode3D(camera);
                        {
                            RayGizmo.DrawGizmo3D(gizmoMode, ref gizmoTransform);
                        }
                        Raylib.EndMode3D();
                    }

                    // ImGUI
                    rlImGui.Begin();
                    {
                        // Selection Info
                        if (selectedEntities.Any())
                        {
                            if (ImGui.Begin("Selected Entities"))
                            {
                                foreach (var renderEntity in selectedEntities)
                                {
                                    var transform = renderEntity.GetTransform();
                                    var rotation = Raymath.QuaternionToEuler(transform.Rotation);
                                    ImGui.Text($"Position: {transform.Translation.X:F2}, {transform.Translation.Y:F2}, {transform.Translation.Z:F2}");
                                    ImGui.Text($"Rotation: {(rotation.X * Raylib.RAD2DEG):F2}, {(rotation.Y * Raylib.RAD2DEG):F2}, {(rotation.Z * Raylib.RAD2DEG):F2}");
                                    ImGui.Text($"Scale: {transform.Scale.X:F2}, {transform.Scale.Y:F2}, {transform.Scale.Z:F2}");
                                    ImGui.Text("");
                                }
                            }
                            ImGui.End();
                        }

                        // Selection Config
                        if (ImGui.Begin("Selection Config"))
                        {
                            if (ImGui.ColorEdit4("Color", ref outlineColor))
                            {
                                Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "color"), outlineColor, ShaderUniformDataType.Vec4);
                            }

                            if (ImGui.SliderInt("Size", ref outlineSize, 1, 16))
                            {
                                Raylib.SetShaderValue(outlineShader, Raylib.GetShaderLocation(outlineShader, "size"), outlineSize, ShaderUniformDataType.Int);
                            }
                        }
                        ImGui.End();

                        // Gizmo Config
                        if (ImGui.Begin("Gizmo Config"))
                        {
                            ImGui.SliderFloat("Size", ref RayGizmo.GizmoConfig.GIZMO.gizmoSize, 0.5f, 3.0f);
                            ImGui.SliderFloat("Line Width", ref RayGizmo.GizmoConfig.GIZMO.lineWidth, 0.5f, 5.0f);
                            ImGui.SliderFloat("Plane Size", ref RayGizmo.GizmoConfig.GIZMO.trPlaneSizeFactor, 0.1f, 1.0f);
                            ImGui.Text("");
                            ImGuiColorEdit("X-axis Color", ref RayGizmo.GizmoConfig.GIZMO.axisCfg[0].color);
                            ImGuiColorEdit("Y-axis Color", ref RayGizmo.GizmoConfig.GIZMO.axisCfg[1].color);
                            ImGuiColorEdit("Z-axis Color", ref RayGizmo.GizmoConfig.GIZMO.axisCfg[2].color);
                        }
                        ImGui.End();

                        // Grid Config
                        if (ImGui.Begin("Grid Config"))
                        {
                            if (ImGui.SliderFloat("Bias", ref gridBias, 0.0f, 3.0f))
                            {
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_GridBias"), gridBias, ShaderUniformDataType.Float);
                            }
                            var gv = (int)gridDivides;
                            if (ImGui.SliderInt("Divides", ref gv, 1, 100))
                            {
                                gridDivides = gv;
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_GridDiv"), gridDivides, ShaderUniformDataType.Float);
                            }
                            ImGui.Text("");
                            if (ImGui.SliderFloat("Line Width", ref gridLineWidth, 0.0f, 5.0f))
                            {
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_LineWidth"), gridLineWidth, ShaderUniformDataType.Float);
                            }
                            if (ImGui.SliderFloat("Major Line Width", ref gridMajorLineWidth, 0.0f, 10.0f))
                            {
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_MajorLineWidth"), gridMajorLineWidth, ShaderUniformDataType.Float);
                            }
                            if (ImGui.ColorEdit4("Base Color", ref gridBaseColor))
                            {
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_BaseColor"), gridBaseColor, ShaderUniformDataType.Vec4);
                            }
                            if (ImGui.ColorEdit4("Line Color", ref gridLineColor))
                            {
                                Raylib.SetShaderValue(gridShader, Raylib.GetShaderLocation(gridShader, "_LineColor"), gridLineColor, ShaderUniformDataType.Vec4);
                            }
                        }
                        ImGui.End();
                    }
                    rlImGui.End();

                    Raylib.DrawFPS(10, 10);
                }
                Raylib.EndDrawing();

                // Update picking last, to ensure Gizmo state is up to date
                if (!RayGizmo.IsGizmoTransforming() && !ImGui.IsAnyItemActive())
                {
                    if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                    {
                        var ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), camera);

                        RenderEntity? toSelect = null;
                        var closest = float.MaxValue;

                        foreach (var renderEntity in renderEntities)
                        {
                            if (renderEntity.IntersectsRay(ray, out var distance) && distance < closest)
                            {
                                closest = distance;
                                toSelect = renderEntity;
                            }
                        }

                        if (toSelect == null)
                        {
                            foreach (var selected in selectedEntities)
                            {
                                selected.IsSelected = false;
                            }
                        }
                        else if (!toSelect.IsSelected)
                        {
                            foreach (var selected in selectedEntities)
                            {
                                selected.IsSelected = false;
                            }

                            toSelect.IsSelected = true;
                            gizmoTransform = toSelect.GetTransform();
                        }
                    }
                }
            }

            Raylib.CloseWindow();
        }

        private static bool TryLoadShader(string? vertexShaderPath, string? fragmentShaderPath, out Shader shader)
        {
            var vert = vertexShaderPath != null ? File.ReadAllText(vertexShaderPath) : null;
            var frag = fragmentShaderPath != null ? File.ReadAllText(fragmentShaderPath) : null;
            shader = Raylib.LoadShaderFromMemory(vert, frag);
            return shader.Id != 0;
        }

        private static void ImGuiColorEdit(string name, ref Color color)
        {
            var vecColor = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            if (ImGui.ColorEdit4(name, ref vecColor))
            {
                color = new Color(vecColor.X, vecColor.Y, vecColor.Z, vecColor.W);
            }
        }
    }
}
