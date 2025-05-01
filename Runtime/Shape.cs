using System.Collections.Generic;
using JD.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace JD.Shapes
{
    public static class Shape
    {
        private struct DebugLineInfo
        {
            public Vector3 Start;
            public Vector3 End;
            public Color Color;
        }
        
        private struct RectOneShotInfo
        {
            public Rect Rect;
            public Color Color;
            public float Time;
            public float Duration;
        }
        
        private struct CircleOneShotInfo
        {
            public Vector3 Position;
            public float Radius;
            public Color Color;
            public float Time;
            public float Duration;
        }
        
        private struct ArcOneShotInfo
        {
            public Vector3 Position;
            public float Radius;
            public float Angle;
            public float Degrees;
            public Color Color;
            public float Time;
            public float Duration;
        }
        
        private struct TriangleOneShotInfo
        {
            public Vector3 Position;
            public float Size;
            public float Angle;
            public Color Color;
            public float Time;
            public float Duration;
        }
        
        private struct ArrowOneShotInfo
        {
            public Vector3 Start;
            public Vector3 End;
            public float Width;
            public Color Color;
            public float Time;
            public float Duration;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoadRuntimeMethod()
        {
            CreateRoot();
        }
        
        private static void CreateRoot()
        {
#if UNITY_EDITOR
            if (_root == null)
#else
            if (!_hasRoot)
#endif
            {
                var obj = new GameObject("Shapes");
                _root = obj.AddComponent<ShapeRoot>();
                _hasRoot = true;
                Object.DontDestroyOnLoad(obj);
            }
        }

        internal static void OnUpdate()
        {
            // One Shots
            if (_rectOneShots.Count > 0)
            {
                var now = Time.time;
                while (_rectOneShots.TryDequeue(out var info))
                {
                    var time = now - info.Time;
                    var a = Mathf.Max(1 - time / info.Duration, 0);
                    RectInternal(info.Rect, info.Color.ToAlpha(a));
                    if (a > 0) _rectOneShotsTemp.Enqueue(info);
                }
                (_rectOneShotsTemp, _rectOneShots) = (_rectOneShots, _rectOneShotsTemp);
            }
            
            if (_circleOneShots.Count > 0)
            {
                var now = Time.time;
                while (_circleOneShots.TryDequeue(out var info))
                {
                    var time = now - info.Time;
                    var a = Mathf.Max(1 - time / info.Duration, 0);
                    Circle(info.Position, info.Radius, info.Color.ToAlpha(a));
                    if (a > 0) _circleOneShotsTemp.Enqueue(info);
                }
                (_circleOneShotsTemp, _circleOneShots) = (_circleOneShots, _circleOneShotsTemp);
            }
            
            if (_arcOneShots.Count > 0)
            {
                var now = Time.time;
                while (_arcOneShots.TryDequeue(out var info))
                {
                    var time = now - info.Time;
                    var a = Mathf.Max(1 - time / info.Duration, 0);
                    Arc(info.Position, info.Radius, info.Angle, info.Degrees, info.Color.ToAlpha(a));
                    if (a > 0) _arcOneShotsTemp.Enqueue(info);
                }
                (_arcOneShotsTemp, _arcOneShots) = (_arcOneShots, _arcOneShotsTemp);
            }
            
            if (_triangleOneShots.Count > 0)
            {
                var now = Time.time;
                while (_triangleOneShots.TryDequeue(out var info))
                {
                    var time = now - info.Time;
                    var a = Mathf.Max(1 - time / info.Duration, 0);
                    Triangle(info.Position, info.Size, info.Angle, info.Color.ToAlpha(a));
                    if (a > 0) _triangleOneShotsTemp.Enqueue(info);
                }
                (_triangleOneShotsTemp, _triangleOneShots) = (_triangleOneShots, _triangleOneShotsTemp);
            }
            
            if (_arrowOneShots.Count > 0)
            {
                var now = Time.time;
                while (_arrowOneShots.TryDequeue(out var info))
                {
                    var time = now - info.Time;
                    var a = Mathf.Max(1 - time / info.Duration, 0);
                    Arrow(info.Start, info.End, info.Color.ToAlpha(a), info.Width);
                    if (a > 0) _arrowOneShotsTemp.Enqueue(info);
                }
                (_arrowOneShotsTemp, _arrowOneShots) = (_arrowOneShots, _arrowOneShotsTemp);
            }
        }
        
        internal static void OnRender()
        {
            // Debug Lines
            if (_debugLines.Count == 0) return;
            
#if UNITY_EDITOR
            if (_lineMaterial == null)
#else
            if (!_hasLineMaterial)
#endif
            {
                _lineMaterial = new Material(Shader.Find("Hidden/Shapes/Debug"));
                _hasLineMaterial = true;
            }
            
            GL.PushMatrix();
            GL.MultMatrix(ShapeCommon.LineMatrix);
            
            _lineMaterial.SetPass(0);
            
            GL.Begin(GL.LINES);
            
            while (_debugLines.TryDequeue(out var info))
                DebugLineInternal(info.Start, info.End, info.Color);
            
            GL.End();
            GL.PopMatrix();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _debugLines?.Clear();
            _rectOneShots?.Clear();
            _rectOneShotsTemp?.Clear();
            _circleOneShots?.Clear();
            _circleOneShotsTemp?.Clear();
            _arcOneShots?.Clear();
            _arcOneShotsTemp?.Clear();
            _triangleOneShots?.Clear();
            _triangleOneShotsTemp?.Clear();
            _arrowOneShots?.Clear();
            _arrowOneShotsTemp?.Clear();
            _lineMaterial = null;
            _hasLineMaterial = false;
            _root = null;
            _hasRoot = false;
        }
        
        private static Material _lineMaterial;
        private static bool _hasLineMaterial;

        private static ShapeRoot _root;
        private static bool _hasRoot;
        
        private static readonly Queue<DebugLineInfo> _debugLines = new Queue<DebugLineInfo>(100);
        private static Queue<RectOneShotInfo> _rectOneShots = new Queue<RectOneShotInfo>(100);
        private static Queue<RectOneShotInfo> _rectOneShotsTemp = new Queue<RectOneShotInfo>(100);
        private static Queue<CircleOneShotInfo> _circleOneShots = new Queue<CircleOneShotInfo>(100);
        private static Queue<CircleOneShotInfo> _circleOneShotsTemp = new Queue<CircleOneShotInfo>(100);
        private static Queue<ArcOneShotInfo> _arcOneShots = new Queue<ArcOneShotInfo>(100);
        private static Queue<ArcOneShotInfo> _arcOneShotsTemp = new Queue<ArcOneShotInfo>(100);
        private static Queue<TriangleOneShotInfo> _triangleOneShots = new Queue<TriangleOneShotInfo>(100);
        private static Queue<TriangleOneShotInfo> _triangleOneShotsTemp = new Queue<TriangleOneShotInfo>(100);
        private static Queue<ArrowOneShotInfo> _arrowOneShots = new Queue<ArrowOneShotInfo>(100);
        private static Queue<ArrowOneShotInfo> _arrowOneShotsTemp = new Queue<ArrowOneShotInfo>(100);
        
        public static void Arc(Vector3 position, float radius, float angle, float degrees, Color color)
        {
            Shapes.Circle.Draw(new CircleInfo{
                Center = position,
                Forward = ShapeCommon.CircleRotation,
                Radius = radius,
                FillColor = color.ToAlpha(ShapeCommon.Alpha * color.a),
                BorderColor = color,
                BorderWidth = ShapeCommon.CircleBorderWidth,
                Bordered = true,
                IsSector = true,
                SectorInitialAngleInDegrees = angle - degrees / 2f,
                SectorArcLengthInDegrees = degrees,
            });
        }
        
        public static void ArcOneShot(Vector3 position, float radius, float angle, float degrees, Color color, float duration = 0.25f)
        {
            if (!Application.isPlaying) return;
            
            _arcOneShots.Enqueue(new ArcOneShotInfo()
            {
                Position = position,
                Radius = radius,
                Angle = angle,
                Degrees = degrees,
                Color =  color,
                Duration = duration,
                Time = Time.time
            });
        }
        
        public static void Circle(Vector3 position, float radius, Color color)
        {
            Shapes.Circle.Draw(new CircleInfo{
                Center = position,
                Forward = ShapeCommon.CircleRotation,
                Radius = radius,
                FillColor = color.ToAlpha(ShapeCommon.Alpha * color.a),
                BorderColor = color,
                BorderWidth = ShapeCommon.CircleBorderWidth,
                Bordered = true,
            });
        }
        
        public static void CircleOneShot(Vector3 position, float radius, Color color, float duration = 0.25f)
        {
            if (!Application.isPlaying) return;
            
            _circleOneShots.Enqueue(new CircleOneShotInfo()
            {
                Position = position,
                Radius = radius,
                Color =  color,
                Duration = duration,
                Time = Time.time
            });
        }

        public static void Text(Vector3 position, string text, Color color)
        {
            Label.Draw(new LabelInfo()
            {
                Position = position,
                Center = false,
                Text = text,
                Color = color,
                Size = ShapeCommon.TextSize,
                Rotation = ShapeCommon.TextRotation
            });
        }
        
        public static void Triangle(Vector3 position, float size, float angle, Color color)
        {
            Polygon.Draw(new PolygonInfo()
            {
                Sides = 3,
                Center = position,
                Size = size,
                Color = color.ToAlpha(ShapeCommon.Alpha * color.a),
                Rotation = angle == 0 ? ShapeCommon.RectRotation : Quaternion.Euler(0, 0, angle),
                BorderColor = color,
                BorderWidth = ShapeCommon.CircleBorderWidth,
                Bordered = true,
            });
        }
        
        public static void TriangleOneShot(Vector3 position, float size, float angle, Color color, float duration = 0.25f)
        {
            if (!Application.isPlaying) return;
            
            _triangleOneShots.Enqueue(new TriangleOneShotInfo()
            {
                Position = position,
                Size = size,
                Angle = angle,
                Color =  color,
                Duration = duration,
                Time = Time.time
            });
        }

        public static void TextFormat(Vector3 position, string text, float arg0 = 0, float arg1 = 0, float arg2 = 0, float arg3 = 0, float arg4 = 0, float arg5 = 0, float arg6 = 0, float arg7 = 0)
        {
            TextFormat(position, text, Color.white, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        public static void TextFormat(Vector3 position, string text, Color color, float arg0 = 0, float arg1 = 0, float arg2 = 0, float arg3 = 0, float arg4 = 0, float arg5 = 0, float arg6 = 0, float arg7 = 0)
        {
            Label.DrawFormat(new LabelInfo()
            {
                Position = position,
                Center = false,
                Text = text,
                Color = color,
                Size = ShapeCommon.TextSize,
                Rotation = ShapeCommon.TextRotation
                
            }, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        
        // Hard debug line only visible in editor
        public static void DebugLines(Vector3[] lines, Color color)
        {
            for (var i = 0; i < lines.Length; i++)
                DebugLine(lines[i], lines[(i + 1) % lines.Length], color);
        }
        
        public static void DebugLine(Vector2 start, Vector2 end, Color color)
        {
            // Couldn't get `GL.Lines` to works in editor mode, was flickering...
            // Instead, we'll use `Debug.DrawLine` in editor and switch to `GL.Lines` while playing.
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                _debugLines.Enqueue(new DebugLineInfo()
                {
                    Start = start,
                    End = end,
                    Color = color.ToAlpha(ShapeCommon.LineAlpha),
                });
            }
#if UNITY_EDITOR
            else
            {
                Debug.DrawLine(start, end, color.ToAlpha(ShapeCommon.LineAlpha));
            }
#endif
        }
        
        private static void DebugLineInternal(Vector3 start, Vector3 end, Color color)
        {
            GL.Color(color);
            
            GL.Vertex(start);
            GL.Vertex(end);
        }

        public static void Lines(Vector3[] lines, Color color)
        {
            for (var i = 0; i < lines.Length; i++)
                Line(lines[i], lines[(i + 1) % lines.Length], color);
        }

        public static void Line(Vector3 start, Vector3 end, Color color, float width = 0.055f)
        {
            Shapes.Line.Draw(new LineInfo()
            {
                StartPos = start,
                EndPos = end,
                FillColor = color.ToAlpha(ShapeCommon.LineAlpha),
                Width = width,
                Hard = false,
                Forward = ShapeCommon.LineRotation,
                Bordered = false,
                BorderWidth = 0,
                BorderColor = Color.black,
                StartArrow = false,
                EndArrow = false,
                ArrowLength = 0f,
                ArrowWidth = 0f,
                Dashed =  false,
                DashLength = 0f,
                DistanceBetweenDashes = 0f
            });
        }
        
        public static void Arrow(Vector3 start, Vector3 end, Color color, float width = 0.055f)
        {
            Shapes.Line.Draw(new LineInfo()
            {
                StartPos = start,
                EndPos = end,
                FillColor = color.ToAlpha(ShapeCommon.LineAlpha * color.a),
                Width = width,
                Hard = false,
                Forward = ShapeCommon.LineRotation,
                Bordered = false,
                BorderWidth = 0,
                BorderColor = Color.black,
                StartArrow = false,
                EndArrow = true,
                ArrowLength = 0.5f,
                ArrowWidth = 0.25f,
                Dashed =  false,
                DashLength = 0f,
                DistanceBetweenDashes = 0f
            });
        }
        
        public static void ArrowOneShot(Vector3 start, Vector3 end, Color color, float width = 0.055f, float duration = 0.25f)
        {
            if (!Application.isPlaying) return;
            
            _arrowOneShots.Enqueue(new ArrowOneShotInfo()
            {
                Start = start,
                End = end,
                Width = width,
                Color =  color,
                Duration = duration,
                Time = Time.time
            });
        }
        
        public static void Rect(Vector2 start, Vector2 end, Color color)
        {
            var minX = start.x;
            var minY = start.y;
            var maxX = end.x;
            var maxY = end.y;
            
            var width = end.x - minX;
            var height = end.y - minY;
            
            if (width < 0)
            {
                width = -width;
                (minX, maxX) = (maxX, minX);
            }
            
            if (height < 0)
            {
                height = -height;
                (minY, maxY) = (maxY, minY);
            }

            RectInternal(new Rect(minX, minY, width, height), color);
        }
        
        public static void Rect(Vector2 center, Vector2 size, float angle, Color color)
        {
            RectInternal(new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y), color, angle);
        }
        
        public static void Rect(Rect rect, Color color)
        {
            if (rect.width < 0)
                (rect.xMin, rect.xMax) = (rect.xMax, rect.xMin);
            
            if (rect.height < 0)
                (rect.yMin, rect.yMax) = (rect.yMax, rect.yMin);
            
            RectInternal(rect, color);
        }
        
        public static void RectOneShot(Rect rect, Color color, float duration = 0.25f)
        {
            if (!Application.isPlaying) return;
            
            if (rect.width < 0)
                (rect.xMin, rect.xMax) = (rect.xMax, rect.xMin);
            
            if (rect.height < 0)
                (rect.yMin, rect.yMax) = (rect.yMax, rect.yMin);

            _rectOneShots.Enqueue(new RectOneShotInfo()
            {
                Rect = rect,
                Color =  color,
                Duration = duration,
                Time = Time.time
            });
        }

        private static void RectInternal(Rect rect, Color color, float angle = 0)
        {
            Quad.Draw(new QuadInfo()
            {
                Center = rect.center,
                Size = rect.size,
                Color = color.ToAlpha(ShapeCommon.Alpha * color.a),
                Rotation = angle == 0 ? ShapeCommon.RectRotation : Quaternion.Euler(0, 0, angle),
                BorderColor = color,
                BorderWidth = ShapeCommon.RectBorderWidth,
                Bordered = true,
            });
        }
    }
}