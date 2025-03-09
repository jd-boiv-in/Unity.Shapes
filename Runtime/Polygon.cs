using System;
using System.Collections.Generic;
using UnityEngine;

namespace JD.Shapes
{
    [Serializable]
    public struct PolygonInfo
    {
        public int Sides;
        public Vector3 Center;
        public float Size;

        public Color Color;

        public bool Bordered;
        public float BorderWidth;
        public Color BorderColor;

        public Quaternion Rotation;
    }

    public static class Polygon
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            AntiAliasingSmoothing = 1.5f;
            _materialPropertyBlock = null;
            
            for (var i = 0; i < _hasMaterials.Length; i++)
            {
                _materials[i] = null;
                _hasMaterials[i] = false;
            }
            
            for (var i = 0; i < _hasMeshes.Length; i++)
            {
                _meshes[i] = null;
                _hasMeshes[i] = false;
            }
        }
        
        public static float AntiAliasingSmoothing = 1.5f;
        private const int CachedMesh = 20;

        private const string BorderColorKeyword = "BORDER";

        private const string FillColorParam = "_FillColor";
        private const string AASmoothingParam = "_AASmoothing";
        private const string BorderColorParam = "_BorderColor";
        private const string FillWidthParam = "_FillWidth";

        private static readonly int _fillColor = Shader.PropertyToID(FillColorParam);
        private static readonly int _aaSmoothing = Shader.PropertyToID(AASmoothingParam);
        private static readonly int _borderColor = Shader.PropertyToID(BorderColorParam);
        private static readonly int _fillWidth = Shader.PropertyToID(FillWidthParam);
        
        private static MaterialPropertyBlock _materialPropertyBlock;
        private static readonly Material[] _materials = new Material[4];
        private static readonly bool[] _hasMaterials = new bool[4];
        private static readonly Mesh[] _meshes = new Mesh[CachedMesh];
        private static readonly bool[] _hasMeshes = new bool[CachedMesh];

        private static readonly string[][] _materialKeywords = new string[][]
        {
            null,
            new[] { BorderColorKeyword },
        };

        private static Mesh GenerateMeshPolygon(PolygonInfo info)
        {
            var id = info.Sides - 3;
            if (id < CachedMesh)
            {
#if UNITY_EDITOR
                if (_meshes[id] != null)
#else
                if (_hasMeshes[id])
#endif
                    return _meshes[id];
            } 
            
            var polygonMesh = new Mesh();
            var vertices = new List<Vector3> { Vector3.zero };

            var angleIncrement = (Mathf.PI * 2) / info.Sides;
            var offset = Mathf.PI / 4f;

            for (var currentAngle = 0f; currentAngle < (Mathf.PI * 2); currentAngle += angleIncrement)
            {
                var x = Mathf.Cos(currentAngle + offset);
                var y = Mathf.Sin(currentAngle + offset);
                vertices.Add(new Vector3(x, y, 0f));
            }

            var triangles = new int[(vertices.Count - 1) * 3];
            var triangleIndex = 0;
            for (var vertexIndex = 1; vertexIndex < vertices.Count; vertexIndex++)
            {
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex;
                triangles[triangleIndex + 2] = vertexIndex + 1;

                if ((vertexIndex + 1) >= vertices.Count)
                    triangles[triangleIndex + 2] = 1;

                triangleIndex += 3;
            }

            polygonMesh.SetVertices(vertices);
            polygonMesh.triangles = triangles;

            var uv = new Vector2[vertices.Count];
            for (var uvIndex = 1; uvIndex < uv.Length; uvIndex++)
            {
                uv[uvIndex] = Vector2.one;
            }

            polygonMesh.uv = uv;

            if (id < CachedMesh)
            {
                _hasMeshes[id] = true;
                _meshes[id] = polygonMesh;
            }
            
            return polygonMesh;
        }

        private static MaterialPropertyBlock GetMaterialPropertyBlock(PolygonInfo info)
        {
            if (_materialPropertyBlock == null)
                _materialPropertyBlock = new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(_fillColor, info.Color);
            _materialPropertyBlock.SetFloat(_aaSmoothing, AntiAliasingSmoothing);

            if (info.Bordered)
            {
                _materialPropertyBlock.SetColor(_borderColor, info.BorderColor);
                var borderWidthNormalized = info.BorderWidth / info.Size;
                _materialPropertyBlock.SetFloat(_fillWidth, 1.0f - borderWidthNormalized);
            }

            return _materialPropertyBlock;
        }

        private static Material GetMaterial(PolygonInfo info)
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

            var mat = new Material(Shader.Find("Shapes/Polygon"));
            if (SystemInfo.supportsInstancing)
                mat.enableInstancing = true;

            var keywords = _materialKeywords[materialIndex];
            if (keywords != null)
                mat.shaderKeywords = keywords;
            
            _materials[materialIndex] = mat;
            _hasMaterials[materialIndex] = true;

            return mat;
        }

        public static void Draw(PolygonInfo info)
        {
            if (info.Sides < 2)
                throw new ArgumentException("Polygon must have at least 3 sides");

            var mesh = GenerateMeshPolygon(info);

            var rotation = info.Rotation;
            var matrix = Matrix4x4.TRS(info.Center, rotation, new Vector3(info.Size, info.Size, 1f));

            var materialPropertyBlock = GetMaterialPropertyBlock(info);
            var material = GetMaterial(info);

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
        }
    }
}