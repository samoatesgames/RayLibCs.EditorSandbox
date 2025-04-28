using Raylib_cs;
using System.Numerics;

namespace RayLibCs.EditorSandbox.Gizmo
{
    public class RayGizmo
    {
        [Flags]
        public enum GizmoFlags
        {
            GIZMO_DISABLED = 0,                 // Disables gizmo drawing

            GIZMO_TRANSLATE = 1 << 0,            // Enables translation gizmo
            GIZMO_ROTATE = 1 << 1,            // Enables rotation gizmo
            GIZMO_SCALE = 1 << 2,            // Enables scaling gizmo (implicitly enables GIZMO_LOCAL)
            GIZMO_ALL = GIZMO_TRANSLATE | GIZMO_ROTATE | GIZMO_SCALE,  // Enables all gizmos

            GIZMO_LOCAL = 1 << 3,            // Orients axes locally
            GIZMO_VIEW = 1 << 4             // Orients axes based on screen view
        }

        [Flags]
        public enum GizmoActiveAxis
        {
            GZ_ACTIVE_X = 1 << 0,              // Active transformation on the X-axis
            GZ_ACTIVE_Y = 1 << 1,              // Active transformation on the Y-axis
            GZ_ACTIVE_Z = 1 << 2,              // Active transformation on the Z-axis
            GZ_ACTIVE_XYZ = GZ_ACTIVE_X | GZ_ACTIVE_Y | GZ_ACTIVE_Z,  // Active transformation on all axes
            GZ_ACTIVE_NONE = 0
        }

        public enum GizmoAction
        {
            GZ_ACTION_NONE = 0,       // No active transformation
            GZ_ACTION_TRANSLATE,      // Translation (movement) transformation
            GZ_ACTION_SCALE,          // Scaling transformation
            GZ_ACTION_ROTATE          // Rotation transformation
        }

        public enum GizmoAxisId
        {
            GZ_AXIS_X = 0,     // Index of the X-axis
            GZ_AXIS_Y = 1,     // Index of the Y-axis
            GZ_AXIS_Z = 2,     // Index of the Z-axis

            GIZMO_AXIS_COUNT = 3  // Total number of axes
        }

        public struct GizmoAxis
        {
            public Vector3 normal;  // Direction of the axis
            public Color color;     // Color used to represent the axis
        }

        public struct GizmoGlobals
        {
            public GizmoAxis[] axisCfg;            // Data related to the 3 axes, globally oriented

            public float gizmoSize;                // Size of the gizmos
            public float lineWidth;                // Width of the lines used to draw the gizmos

            public float trArrowWidthFactor;       // Width of the arrows as a fraction of gizmoSize
            public float trArrowLengthFactor;      // Length of the arrows as a fraction of gizmoSize
            public float trPlaneOffsetFactor;      // Offset of the gizmo planes from the center
            public float trPlaneSizeFactor;        // Size of the planes
            public float trCircleRadiusFactor;     // Radius of the central circle
            public Color trCircleColor;            // Color of the central circle

            public GizmoAction curAction;              // Currently active GizmoAction
            public GizmoActiveAxis activeAxis;                 // Active axis flags (GizmoActiveAxis)

            public Transform startTransform;       // Backup Transform before transformation
            public Transform activeTransform;     // Reference to the active Transform during transformation
            public Vector3 startWorldMouse;        // Mouse position in world space at transformation start

            public GizmoGlobals(int axisCount)
            {
                axisCfg = new GizmoAxis[axisCount];
                gizmoSize = 1.0f;
                lineWidth = 1.0f;
                trArrowWidthFactor = 0.1f;
                trArrowLengthFactor = 0.3f;
                trPlaneOffsetFactor = 0.2f;
                trPlaneSizeFactor = 0.2f;
                trCircleRadiusFactor = 0.5f;
                trCircleColor = new Color(); // Replace with actual default if needed
                curAction = 0;
                activeAxis = 0;
                startTransform = GizmoIdentity();  // Replace with actual default if needed
                activeTransform = GizmoIdentity();
                startWorldMouse = new Vector3();   // Replace with actual default if needed
            }
        }

        public struct GizmoData
        {
            public Matrix4x4 invViewProj;         // Inverted View-Projection matrix

            public Transform curTransform;       // Reference to the current Transform

            public Vector3[] axis;                // Current transformation axes

            public float gizmoSize;               // Adjusted gizmo size

            public Vector3 camPos;                // Camera position
            public Vector3 right;                 // Local right vector
            public Vector3 up;                    // Local up vector
            public Vector3 forward;               // Local forward vector

            public GizmoFlags flags;                     // Configuration flags

