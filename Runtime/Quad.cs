using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace JD.Shapes
{
    [Serializable]
    public struct QuadInfo
    {
        public int Sides;
        public Vector3 Center;
        public Vector2 Size;

        public Color Color;

        public bool Bordered;
        public float BorderWidth;
        public Color BorderColor;

        public Quaternion Rotation;
    }

    public static class Quad
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            AntiAliasingSmoothing = 1.5f;
            _mesh = null;
            _hasMesh = false;
            _materialPropertyBlock = null;
            for (var i = 0; i < _hasMaterials.Length; i++)
            {
                _materials[i] = null;
                _hasMaterials[i] = false;
            }
        }
        
        public static float AntiAliasingSmoothing = 1.5f;

        private const string BorderColorKeyword = "BORDER";

        private const string FillColorParam = "_FillColor";
        private const string AASmoothingParam = "_AASmoothing";
        private const string BorderColorParam = "_BorderColor";
        private const string FillWidthParam = "_FillWidth";
        private const string FillHeightParam = "_FillHeight";

        private static readonly int _fillColor = Shader.PropertyToID(FillColorParam);
        private static readonly int _aaSmoothing = Shader.PropertyToID(AASmoothingParam);
        private static readonly int _borderColor = Shader.PropertyToID(BorderColorParam);
        private static readonly int _fillWidth = Shader.PropertyToID(FillWidthParam);
        private static readonly int _fillHeight = Shader.PropertyToID(FillHeightParam);
        
        private static Mesh _mesh;
        private static bool _hasMesh;
        private static MaterialPropertyBlock _materialPropertyBlock;
        private static readonly Material[] _materials = new Material[4];
        private static readonly bool[] _hasMaterials = new bool[4];

        private static readonly string[][] _materialKeywords = new string[][]
        {
            null,
            new[] { BorderColorKeyword },
        };

        private static Mesh CreateMesh()
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

        private static MaterialPropertyBlock GetMaterialPropertyBlock(QuadInfo info)
        {
            if (_materialPropertyBlock == null)
                _materialPropertyBlock = new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(_fillColor, info.Color);
            _materialPropertyBlock.SetFloat(_aaSmoothing, AntiAliasingSmoothing);

            if (info.Bordered)
            {
                _materialPropertyBlock.SetColor(_borderColor, info.BorderColor);
                _materialPropertyBlock.SetFloat(_fillWidth, 2 * info.BorderWidth / info.Size.x);
                _materialPropertyBlock.SetFloat(_fillHeight, 2 * info.BorderWidth / info.Size.x * (info.Size.y / info.Size.x));
            }

            return _materialPropertyBlock;
        }

        private static Material GetMaterial(QuadInfo info)
        {
            var materialIndex = 0;
            if (info.Bordered)
                materialIndex = 1;

#if UNITY_EDITOR
            if (_materials[materialIndex] != null)      
#else
            if (_hasMaterials[materialIndex])
#endif
                return _materials[materialIndex];

            var mat = new Material(Shader.Find("Hidden/Shapes/Rect"));
            if (SystemInfo.supportsInstancing)
                mat.enableInstancing = true;

            var keywords = _materialKeywords[materialIndex];
            if (keywords != null)
                mat.shaderKeywords = keywords;

            _materials[materialIndex] = mat;
            _hasMaterials[materialIndex] = true;
            
            return mat;
        }

        private static Mesh GetQuadMesh()
        {
#if UNITY_EDITOR
            if (_mesh != null)
#else
            if (_hasMesh)
#endif
                return _mesh;

            _mesh = CreateMesh();
            _hasMesh = true;
            
            return _mesh;
        }

        public static void Draw(QuadInfo info)
        {
            var mesh = GetQuadMesh();

            var rotation = info.Rotation;
            var matrix = Matrix4x4.TRS(info.Center, rotation, new Vector3(info.Size.x / 2f, info.Size.y / 2f, 1f));

            var materialPropertyBlock = GetMaterialPropertyBlock(info);
            var material = GetMaterial(info);

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
        }
    }
}