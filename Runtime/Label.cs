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
            _charsIndex = null;
        }
        
        public static float AntiAliasingSmoothing = 1.5f;

        public const char UnicodeDegree     = '\u00b0'; // °
        public const char UnicodeAlpha      = '\u03b1'; // α
        public const char UnicodeDelta      = '\u03b4'; // δ
        public const char UnicodeTriangleup = '\u25b2'; // ▲

        public static string FormatFloat(float x) => $"{( x >= 0 ? "+" : "")}{x:N2}";
        public static string FormatDegrees(float x) => FormatFloat(x) + UnicodeDegree;

        // TODO: Bug with some chars at the very edge of the atlas, but we can simply swap them, so...
        private static readonly string _charsStr = " ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !\"#$%&'()*+,-./0123456789:;<=>O@ABCDEFGHIJKLMN?PQRSTUVWXYZ[\\]^o`abcdefghijklmn_pqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";
        //private static readonly string _charsStr = " ☺☻♥♦♣♠•◘○◙♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼ !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~⌂ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»░▒▓│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌█▄▌▐▀αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ²■ ";
        private static Dictionary<int, int> _charsIndex;
        
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

            var mat = new Material(Shader.Find("Shapes/Label"));
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

        private static int GetIndex(string str, int i)
        {
            var c = i >= str.Length ? ' ' : str[i];
            if (_charsIndex == null) _charsIndex = new Dictionary<int, int>();
            if (_charsIndex.TryGetValue(c, out var index)) return index;

            index = _charsStr.IndexOf(c) + 1;
            _charsIndex[c] = index;
            return index;
        }
        
        // TODO: Allow something like `SetText("Hey: {0}", 12)` to prevent alloc furthermore, altho this is debug stuff so not worth the time currently
        public static void Draw(LabelInfo info)
        {
            if (info.Text == null || string.IsNullOrEmpty(info.Text)) return;

            var countf = info.Text.Length / 3f;
            var count = Mathf.CeilToInt(countf);
            var step = (3 / 4f) * info.Size;
            var stepSize = info.Rotation * (step * 2 * Vector3.right);
            var pos = info.Position - stepSize * (count - 1) / 2f;
            if (!info.Center) pos = info.Position + stepSize / 2f;
            if (info.Center) pos += (count - countf) * stepSize / 2f;
            
            for (var i = 0; i < count; i++)
            {
                var data = 0;
                for (var j = 0; j < 3; j++) data |= (GetIndex(info.Text, i * 3 + j) << (8 * j));
                
                DrawPart(pos + stepSize * (i * 1), data, info.Color, info.Size, info.Rotation);
            }
        }

        private static void DrawPart(Vector3 position, int data, Color color, float size, Quaternion rotation)
        {
            var mesh = GetMesh();
            var material = GetMaterial();
            var materialPropertyBlock = GetMaterialPropertyBlock(color);
            
            materialPropertyBlock.SetColor(_fillColor, color);
            materialPropertyBlock.SetInt(_index, data);

            var scale = new Vector3( -3f/4f, 0.5f, 0 ) * size;
            var matrix = Matrix4x4.TRS(position, rotation, scale); // Quaternion.LookRotation(Common.normal)

            Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, materialPropertyBlock);
        }
    }
}