            public GizmoData(int axisCount)
            {
                invViewProj = Matrix4x4.Identity;
                curTransform = GizmoIdentity();
                axis = new Vector3[axisCount];
                gizmoSize = 1.0f;
                camPos = new Vector3();
                right = new Vector3(1, 0, 0);
                up = new Vector3(0, 1, 0);
                forward = new Vector3(0, 0, 1);
                flags = 0;
            }
        }

        public static class GizmoConfig
        {
            public static GizmoGlobals GIZMO = new GizmoGlobals((int)GizmoAxisId.GIZMO_AXIS_COUNT)
            {
                axisCfg = new GizmoAxis[]
                {
                    new GizmoAxis
                    {
                        normal = new Vector3(1, 0, 0),
                        color = new Color(229, 72, 91, 255)
                    },
                    new GizmoAxis
                    {
                        normal = new Vector3(0, 1, 0),
                        color = new Color(131, 205, 56, 255)
                    },
                    new GizmoAxis
                    {
                        normal = new Vector3(0, 0, 1),
                        color = new Color(69, 138, 242, 255)
                    }
                },

                gizmoSize = 1.5f,
                lineWidth = 2.5f,

                trArrowLengthFactor = 0.15f,
                trArrowWidthFactor = 0.1f,
                trPlaneOffsetFactor = 0.3f,
                trPlaneSizeFactor = 0.25f,
                trCircleRadiusFactor = 0.1f,
                trCircleColor = new Color(255, 255, 255, 200),

                curAction = (int)GizmoAction.GZ_ACTION_NONE,
                activeAxis = 0
            };
        }

        public static bool DrawGizmo3D(GizmoFlags flags, ref Transform transform)
        {
            //-------------------------------------------------------------------------

            if (flags == GizmoFlags.GIZMO_DISABLED)
                return false;

            //-------------------------------------------------------------------------

            var data = new GizmoData((int)GizmoAxisId.GIZMO_AXIS_COUNT);

            //-------------------------------------------------------------------------

            var matProj = Rlgl.GetMatrixProjection();
            var matView = Rlgl.GetMatrixModelview();
            var invMat = Raymath.MatrixInvert(matView);

            data.invViewProj = Raymath.MatrixMultiply(Raymath.MatrixInvert(matProj), invMat);

            data.camPos = new Vector3(invMat.M14, invMat.M24, invMat.M34);

            data.right = new Vector3(matView.M11, matView.M12, matView.M13);
            data.up = new Vector3(matView.M21, matView.M22, matView.M23);
            data.forward = Raymath.Vector3Normalize(Raymath.Vector3Subtract(transform.Translation, data.camPos));

            data.curTransform = transform;

            data.gizmoSize = GizmoConfig.GIZMO.gizmoSize * Raymath.Vector3Distance(data.camPos, transform.Translation) * 0.1f;

            data.flags = flags;

            ComputeAxisOrientation(ref data);

            //-------------------------------------------------------------------------

            Rlgl.DrawRenderBatchActive();
            var prevLineWidth = Rlgl.GetLineWidth();
            Rlgl.SetLineWidth(GizmoConfig.GIZMO.lineWidth);
            Rlgl.DisableBackfaceCulling();
            Rlgl.DisableDepthTest();
            Rlgl.DisableDepthMask();

            //-------------------------------------------------------------------------

            for (var i = 0; i < (int)GizmoAxisId.GIZMO_AXIS_COUNT; i++)
            {
                if ((data.flags & GizmoFlags.GIZMO_TRANSLATE) != 0)
                {
                    DrawGizmoArrow(ref data, i);
                }
                if ((data.flags & GizmoFlags.GIZMO_SCALE) != 0)
                {
                    DrawGizmoCube(ref data, i);
                }
                if ((data.flags & (GizmoFlags.GIZMO_SCALE | GizmoFlags.GIZMO_TRANSLATE)) != 0)
                {
                    DrawGizmoPlane(ref data, i);
                }
                if ((data.flags & GizmoFlags.GIZMO_ROTATE) != 0)
                {
                    DrawGizmoCircle(ref data, i);
                }
            }

            if ((data.flags & (GizmoFlags.GIZMO_SCALE | GizmoFlags.GIZMO_TRANSLATE)) != 0)
            {
                DrawGizmoCenter(ref data);
            }

            //-------------------------------------------------------------------------

            Rlgl.DrawRenderBatchActive();
            Rlgl.SetLineWidth(prevLineWidth);
            Rlgl.EnableBackfaceCulling();
            Rlgl.EnableDepthTest();
            Rlgl.EnableDepthMask();

            //-------------------------------------------------------------------------

            if (!IsGizmoTransforming() || AreTransformsEqual(ref data.curTransform, ref GizmoConfig.GIZMO.activeTransform))
            {
                GizmoHandleInput(ref data);
            }

            //-------------------------------------------------------------------------

            var res = IsThisGizmoTransforming(ref data);
            transform = data.curTransform;
            return res;
        }

