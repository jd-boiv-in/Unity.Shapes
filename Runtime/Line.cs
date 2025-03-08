using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shapes
{
    [Serializable]
    public struct LineInfo
    {
        public Vector3 StartPos;
        public Vector3 EndPos;
        public Color FillColor;
        public Vector3 Forward;
        public float Width;

        public bool Hard;

        public bool Bordered;
        public Color BorderColor;
        public float BorderWidth;

        public bool Dashed;
        public float DistanceBetweenDashes;
        public float DashLength;

        public bool StartArrow;
        public bool EndArrow;

        public float ArrowWidth;
        public float ArrowLength;
    }

    public static class Line
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            AntiAliasingSmoothing = 1.5f;
            _lineSegmentMesh = null;
            _hasLineSegmentMesh = false;
            _arrowHeadMesh = null;
            _hasArrowHeadMesh = false;
            _arrowHeadMaterial = null;
            _hasArrowHeadMaterial = false;
            _materialPropertyBlock = null;
            for (var i = 0; i < _lineMaterials.Length; i++)
            {
                _lineMaterials[i] = null;
                _hasLineMaterials[i] = false;
            }
        }
        
        public static float AntiAliasingSmoothing = 1.5f;
        
        private const string BorderColorKeyword = "BORDER";
        private const string DashedKeyword = "DASHED";
        private const string VerticalEdgeKeyword = "VERTICAL_EDGE_SMOOTH_OFF";

        private const string ColorParam = "_Color";
        private const string AntiAliasingSmoothingParam = "_AASmoothing";
        private const string HardParam = "_Hard";
        private const string FillWidthParam = "_FillWidth";
        private const string BorderColorParam = "_BorderColor";
        private const string LineLengthParam = "_LineLength";
        private const string DistanceBetweenDashesParam = "_DistanceBetweenDashes";
        private const string DashWidthParam = "_DashWidth";
        private const string ArrowHeadBaseEdgeNoAAMinX = "_BaseEdgeNoAAMinX";
        private const string ArrowHeadBaseEdgeNoAAMaxX = "_BaseEdgeNoAAMaxX";
        
        private static readonly int _color = Shader.PropertyToID(ColorParam);
        private static readonly int _aaSmoothing = Shader.PropertyToID(AntiAliasingSmoothingParam);
        private static readonly int _hard = Shader.PropertyToID(HardParam);
        private static readonly int _borderColor = Shader.PropertyToID(BorderColorParam);
        private static readonly int _fillWidth = Shader.PropertyToID(FillWidthParam);
        private static readonly int _lineLength = Shader.PropertyToID(LineLengthParam);
        private static readonly int _distanceBetweenDashes = Shader.PropertyToID(DistanceBetweenDashesParam);
        private static readonly int _dashWidth = Shader.PropertyToID(DashWidthParam);
        private static readonly int _baseEdgeNoAAMinX = Shader.PropertyToID(ArrowHeadBaseEdgeNoAAMinX);
        private static readonly int _baseEdgeNoAAMaxX = Shader.PropertyToID(ArrowHeadBaseEdgeNoAAMaxX);
        
        private static Mesh _lineSegmentMesh;
        private static bool _hasLineSegmentMesh;
        private static Mesh _arrowHeadMesh;
        private static bool _hasArrowHeadMesh;
        private static Material _arrowHeadMaterial;
        private static bool _hasArrowHeadMaterial;
        private static MaterialPropertyBlock _materialPropertyBlock;
        private static readonly Matrix4x4 _cacheMatrix = Matrix4x4.identity;
        private static readonly Material[] _lineMaterials = new Material[9];
        private static readonly bool[] _hasLineMaterials = new bool[9];

        private static readonly string[][] _materialKeywords = new string[][]
        {
            null,
            new[] { BorderColorKeyword },
            new[] { DashedKeyword },
            new[] { BorderColorKeyword, DashedKeyword },
            new[] { VerticalEdgeKeyword },
            new[] { BorderColorKeyword, VerticalEdgeKeyword },
            new[] { DashedKeyword, VerticalEdgeKeyword },
            new[] { BorderColorKeyword, DashedKeyword, VerticalEdgeKeyword },
        };

        private static Material GetLineMaterial(LineInfo lineInfo)
        {
            var materialIndex = 0;
            var hasArrows = lineInfo.StartArrow || lineInfo.EndArrow;

            if (lineInfo.Bordered)
                materialIndex = 1;

            if (lineInfo.Dashed)
                materialIndex = 2;

            if (lineInfo.Bordered && lineInfo.Dashed)
                materialIndex = 3;

            if (hasArrows)
                materialIndex = 4;

            if (hasArrows && lineInfo.Bordered)
                materialIndex = 5;

            if (hasArrows && lineInfo.Dashed)
                materialIndex = 6;

            if (hasArrows && lineInfo.Bordered && lineInfo.Dashed)
                materialIndex = 7;

#if UNITY_EDITOR
            if (_lineMaterials[materialIndex] == null)
#else
            if (!_hasLineMaterials[materialIndex])
#endif
            {
                var material = new Material(Shader.Find("Hidden/Shapes/Line"));
                if (SystemInfo.supportsInstancing)
                    material.enableInstancing = true;

                if (_materialKeywords[materialIndex] != null)
                    material.shaderKeywords = _materialKeywords[materialIndex];

                _lineMaterials[materialIndex] = material;
                _hasLineMaterials[materialIndex] = true;
                return material;
            }

            return _lineMaterials[materialIndex];
        }

        private static Mesh GetLineMesh(LineInfo lineInfo)
        {
#if UNITY_EDITOR
            if (_lineSegmentMesh == null)
#else
            if (!_hasLineSegmentMesh)
#endif
            {
                _lineSegmentMesh = CreateLineSegmentMesh();
                _hasLineSegmentMesh = true;
            }

            return _lineSegmentMesh;
        }

        private static MaterialPropertyBlock GetMaterialPropertyBlock(LineInfo lineInfo)
        {
            if (_materialPropertyBlock == null)
                _materialPropertyBlock = new MaterialPropertyBlock();
            else
                _materialPropertyBlock.Clear();

            return _materialPropertyBlock;
        }

        private static Material GetArrowHeadMaterial()
        {
#if UNITY_EDITOR
            if (_arrowHeadMaterial == null)
#else
            if (!_hasArrowHeadMaterial)
#endif
            {
                _arrowHeadMaterial = new Material(Shader.Find("Hidden/Shapes/ArrowHead"));
                _hasArrowHeadMaterial = true;
            }

            return _arrowHeadMaterial;
        }

        private static Mesh GetArrowHeadMesh()
        {
#if UNITY_EDITOR
            if (_arrowHeadMesh == null)
#else
            if (!_hasArrowHeadMesh)
#endif
            {
                _arrowHeadMesh = CreateArrowHeadMesh();
                _hasArrowHeadMesh = true;
            }

            return _arrowHeadMesh;
        }

        private static Mesh CreateArrowHeadMesh()
        {
            var quadMesh = new Mesh();
            
            quadMesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, 0.0f, 0f),
                new Vector3(0.5f, 0.0f, 0f),
                new Vector3(0.0f, 1.0f, 0f),
            });

            quadMesh.triangles = new[]
            {
                0, 1, 2,
            };

            quadMesh.uv = new[]
            {
                new Vector2(-0.5f, 0.0f),
                new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 1.0f),
            };

            quadMesh.colors32 = new[]
            {
                new Color32(Byte.MaxValue, 0, 0, 0),
                new Color32(0, Byte.MaxValue, 0, 0),
                new Color32(0, 0, Byte.MaxValue, 0),
            };

            return quadMesh;
        }

        private static void FillPropertyBlock(MaterialPropertyBlock block, LineInfo lineInfo, float lineSegmentLength)
        {
            block.SetColor(_color, lineInfo.FillColor);
            block.SetFloat(_aaSmoothing, AntiAliasingSmoothing);
            block.SetFloat(_hard, lineInfo.Hard ? 1 : 0);

            if (lineInfo.Bordered)
            {
                block.SetColor(_borderColor, lineInfo.BorderColor);
                var borderWidthNormalized = lineInfo.BorderWidth / lineInfo.Width;
                block.SetFloat(_fillWidth, 0.5f - borderWidthNormalized);
            }

            if (lineInfo.Dashed)
            {
                block.SetFloat(_lineLength, lineSegmentLength);
                block.SetFloat(_distanceBetweenDashes, lineInfo.DistanceBetweenDashes);
                block.SetFloat(_dashWidth, lineInfo.DashLength);
            }

            if (lineInfo.StartArrow || lineInfo.EndArrow)
            {
                var baseEdgeNoAAMaxX = (lineInfo.Width / lineInfo.ArrowWidth) * 0.5f;
                var baseEdgeNoAAMinX = -baseEdgeNoAAMaxX;
                block.SetFloat(_baseEdgeNoAAMinX, baseEdgeNoAAMinX);
                block.SetFloat(_baseEdgeNoAAMaxX, baseEdgeNoAAMaxX);
            }
        }

        private static Mesh CreateLineSegmentMesh()
        {
            var quadMesh = new Mesh();

            var xLeft = -0.5f;
            var xCenter = 0f;
            var xRight = 0.5f;

            var yBottom = 0f;
            var yTop = 1f;

            quadMesh.SetVertices(new List<Vector3>
            {
                new Vector3(xLeft, yBottom, 0f),
                new Vector3(xCenter, yBottom, 0f),
                new Vector3(xRight, yBottom, 0f),

                new Vector3(xLeft, yTop, 0f),
                new Vector3(xCenter, yTop, 0f),
                new Vector3(xRight, yTop, 0f),
            });

            quadMesh.triangles = new[]
            {
                0, 1, 4,
                4, 3, 0,

                1, 2, 5,
                5, 4, 1,
            };

            var uvXLeft = 0.5f;
            var uvXCenter = 0f;
            var uvXRight = 0.5f;

            var uvYBottom = 0f;
            var uvYTop = 1f;

            quadMesh.uv = new[]
            {
                new Vector2(uvXLeft, uvYBottom),
                new Vector2(uvXCenter, uvYBottom),
                new Vector2(uvXRight, uvYBottom),
                new Vector2(uvXLeft, uvYTop),
                new Vector2(uvXCenter, uvYTop),
                new Vector2(uvXRight, uvYTop)
            };

            return quadMesh;
        }

        private static Matrix4x4 GetLineTRSMatrix(Vector3 startPos, Vector3 endPos, Vector3 forward, float width, out float lineLength)
        {
            lineLength = Vector3.Distance(endPos, startPos);

            var up = (endPos - startPos).normalized;
            forward = forward - Vector3.Dot(forward, up) * up;
            forward.Normalize();

            var right = Vector3.Cross(up, forward);
            right.Normalize();

            var mat = _cacheMatrix;

            // Orthonormal basis
            mat.SetColumn(0, right * width); //equivalent to mat.SetColumn(0,right) followed by a mat *= Matrix4x4.Scale(new Vector3(width, 1f, 1f));
            mat.SetColumn(1,up * lineLength); //equivalent to mat.SetColumn(1,up) followed by a mat *= Matrix4x4.Scale(new Vector3(1f, lineLength, 1f));
            mat.SetColumn(2, forward);

            // Origin translation
            Vector4 translation = startPos;
            translation.w = 1f;
            mat.SetColumn(3, translation);

            return mat;
        }

        private static void DrawArrowHead(Vector3 startPos, Vector3 endPos, Vector3 forward, float width, MaterialPropertyBlock materialPropertyBlock)
        {
            // TODO: Reuse matrix from line
            var matrix = GetLineTRSMatrix(startPos, endPos, forward, width, out var arrowHeadHeight);

            var arrowHeadMesh = GetArrowHeadMesh();
            var arrowHeadMaterial = GetArrowHeadMaterial();

            Graphics.DrawMesh(arrowHeadMesh, matrix, arrowHeadMaterial, 0, null, 0, materialPropertyBlock);
        }

        public static void Draw(LineInfo lineInfo)
        {
            var lineMaterial = GetLineMaterial(lineInfo);
            var lineSegmentMesh = GetLineMesh(lineInfo);
            var materialPropertyBlock = GetMaterialPropertyBlock(lineInfo);

            var lineSegmentStartPos = lineInfo.StartPos;
            var lineSegmentEndPos = lineInfo.EndPos;

            var lineDirection = (lineSegmentEndPos - lineSegmentStartPos).normalized;

            if (lineInfo.StartArrow)
                lineSegmentStartPos = lineSegmentStartPos + lineDirection * lineInfo.ArrowLength;

            if (lineInfo.EndArrow)
                lineSegmentEndPos = lineSegmentEndPos - lineDirection * lineInfo.ArrowLength;

            var forward = lineInfo.Forward;
            var width = lineInfo.Width;
            var matrix = GetLineTRSMatrix(lineSegmentStartPos, lineSegmentEndPos, forward, width, out var lineLength);

            FillPropertyBlock(materialPropertyBlock, lineInfo, lineLength);

            Graphics.DrawMesh(lineSegmentMesh, matrix, lineMaterial, 0, null, 0, materialPropertyBlock);

            if (lineInfo.EndArrow)
                DrawArrowHead(lineSegmentEndPos, lineInfo.EndPos, forward, lineInfo.ArrowWidth, materialPropertyBlock);

            if (lineInfo.StartArrow)
                DrawArrowHead(lineSegmentStartPos, lineInfo.StartPos, forward, lineInfo.ArrowWidth, materialPropertyBlock);
        }
    }
}