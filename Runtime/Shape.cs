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
        
        internal static void OnRender()
        {
            if (_debugLineQueue.Count == 0) return;
            
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
            
            while (_debugLineQueue.TryDequeue(out var info))
                DebugLineInternal(info.Start, info.End, info.Color);
            
            GL.End();
            GL.PopMatrix();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _debugLineQueue?.Clear();
            _lineMaterial = null;
            _hasLineMaterial = false;
            _root = null;
            _hasRoot = false;
        }
        
        private static Material _lineMaterial;
        private static bool _hasLineMaterial;

        private static ShapeRoot _root;
        private static bool _hasRoot;
        
        private static readonly Queue<DebugLineInfo> _debugLineQueue = new Queue<DebugLineInfo>(100);
        
        public static void Circle(Vector3 position, float radius, Color color)
        {
            Shapes.Circle.Draw(new CircleInfo{
                Center = position,
                Forward = ShapeCommon.CircleRotation,
                Radius = radius,
                FillColor = color.ToAlpha(ShapeCommon.Alpha),
                BorderColor = color,
                BorderWidth = ShapeCommon.CircleBorderWidth,
                Bordered = true,
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
                _debugLineQueue.Enqueue(new DebugLineInfo()
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

        public static void Line(Vector3 start, Vector3 end, Color color)
        {
            Shapes.Line.Draw(new LineInfo()
            {
                StartPos = start,
                EndPos = end,
                FillColor = color.ToAlpha(ShapeCommon.LineAlpha),
                Width = 0.1f,
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

            Quad.Draw(new QuadInfo()
            {
                Center = new Vector3(minX + width / 2f, minY + height / 2f),
                Size = new Vector2(width, height),
                Color = color.ToAlpha(ShapeCommon.Alpha),
                Rotation = ShapeCommon.RectRotation,
                BorderColor = color,
                BorderWidth = ShapeCommon.RectBorderWidth,
                Bordered = true,
            });
        }
        
        public static void Rect(Rect rect, Color color)
        {
            if (rect.width < 0)
                (rect.xMin, rect.xMax) = (rect.xMax, rect.xMin);
            
            if (rect.height < 0)
                (rect.yMin, rect.yMax) = (rect.yMax, rect.yMin);
            
            Quad.Draw(new QuadInfo()
            {
                Center = rect.center,
                Size = rect.size,
                Color = color.ToAlpha(ShapeCommon.Alpha),
                Rotation = ShapeCommon.RectRotation,
                BorderColor = color,
                BorderWidth = ShapeCommon.RectBorderWidth,
                Bordered = true,
            });
        }
    }
}