        public static void SetGizmoSize(float size)
        {
            GizmoConfig.GIZMO.gizmoSize = MathF.Max(0, size);
        }

        public static void SetGizmoLineWidth(float width)
        {
            GizmoConfig.GIZMO.lineWidth = MathF.Max(0, width);
        }

        public static void SetGizmoColors(Color x, Color y, Color z, Color center)
        {
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_X].color = x;
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Y].color = y;
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Z].color = z;
            GizmoConfig.GIZMO.trCircleColor = center;
        }

        public static void SetGizmoGlobalAxis(Vector3 right, Vector3 up, Vector3 forward)
        {
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_X].normal = Raymath.Vector3Normalize(right);
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Y].normal = Raymath.Vector3Normalize(up);
            GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Z].normal = Raymath.Vector3Normalize(forward);
        }

        public static Transform GizmoIdentity()
        {
            return new Transform
            {
                Rotation = Quaternion.Identity,
                Scale = new Vector3(1.0f, 1.0f, 1.0f),
                Translation = Vector3.Zero
            };
        }

        public static Matrix4x4 GizmoToMatrix(Transform transform)
        {
            return Raymath.MatrixMultiply(
                Raymath.MatrixMultiply(
                    Raymath.MatrixScale(transform.Scale.X, transform.Scale.Y, transform.Scale.Z),
                    Raymath.QuaternionToMatrix(transform.Rotation)
                    ),
                Raymath.MatrixTranslate(transform.Translation.X, transform.Translation.Y, transform.Translation.Z)
                );
        }

        public static void ComputeAxisOrientation(ref GizmoData gizmoData)
        {
            var flags = gizmoData.flags;

            // Scaling is currently supported only in local mode
            if ((flags & GizmoFlags.GIZMO_SCALE) != 0)
            {
                flags &= ~GizmoFlags.GIZMO_VIEW;
                flags |= GizmoFlags.GIZMO_LOCAL;
            }

            if ((flags & GizmoFlags.GIZMO_VIEW) != 0)
            {
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_X] = gizmoData.right;
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_Y] = gizmoData.up;
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_Z] = gizmoData.forward;
            }
            else
            {
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_X] = GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_X].normal;
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_Y] = GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Y].normal;
                gizmoData.axis[(int)GizmoAxisId.GZ_AXIS_Z] = GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Z].normal;

                if ((flags & GizmoFlags.GIZMO_LOCAL) != 0)
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        gizmoData.axis[i] = Raymath.Vector3Normalize(Raymath.Vector3RotateByQuaternion(gizmoData.axis[i], gizmoData.curTransform.Rotation));
                    }
                }
            }
        }

        public static bool IsGizmoAxisActive(int axis)
        {
            return axis == (int)GizmoAxisId.GZ_AXIS_X && (GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_X) == GizmoActiveAxis.GZ_ACTIVE_X ||
                   axis == (int)GizmoAxisId.GZ_AXIS_Y && (GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Y) == GizmoActiveAxis.GZ_ACTIVE_Y ||
                   axis == (int)GizmoAxisId.GZ_AXIS_Z && (GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Z) == GizmoActiveAxis.GZ_ACTIVE_Z;
        }

        public static bool CheckGizmoType(ref GizmoData data, GizmoFlags type)
        {
            return (data.flags & type) == type;
        }

        public static bool IsGizmoTransforming()
        {
            return GizmoConfig.GIZMO.curAction != GizmoAction.GZ_ACTION_NONE;
        }

        public static bool IsThisGizmoTransforming(ref GizmoData data)
        {
            return IsGizmoTransforming() && AreTransformsEqual(ref data.curTransform, ref GizmoConfig.GIZMO.activeTransform);
        }

        public static bool IsGizmoScaling()
        {
            return GizmoConfig.GIZMO.curAction == GizmoAction.GZ_ACTION_SCALE;
        }

        public static bool IsGizmoTranslating()
        {
            return GizmoConfig.GIZMO.curAction == GizmoAction.GZ_ACTION_TRANSLATE;
        }

        public static bool IsGizmoRotating()
        {
            return GizmoConfig.GIZMO.curAction == GizmoAction.GZ_ACTION_ROTATE;
        }

        public static Vector3 Vec3ScreenToWorld(Vector3 source, Matrix4x4 matViewProjInv)
        {
            var qt = Raymath.QuaternionTransform(new Quaternion(source.X, source.Y, source.Z, 1.0f), matViewProjInv);
            return new Vector3(qt.X / qt.W, qt.Y / qt.W, qt.Z / qt.W);
        }

        private static bool AreTransformsEqual(ref Transform a, ref Transform b)
        {
            if (a.Translation != b.Translation)
            {
                return false;
            }

            if (a.Scale != b.Scale)
            {
                return false;
            }

            if (a.Rotation != b.Rotation)
            {
                return false;
            }

            return true;
        }

        public static Ray Vec3ScreenToWorldRay(Vector2 position, ref Matrix4x4 matViewProjInv)
        {
            var ray = new Ray();

            float width = Raylib.GetScreenWidth();
            float height = Raylib.GetScreenHeight();

            // Convert screen position to device coordinates
            var deviceCoords = new Vector2(2.0f * position.X / width - 1.0f, 1.0f - 2.0f * position.Y / height);

            // Calculate the near and far points in world space
            var nearPoint = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, 0.0f), matViewProjInv);
            var farPoint = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, 1.0f), matViewProjInv);

            // The camera plane pointer position (optional: you can adjust this depending on your setup)
            var cameraPlanePointerPos = Vec3ScreenToWorld(new Vector3(deviceCoords.X, deviceCoords.Y, -1.0f), matViewProjInv);

            // Calculate the ray direction
            var direction = Raymath.Vector3Normalize(Raymath.Vector3Subtract(farPoint, nearPoint));

            // Set ray position (adjust based on your camera projection type)
            ray.Position = cameraPlanePointerPos;

            // Set ray direction
            ray.Direction = direction;

            return ray;
        }

        public static void DrawGizmoCube(ref GizmoData data, int axis)
        {
            if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoScaling()))
            {
                return;
            }

            var gizmoSize = CheckGizmoType(ref data, GizmoFlags.GIZMO_SCALE | GizmoFlags.GIZMO_TRANSLATE)
                ? data.gizmoSize * 0.5f
                : data.gizmoSize;

            var endPos = data.curTransform.Translation + Raymath.Vector3Scale(data.axis[axis], gizmoSize * (1.0f - GizmoConfig.GIZMO.trArrowWidthFactor));

            // Draw the line from the current transform position to the end position
            Raylib.DrawLine3D(data.curTransform.Translation, endPos, GizmoConfig.GIZMO.axisCfg[axis].color);

            var boxSize = data.gizmoSize * GizmoConfig.GIZMO.trArrowWidthFactor;

            // Prepare the box dimensions and coordinates
            var dim1 = Raymath.Vector3Scale(data.axis[(axis + 1) % 3], boxSize);
            var dim2 = Raymath.Vector3Scale(data.axis[(axis + 2) % 3], boxSize);
            var n = data.axis[axis];
            var col = GizmoConfig.GIZMO.axisCfg[axis].color;

            var depth = Raymath.Vector3Scale(n, boxSize);

            var a = endPos - dim1 * 0.5f - dim2 * 0.5f;
            var b = a + dim1;
            var c = b + dim2;
            var d = a + dim2;

            var e = a + depth;
            var f = b + depth;
            var g = c + depth;
            var h = d + depth;

            // Begin drawing the cube using quads
            Rlgl.Begin(DrawMode.Quads);

            Rlgl.Color4ub(col.R, col.G, col.B, col.A);

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.Vertex3f(e.X, e.Y, e.Z);
            Rlgl.Vertex3f(f.X, f.Y, f.Z);
            Rlgl.Vertex3f(g.X, g.Y, g.Z);
            Rlgl.Vertex3f(h.X, h.Y, h.Z);

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(e.X, e.Y, e.Z);
            Rlgl.Vertex3f(f.X, f.Y, f.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(f.X, f.Y, f.Z);
            Rlgl.Vertex3f(g.X, g.Y, g.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(f.X, f.Y, f.Z);
            Rlgl.Vertex3f(e.X, e.Y, e.Z);

            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(g.X, g.Y, g.Z);
            Rlgl.Vertex3f(h.X, h.Y, h.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.End();
        }

        public static void DrawGizmoPlane(ref GizmoData data, int index)
        {
            if (IsThisGizmoTransforming(ref data))
            {
                return;
            }

            var dir1 = data.axis[(index + 1) % 3];
            var dir2 = data.axis[(index + 2) % 3];
            var col = GizmoConfig.GIZMO.axisCfg[index].color;

            var offset = GizmoConfig.GIZMO.trPlaneOffsetFactor * data.gizmoSize;
            var size = GizmoConfig.GIZMO.trPlaneSizeFactor * data.gizmoSize;

            var a = data.curTransform.Translation + Raymath.Vector3Scale(dir1, offset) + Raymath.Vector3Scale(dir2, offset);
            var b = a + Raymath.Vector3Scale(dir1, size);
            var c = b + Raymath.Vector3Scale(dir2, size);
            var d = a + Raymath.Vector3Scale(dir2, size);

            // Begin drawing the plane as a filled quad
            Rlgl.Begin(DrawMode.Quads);

            Rlgl.Color4ub(col.R, col.G, col.B, (byte)(col.A * 0.5f)); // Set semi-transparent color

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.End();

            // Begin drawing the outline of the plane as lines
            Rlgl.Begin(DrawMode.Lines);

            Rlgl.Color4ub(col.R, col.G, col.B, col.A); // Set full opacity color for the outline

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);

            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);

            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.Vertex3f(d.X, d.Y, d.Z);
            Rlgl.Vertex3f(a.X, a.Y, a.Z);

            Rlgl.End();
        }

        public static void DrawGizmoArrow(ref GizmoData data, int axis)
        {
            if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoTranslating()))
            {
                return;
            }

            // Calculate the end position of the arrow
            var endPos = data.curTransform.Translation + Raymath.Vector3Scale(data.axis[axis], data.gizmoSize * (1.0f - GizmoConfig.GIZMO.trArrowLengthFactor));

            // If not scaling, draw the arrow line
            if ((data.flags & GizmoFlags.GIZMO_SCALE) == 0)
            {
                Raylib.DrawLine3D(data.curTransform.Translation, endPos, GizmoConfig.GIZMO.axisCfg[axis].color);
            }

            // Arrow properties
            var arrowLength = data.gizmoSize * GizmoConfig.GIZMO.trArrowLengthFactor;
            var arrowWidth = data.gizmoSize * GizmoConfig.GIZMO.trArrowWidthFactor;

            var dim1 = Raymath.Vector3Scale(data.axis[(axis + 1) % 3], arrowWidth);
            var dim2 = Raymath.Vector3Scale(data.axis[(axis + 2) % 3], arrowWidth);
            var n = data.axis[axis];
            var col = GizmoConfig.GIZMO.axisCfg[axis].color;

            // Tip of the arrow
            var v = endPos + Raymath.Vector3Scale(n, arrowLength);

            // Define vertices for the arrowhead
            var a = endPos - Raymath.Vector3Scale(dim1, 0.5f) - Raymath.Vector3Scale(dim2, 0.5f);
            var b = a + dim1;
            var c = b + dim2;
            var d = a + dim2;

            // Begin drawing the arrowhead as triangles
            Rlgl.Begin(DrawMode.Triangles);

            Rlgl.Color4ub(col.R, col.G, col.B, col.A);

            // Draw the base of the arrow
            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);

            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            // Draw the sides of the arrowhead
            Rlgl.Vertex3f(a.X, a.Y, a.Z);
            Rlgl.Vertex3f(v.X, v.Y, v.Z);
            Rlgl.Vertex3f(b.X, b.Y, b.Z);

            Rlgl.Vertex3f(b.X, b.Y, b.Z);
            Rlgl.Vertex3f(v.X, v.Y, v.Z);
            Rlgl.Vertex3f(c.X, c.Y, c.Z);

            Rlgl.Vertex3f(c.X, c.Y, c.Z);
            Rlgl.Vertex3f(v.X, v.Y, v.Z);
            Rlgl.Vertex3f(d.X, d.Y, d.Z);

            Rlgl.Vertex3f(d.X, d.Y, d.Z);
            Rlgl.Vertex3f(v.X, v.Y, v.Z);
            Rlgl.Vertex3f(a.X, a.Y, a.Z);

            Rlgl.End();
        }

        public static void DrawGizmoCenter(ref GizmoData data)
        {
            var origin = data.curTransform.Translation;

            var radius = data.gizmoSize * GizmoConfig.GIZMO.trCircleRadiusFactor;
            var col = GizmoConfig.GIZMO.trCircleColor;
            var angleStep = 15;

            Rlgl.PushMatrix();
            Rlgl.Translatef(origin.X, origin.Y, origin.Z);

            Rlgl.Begin(DrawMode.Lines);
            Rlgl.Color4ub(col.R, col.G, col.B, col.A);

            for (var i = 0; i < 360; i += angleStep)
            {
                var angle = i * Raylib.DEG2RAD;
                var p = Raymath.Vector3Scale(data.right, (float)Math.Sin(angle) * radius);
                p = Raymath.Vector3Add(p, Raymath.Vector3Scale(data.up, (float)Math.Cos(angle) * radius));
                Rlgl.Vertex3f(p.X, p.Y, p.Z);

                angle += angleStep * Raylib.DEG2RAD;
                p = Raymath.Vector3Scale(data.right, (float)Math.Sin(angle) * radius);
                p = Raymath.Vector3Add(p, Raymath.Vector3Scale(data.up, (float)Math.Cos(angle) * radius));
                Rlgl.Vertex3f(p.X, p.Y, p.Z);
            }

            Rlgl.End();
            Rlgl.PopMatrix();
        }

        public static void DrawGizmoCircle(ref GizmoData data, int axis)
        {
            if (IsThisGizmoTransforming(ref data) && (!IsGizmoAxisActive(axis) || !IsGizmoRotating()))
            {
                return;
            }

            var origin = data.curTransform.Translation;

            var dir1 = data.axis[(axis + 1) % 3];
            var dir2 = data.axis[(axis + 2) % 3];

            var col = GizmoConfig.GIZMO.axisCfg[axis].color;

            var radius = data.gizmoSize;
            var angleStep = 10;

            Rlgl.PushMatrix();
            Rlgl.Translatef(origin.X, origin.Y, origin.Z);

            Rlgl.Begin(DrawMode.Lines);
            Rlgl.Color4ub(col.R, col.G, col.B, col.A);

            for (var i = 0; i < 360; i += angleStep)
            {
                var angle = i * Raylib.DEG2RAD;
                var p = Raymath.Vector3Scale(dir1, (float)Math.Sin(angle) * radius);
                p = Raymath.Vector3Add(p, Raymath.Vector3Scale(dir2, (float)Math.Cos(angle) * radius));
                Rlgl.Vertex3f(p.X, p.Y, p.Z);

                angle += angleStep * Raylib.DEG2RAD;
                p = Raymath.Vector3Scale(dir1, (float)Math.Sin(angle) * radius);
                p = Raymath.Vector3Add(p, Raymath.Vector3Scale(dir2, (float)Math.Cos(angle) * radius));
                Rlgl.Vertex3f(p.X, p.Y, p.Z);
            }

            Rlgl.End();
            Rlgl.PopMatrix();
        }

        public static bool CheckOrientedBoundingBox(ref GizmoData data, Ray ray, Vector3 obbCenter, Vector3 obbHalfSize)
        {
            var oLocal = Raymath.Vector3Subtract(ray.Position, obbCenter);

            var localRay = new Ray();

            localRay.Position.X = Raymath.Vector3DotProduct(oLocal, data.axis[(int)GizmoAxisId.GZ_AXIS_X]);
            localRay.Position.Y = Raymath.Vector3DotProduct(oLocal, data.axis[(int)GizmoAxisId.GZ_AXIS_Y]);
            localRay.Position.Z = Raymath.Vector3DotProduct(oLocal, data.axis[(int)GizmoAxisId.GZ_AXIS_Z]);

            localRay.Direction.X = Raymath.Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxisId.GZ_AXIS_X]);
            localRay.Direction.Y = Raymath.Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxisId.GZ_AXIS_Y]);
            localRay.Direction.Z = Raymath.Vector3DotProduct(ray.Direction, data.axis[(int)GizmoAxisId.GZ_AXIS_Z]);

            var aabbLocal = new BoundingBox
            {
                Min = new Vector3(-obbHalfSize.X, -obbHalfSize.Y, -obbHalfSize.Z),
                Max = new Vector3(obbHalfSize.X, obbHalfSize.Y, obbHalfSize.Z)
            };

            return Raylib.GetRayCollisionBox(localRay, aabbLocal).Hit;
        }

        public static bool CheckGizmoAxis(ref GizmoData data, int axis, Ray ray, GizmoFlags type)
        {
            var halfDim = new float[3];

            halfDim[axis] = data.gizmoSize * 0.5f;
            halfDim[(axis + 1) % 3] = data.gizmoSize * GizmoConfig.GIZMO.trArrowWidthFactor * 0.5f;
            halfDim[(axis + 2) % 3] = halfDim[(axis + 1) % 3];

            if (type == GizmoFlags.GIZMO_SCALE && CheckGizmoType(ref data, GizmoFlags.GIZMO_TRANSLATE | GizmoFlags.GIZMO_SCALE))
            {
                halfDim[axis] *= 0.5f;
            }

            var obbCenter = Raymath.Vector3Add(data.curTransform.Translation, Raymath.Vector3Scale(data.axis[axis], halfDim[axis]));

            return CheckOrientedBoundingBox(ref data, ray, obbCenter, new Vector3(halfDim[0], halfDim[1], halfDim[2]));
        }

        public static bool CheckGizmoPlane(ref GizmoData data, int axis, Ray ray)
        {
            var dir1 = data.axis[(axis + 1) % 3];
            var dir2 = data.axis[(axis + 2) % 3];

            var offset = GizmoConfig.GIZMO.trPlaneOffsetFactor * data.gizmoSize;
            var size = GizmoConfig.GIZMO.trPlaneSizeFactor * data.gizmoSize;

            var a = Raymath.Vector3Add(Raymath.Vector3Add(data.curTransform.Translation, Raymath.Vector3Scale(dir1, offset)),
                Raymath.Vector3Scale(dir2, offset));
            var b = Raymath.Vector3Add(a, Raymath.Vector3Scale(dir1, size));
            var c = Raymath.Vector3Add(b, Raymath.Vector3Scale(dir2, size));
            var d = Raymath.Vector3Add(a, Raymath.Vector3Scale(dir2, size));

            return Raylib.GetRayCollisionQuad(ray, a, b, c, d).Hit;
        }

        public static bool CheckGizmoCircle(ref GizmoData data, int index, Ray ray)
        {
            var origin = data.curTransform.Translation;

            var dir1 = data.axis[(index + 1) % 3];
            var dir2 = data.axis[(index + 2) % 3];

            var circleRadius = data.gizmoSize;
            var angleStep = 10;

            // Calculate sphere radius using the sine of half the angle step
            var sphereRadius = circleRadius * (float)Math.Sin(angleStep * Raylib.DEG2RAD / 2.0f);

            for (var i = 0; i < 360; i += angleStep)
            {
                var angle = i * Raylib.DEG2RAD;
                var p = Raymath.Vector3Add(origin, Raymath.Vector3Scale(dir1, (float)Math.Sin(angle) * circleRadius));
                p = Raymath.Vector3Add(p, Raymath.Vector3Scale(dir2, (float)Math.Cos(angle) * circleRadius));

                if (Raylib.GetRayCollisionSphere(ray, p, sphereRadius).Hit)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckGizmoCenter(ref GizmoData data, Ray ray)
        {
            return Raylib.GetRayCollisionSphere(ray, data.curTransform.Translation, data.gizmoSize * GizmoConfig.GIZMO.trCircleRadiusFactor).Hit;
        }

        public static Vector3 GetWorldMouse(ref GizmoData data)
        {
            var dist = Raymath.Vector3Distance(data.camPos, data.curTransform.Translation);
            var mouseRay = Vec3ScreenToWorldRay(Raylib.GetMousePosition(), ref data.invViewProj);
            return Raymath.Vector3Add(mouseRay.Position, Raymath.Vector3Scale(mouseRay.Direction, dist));
        }

        public static void GizmoHandleInput(ref GizmoData data)
        {
            var action = GizmoConfig.GIZMO.curAction;

            if (action != GizmoAction.GZ_ACTION_NONE)
            {
                if (!Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    // SetMouseCursor(MouseCursor.Default);
                    action = GizmoAction.GZ_ACTION_NONE;
                    GizmoConfig.GIZMO.activeAxis = 0;
                }
                else
                {
                    var endWorldMouse = GetWorldMouse(ref data);
                    var pVec = Raymath.Vector3Subtract(endWorldMouse, GizmoConfig.GIZMO.startWorldMouse);

                    var translation = GizmoConfig.GIZMO.startTransform.Translation;
                    var scale = GizmoConfig.GIZMO.startTransform.Scale;
                    var rotation = GizmoConfig.GIZMO.startTransform.Rotation;

                    switch (action)
                    {
                        case GizmoAction.GZ_ACTION_TRANSLATE:
                            {

                                if (GizmoConfig.GIZMO.activeAxis == GizmoActiveAxis.GZ_ACTIVE_XYZ)
                                {
                                    translation = Raymath.Vector3Add(translation, Raymath.Vector3Project(pVec, data.right));
                                    translation = Raymath.Vector3Add(translation, Raymath.Vector3Project(pVec, data.up));
                                }
                                else
                                {
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, data.axis[(int)GizmoAxisId.GZ_AXIS_X]);
                                        translation = Raymath.Vector3Add(translation, prj);
                                    }
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, data.axis[(int)GizmoAxisId.GZ_AXIS_Y]);
                                        translation = Raymath.Vector3Add(translation, prj);
                                    }
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, data.axis[(int)GizmoAxisId.GZ_AXIS_Z]);
                                        translation = Raymath.Vector3Add(translation, prj);
                                    }
                                }
                            }
                            break;

                        case GizmoAction.GZ_ACTION_SCALE:
                            {
                                if (GizmoConfig.GIZMO.activeAxis == GizmoActiveAxis.GZ_ACTIVE_XYZ)
                                {
                                    var delta = Raymath.Vector3DotProduct(pVec, GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_X].normal);
                                    scale = Raymath.Vector3AddValue(scale, delta);
                                }
                                else
                                {
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_X].normal);
                                        scale = Raymath.Vector3Add(scale, prj);
                                    }
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Y].normal);
                                        scale = Raymath.Vector3Add(scale, prj);
                                    }
                                    if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                                    {
                                        var prj = Raymath.Vector3Project(pVec, GizmoConfig.GIZMO.axisCfg[(int)GizmoAxisId.GZ_AXIS_Z].normal);
                                        scale = Raymath.Vector3Add(scale, prj);
                                    }
                                }
                            }
                            break;

                        case GizmoAction.GZ_ACTION_ROTATE:
                            {
                                // SetMouseCursor(MouseCursor.ResizeEW);
                                var delta = Raymath.Clamp(Raymath.Vector3DotProduct(pVec, Raymath.Vector3Add(data.right, data.up)), -2 * (float)Math.PI, +2 * (float)Math.PI);
                                if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_X) != 0)
                                {
                                    var q = Raymath.QuaternionFromAxisAngle(data.axis[(int)GizmoAxisId.GZ_AXIS_X], delta);
                                    rotation = Quaternion.Multiply(q, rotation);
                                }
                                if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Y) != 0)
                                {
                                    var q = Raymath.QuaternionFromAxisAngle(data.axis[(int)GizmoAxisId.GZ_AXIS_Y], delta);
                                    rotation = Quaternion.Multiply(q, rotation);
                                }
                                if ((GizmoConfig.GIZMO.activeAxis & GizmoActiveAxis.GZ_ACTIVE_Z) != 0)
                                {
                                    var q = Raymath.QuaternionFromAxisAngle(data.axis[(int)GizmoAxisId.GZ_AXIS_Z], delta);
                                    rotation = Quaternion.Multiply(q, rotation);
                                }

                                // Bug fix: Updating the transform "starting point" prevents uncontrolled rotations in local mode
                                GizmoConfig.GIZMO.startTransform = new Transform
                                {
                                    Rotation = rotation,
                                    Scale = scale,
                                    Translation = translation
                                };
                                GizmoConfig.GIZMO.startWorldMouse = endWorldMouse;
                            }
                            break;
                    }

                    GizmoConfig.GIZMO.activeTransform = new Transform
                    {
                        Rotation = rotation,
                        Scale = scale,
                        Translation = translation
                    };

                    data.curTransform = GizmoConfig.GIZMO.activeTransform;
                }
            }
            else
            {
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    var mouseRay = Vec3ScreenToWorldRay(Raylib.GetMousePosition(), ref data.invViewProj);

                    var hit = -1;
                    action = GizmoAction.GZ_ACTION_NONE;

                    for (var k = 0; hit == -1 && k < 2; ++k)
                    {
                        var gizmoFlag = k == 0 ? GizmoFlags.GIZMO_SCALE : GizmoFlags.GIZMO_TRANSLATE;
                        var gizmoAction = k == 0 ? GizmoAction.GZ_ACTION_SCALE : GizmoAction.GZ_ACTION_TRANSLATE;

                        if ((data.flags & gizmoFlag) != 0)
                        {
                            if (CheckGizmoCenter(ref data, mouseRay))
                            {
                                action = gizmoAction;
                                hit = 6;
                                break;
                            }
                            for (var i = 0; i < (int)GizmoAxisId.GIZMO_AXIS_COUNT; ++i)
                            {
                                if (CheckGizmoAxis(ref data, i, mouseRay, gizmoFlag))
                                {
                                    action = gizmoAction;
                                    hit = i;
                                    break;
                                }
                                if (CheckGizmoPlane(ref data, i, mouseRay))
                                {
                                    action = CheckGizmoType(ref data, GizmoFlags.GIZMO_SCALE | GizmoFlags.GIZMO_TRANSLATE) ? GizmoAction.GZ_ACTION_TRANSLATE : gizmoAction;
                                    hit = 3 + i;
                                    break;
                                }
                            }
                        }
                    }

                    if (hit == -1 && (data.flags & GizmoFlags.GIZMO_ROTATE) != 0)
                    {
                        for (var i = 0; i < (int)GizmoAxisId.GIZMO_AXIS_COUNT; ++i)
                        {
                            if (CheckGizmoCircle(ref data, i, mouseRay))
                            {
                                action = GizmoAction.GZ_ACTION_ROTATE;
                                hit = i;
                                break;
                            }
                        }
                    }

                    GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_NONE;
                    if (hit >= 0)
                    {
                        switch (hit)
                        {
                            case 0:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_X;
                                break;
                            case 1:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_Y;
                                break;
                            case 2:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_Z;
                                break;
                            case 3:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_Y | GizmoActiveAxis.GZ_ACTIVE_Z;
                                break;
                            case 4:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_X | GizmoActiveAxis.GZ_ACTIVE_Z;
                                break;
                            case 5:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_X | GizmoActiveAxis.GZ_ACTIVE_Y;
                                break;
                            case 6:
                                GizmoConfig.GIZMO.activeAxis = GizmoActiveAxis.GZ_ACTIVE_XYZ;
                                break;
                        }
                        GizmoConfig.GIZMO.activeTransform = data.curTransform;
                        GizmoConfig.GIZMO.startTransform = data.curTransform;
                        GizmoConfig.GIZMO.startWorldMouse = GetWorldMouse(ref data);
                    }
                }
            }

            GizmoConfig.GIZMO.curAction = action;
        }

    }
}
