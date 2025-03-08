using System;
using System.Collections.Generic;
using UnityEngine;

namespace JD.Shapes
{
    [Serializable]
    public struct LabelInfo
    {
        public string Text;
        public float Size;
        
        public Vector3 Position;
        public bool Center;

        public Color Color;

        public Quaternion Rotation;
    }

    public static class Label
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            AntiAliasingSmoothing = 1.5f;
            _mesh = null;
            _hasMesh = false;
            _material = null;
            _hasMaterial = false;
            _texture = null;
            _hasTexture = false;
            _materialPropertyBlock = null;
        }
        
        public static float AntiAliasingSmoothing = 1.5f;

        public const char UnicodeDegree     = '\u00b0'; // °
        public const char UnicodeAlpha      = '\u03b1'; // α
        public const char UnicodeDelta      = '\u03b4'; // δ
        public const char UnicodeTriangleup = '\u25b2'; // ▲

        public static string FormatFloat(float x) => $"{( x >= 0 ? "+" : "")}{x:N2}";
        public static string FormatDegrees(float x) => FormatFloat(x) + UnicodeDegree;

        public static string WinAltChars = " ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";

        private const string FillColorParam = "_FillColor";
        private const string LabelTextParam = "_LabelTex";
        private const string IndexParam = "_index";

        private static readonly int _fillColor = Shader.PropertyToID(FillColorParam);
        private static readonly int _labelTex = Shader.PropertyToID(LabelTextParam);
        private static readonly int _index = Shader.PropertyToID(IndexParam);
        
        private static Mesh _mesh;
        private static bool _hasMesh;
        private static Material _material;
        private static bool _hasMaterial;
        private static Texture2D _texture;
        private static bool _hasTexture;
        private static MaterialPropertyBlock _materialPropertyBlock;

        private static Mesh CreateMesh()
        {
            var mesh = new Mesh();

            mesh.SetVertices(new Vector3[]
            {
                new Vector3( -1, -1,  0 ),
                new Vector3(  1, -1,  0 ),
                new Vector3(  1,  1,  0 ),
                new Vector3( -1,  1,  0 )
            });

            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

            mesh.uv = new[]
            {
                new Vector2( -1, -1 ),
                new Vector2(  1, -1 ),
                new Vector2(  1,  1 ),
                new Vector2( -1,  1 )
            };

            return mesh;
        }

        private static MaterialPropertyBlock GetMaterialPropertyBlock(Color color)
        {
            if (_materialPropertyBlock == null)
                _materialPropertyBlock = new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(_fillColor, color);
            
            return _materialPropertyBlock;
        }

        private static Texture2D GetTexture()
        {
#if UNITY_EDITOR
            if (_texture != null)      
#else
            if (_hasTexture)
#endif
                return _texture;
            
            _texture = Resources.Load<Texture2D>("Shapes/unifont_8x16");
            _hasTexture = true;
            
            Debug.Log($"Text: {_texture == null}");
            
            return _texture;
        }

        private static Material GetMaterial()
        {
#if UNITY_EDITOR
            if (_material != null)
#else
            if (_hasMaterial)
#endif
                return _material;

            var mat = new Material(Shader.Find("Hidden/Shapes/Label"));
            if (SystemInfo.supportsInstancing)
                mat.enableInstancing = true;

            Shader.SetGlobalTexture(_labelTex, GetTexture());

            _material = mat;
            _hasMaterial = true;
            
            return mat;
        }

        private static Mesh GetMesh()
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

        public static void Draw(LabelInfo info)
        {
            DrawPart(info.Position, info.Text, info.Color, info.Size, info.Center, info.Rotation);
        }

        private static void DrawPart(Vector3 position, string label, Color color, float size = 0.1f, bool center = true)
        {
            DrawPart(position, label, color, size, center, Quaternion.identity);
        }

        private static void DrawPart(Vector3 position, string label, Color color, float size, bool center, Quaternion rotation)
        {
            if (label == null || string.IsNullOrEmpty(label.Trim())) return;
            while (label.Length < 3) label += ' ';
            
            // Loop in group of 3 letters
            if (label.Length > 3)
            {
                var count = Mathf.CeilToInt((label.Length - 1) / 3);
                var step = (3 / 4f) * size;
                var stepSize = rotation * ( step * Vector3.right );

                if (center)
                {
                    DrawPart(position + stepSize * ( count * ( 1 ) ), label.Substring(0, 3) , color, size, true, rotation);
                    DrawPart(position - stepSize * ( 1 ), label.Substring( 3 ), color , size, true, rotation);
                }
                else
                {
                    DrawPart(position , label.Substring(0, 3), color, size, false, rotation);
                    DrawPart(position - stepSize * 2f , label.Substring(3), color, size, false, rotation);
                }
                return;
            }
            
            var mesh = GetMesh();
            var material = GetMaterial();
            var materialPropertyBlock = GetMaterialPropertyBlock(color);
            
            var data = 0;
            for (var i = 0; i < 3; ++i) data |= ((WinAltChars.IndexOf(label[i]) + 1) << (8 * i));

            materialPropertyBlock.SetColor(_fillColor, color);
            materialPropertyBlock.SetInt(_index, data );

            var scale = new Vector3( 3f/4f, 0.5f, 0 ) * size;
            var matrix = Matrix4x4.TRS(position, rotation, scale); // Quaternion.LookRotation(Common.normal)

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
        }
    }
}