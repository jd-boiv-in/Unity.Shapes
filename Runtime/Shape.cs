﻿using System.Collections.Generic;
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

            RectInternal(new Rect(minX, minY, width, height), color);
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

        private static void RectInternal(Rect rect, Color color)
        {
            Quad.Draw(new QuadInfo()
            {
                Center = rect.center,
                Size = rect.size,
                Color = color.ToAlpha(ShapeCommon.Alpha * color.a),
                Rotation = ShapeCommon.RectRotation,
                BorderColor = color,
                BorderWidth = ShapeCommon.RectBorderWidth,
                Bordered = true,
            });
        }
    }
}