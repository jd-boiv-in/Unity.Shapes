using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shapes
{
    [Serializable]
    public struct CircleInfo
    {
        public float Radius;
        public Vector3 Center;
        public Vector3 Forward;

        public Color FillColor;

        public bool Bordered;
        public Color BorderColor;
        public float BorderWidth;

        public bool IsSector;
        public float SectorInitialAngleInDegrees;
        public float SectorArcLengthInDegrees;
    }

    public static class Circle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            AntiAliasingSmoothing = 1.5f;
            _quadMesh = null;
            _hasQuadMesh = false;
            _materialPropertyBlock = null;
            for (var i = 0; i < _hasMaterials.Length; i++)
            {
                _materials[i] = null;
                _hasMaterials[i] = false;
            }
        }
        
        public static float AntiAliasingSmoothing = 1.5f;

        private const string BorderColorKeyword = "BORDER";
        private const string SectorKeyword = "SECTOR";

        private const string FillColorParam = "_FillColor";
        private const string BorderColorParam = "_BorderColor";
        private const string FillWidthParam = "_FillWidth";
        private const string AASmoothingParam = "_AASmoothing";
        private const string SectorPlaneNormal1 = "_cutPlaneNormal1";
        private const string SectorPlaneNormal2 = "_cutPlaneNormal2";
        private const string SectorAngleBlendMode = "_AngleBlend";
        
        private static readonly int _fillColor = Shader.PropertyToID(FillColorParam);
        private static readonly int _aaSmoothing = Shader.PropertyToID(AASmoothingParam);
        private static readonly int _borderColor = Shader.PropertyToID(BorderColorParam);
        private static readonly int _fillWidth = Shader.PropertyToID(FillWidthParam);
        private static readonly int _cutPlaneNormal1 = Shader.PropertyToID(SectorPlaneNormal1);
        private static readonly int _cutPlaneNormal2 = Shader.PropertyToID(SectorPlaneNormal2);
        private static readonly int _angleBlend = Shader.PropertyToID(SectorAngleBlendMode);
        
        private static Mesh _quadMesh;
        private static bool _hasQuadMesh;
        private static MaterialPropertyBlock _materialPropertyBlock;
        private static readonly Material[] _materials = new Material[4];
        private static readonly bool[] _hasMaterials = new bool[4];

        private static readonly string[][] _materialKeywords = new string[][]
        {
            null,
            new[] { BorderColorKeyword },
            new[] { SectorKeyword },
            new[] { BorderColorKeyword, SectorKeyword },
        };

        private static Material GetMaterial(CircleInfo circleInfo)
        {
            var materialIndex = 0;

            if (circleInfo.Bordered)
                materialIndex = 1;

            if (circleInfo.IsSector)
                materialIndex = 2;

            if (circleInfo.Bordered && circleInfo.IsSector)
                materialIndex = 3;

#if UNITY_EDITOR
            if (_materials[materialIndex] != null)
#else
            if (_hasMaterials[materialIndex])
#endif
            {
                return _materials[materialIndex];
            }

            var mat = new Material(Shader.Find("Hidden/Shapes/Circle"));
            if (SystemInfo.supportsInstancing)
                mat.enableInstancing = true;

            var keywords = _materialKeywords[materialIndex];
            if (keywords != null)
                mat.shaderKeywords = keywords;

            _materials[materialIndex] = mat;
            _hasMaterials[materialIndex] = true;

            return mat;
        }

        private static MaterialPropertyBlock GetMaterialPropertyBlock(CircleInfo circleInfo)
        {
            if (_materialPropertyBlock == null)
                _materialPropertyBlock = new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(_fillColor, circleInfo.FillColor);
            _materialPropertyBlock.SetFloat(_aaSmoothing, AntiAliasingSmoothing);

            if (circleInfo.Bordered)
            {
                _materialPropertyBlock.SetColor(_borderColor, circleInfo.BorderColor);
                var borderWidthNormalized = circleInfo.BorderWidth / circleInfo.Radius;
                _materialPropertyBlock.SetFloat(_fillWidth, 1.0f - borderWidthNormalized);
            }

            if (circleInfo.IsSector)
            {
                SetSectorAngles(_materialPropertyBlock, circleInfo.SectorInitialAngleInDegrees,
                    circleInfo.SectorArcLengthInDegrees);
            }

            return _materialPropertyBlock;
        }

        private static Mesh GetCircleMesh()
        {
#if UNITY_EDITOR
            if (_quadMesh != null)
#else
            if (_hasQuadMesh)
#endif
                return _quadMesh;

            _quadMesh = CreateQuadMesh();
            _hasQuadMesh = true;
            
            return _quadMesh;
        }

        private static Matrix4x4 GetTRSMatrix(CircleInfo circleInfo)
        {
            var rotation = Quaternion.LookRotation(circleInfo.Forward);
            return Matrix4x4.TRS(circleInfo.Center, rotation, new Vector3(circleInfo.Radius, circleInfo.Radius, 1f));
        }

        private static void SetSectorAngles(MaterialPropertyBlock block, float initialAngleDegrees, float sectorArcLengthDegrees)
        {
            var initialAngleRadians = Mathf.Deg2Rad * initialAngleDegrees;
            var finalAngleRadians = initialAngleRadians + (Mathf.Deg2Rad * sectorArcLengthDegrees);

            var cutPlaneNormal1 = new Vector2(Mathf.Sin(initialAngleRadians), -Mathf.Cos(initialAngleRadians));
            var cutPlaneNormal2 = new Vector2(-Mathf.Sin(finalAngleRadians), Mathf.Cos(finalAngleRadians));

            block.SetVector(_cutPlaneNormal1, cutPlaneNormal1);
            block.SetVector(_cutPlaneNormal2, cutPlaneNormal2);
            block.SetFloat(_angleBlend, sectorArcLengthDegrees < 180f ? 0f : 1f);
        }

        private static Mesh CreateQuadMesh()
        {
            var quadMesh = new Mesh();
            quadMesh.SetVertices(new List<Vector3>
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-1f, 1f, 0f)
            });

            quadMesh.triangles = new[]
            {
                0, 2, 1,
                0, 3, 2
            };

            const float uvMin = -1f;
            const float uvMax = 1f;

            quadMesh.uv = new[]
            {
                new Vector2(uvMin, uvMin),
                new Vector2(uvMax, uvMin),
                new Vector2(uvMax, uvMax),
                new Vector2(uvMin, uvMax)
            };

            return quadMesh;
        }
        
        public static void Draw(CircleInfo circleInfo)
        {
            var mesh = GetCircleMesh();
            var materialPropertyBlock = GetMaterialPropertyBlock(circleInfo);
            var matrix = GetTRSMatrix(circleInfo);
            var material = GetMaterial(circleInfo);

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
        }
    }
}