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

        private static int _charsLength = 0;
        private static int _charsMax = 0;
        private static readonly int[] _chars = new int[1024];
        private static readonly decimal[] _power = { 5e-1m, 5e-2m, 5e-3m, 5e-4m, 5e-5m, 5e-6m, 5e-7m, 5e-8m, 5e-9m, 5e-10m }; // Used by FormatText to enable rounding and avoid using Mathf.Pow.
        
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

        private static char GetChar(int i)
        {
            return i >= _charsLength ? ' ' : (char) _chars[i];
        }
        
        private static char GetChar(string str, int i)
        {
            return i >= str.Length ? ' ' : str[i];
        }
        
        private static int GetIndex(char c)
        {
            if (_charsIndex == null) _charsIndex = new Dictionary<int, int>();
            if (_charsIndex.TryGetValue(c, out var index)) return index;

            index = _charsStr.IndexOf(c) + 1;
            _charsIndex[c] = index;
            return index;
        }
        
        private static int GetIndex(string str, int i)
        {
            var c = i >= str.Length ? ' ' : str[i];
            return GetIndex(c);
        }
        
        private static int GetIndex(int i)
        {
            var c = i >= _charsLength ? ' ' : (char) _chars[i];
            return GetIndex(c);
        }
        
        public static void Draw(LabelInfo info)
        {
            if (info.Text == null || string.IsNullOrEmpty(info.Text)) return;

            // Count new lines
            var indexStart = 0;
            var indexEnd = 0;
            var indexMax = 0;
            for (var i = 0; i < info.Text.Length; i++)
            {
                var c = GetChar(info.Text, i);
                if (c == '\n')
                {
                    indexEnd = i;
                    var length = indexEnd - indexStart;
                    if (length > indexMax) indexMax = length;
                    
                    indexStart = i + 1;
                }
            }
            indexMax = indexMax == 0 ? info.Text.Length : indexMax;
            
            var count = Mathf.CeilToInt(info.Text.Length);
            var countf = indexMax / 3f;
            var countMax = Mathf.CeilToInt(indexMax / 3f);
            var step = (3 / 4f) * info.Size;
            var stepSize = info.Rotation * (step * 2 * Vector3.right);
            var pos = info.Position - stepSize * (countMax - 1) / 2f;
            if (!info.Center) pos = info.Position + stepSize / 2f;
            if (info.Center) pos += (countMax - countf) * stepSize / 2f;
            
            var remainder = 0;
            var offset = 0;
            var offsetPos = 0;
            for (var i = 0; i < count; i++)
            {
                var data = 0;
                var newLine = false;
                for (var j = 0; j < 3; j++)
                {
                    var c = GetChar(info.Text, i * 3 + j - offset);
                    if (c == '\n')
                    {
                        remainder = 2 - j;
                        for (; j < 3; j++) data |= (GetIndex(' ') << (8 * j));
                        newLine = true;
                        break;
                    }
                    
                    data |= (GetIndex(c) << (8 * j));
                }
                
                DrawPart(pos + stepSize * (i - offsetPos), data, info.Color, info.Size, info.Rotation);
                
                if (newLine)
                {
                    pos += info.Rotation * (info.Size * Vector3.down);
                    offsetPos = i + 1;
                    
                    offset += remainder;
                    count++;
                }
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
        
        // The code below was lifted from `TextMeshPro`
        public static void DrawFormat(LabelInfo info, float arg0)
        {
            DrawFormat(info, arg0, 0, 0, 0, 0, 0, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1)
        {
            DrawFormat(info, arg0, arg1, 0, 0, 0, 0, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2)
        {
            DrawFormat(info, arg0, arg1, arg2, 0, 0, 0, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2, float arg3)
        {
            DrawFormat(info, arg0, arg1, arg2, arg3, 0, 0, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2, float arg3, float arg4)
        {
            DrawFormat(info, arg0, arg1, arg2, arg3, arg4, 0, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5)
        {
            DrawFormat(info, arg0, arg1, arg2, arg3, arg4, arg5, 0, 0);
        }
        
        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6)
        {
            DrawFormat(info, arg0, arg1, arg2, arg3, arg4, arg5, arg6, 0);
        }

        public static void DrawFormat(LabelInfo info, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7)
        {
            if (info.Text == null || string.IsNullOrEmpty(info.Text)) return;
            
            DrawInternalText(info.Text, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            
            var count = Mathf.CeilToInt(_charsLength / 3f);
            var countf = _charsMax / 3f;
            var countMax = Mathf.CeilToInt(_charsMax / 3f);
            var step = (3 / 4f) * info.Size;
            var stepSize = info.Rotation * (step * 2 * Vector3.right);
            var pos = info.Position - stepSize * (countMax - 1) / 2f;
            if (!info.Center) pos = info.Position + stepSize / 2f;
            if (info.Center) pos += (countMax - countf) * stepSize / 2f;

            var remainder = 0;
            var offset = 0;
            var offsetPos = 0;
            for (var i = 0; i < count; i++)
            {
                var data = 0;
                var newLine = false;
                for (var j = 0; j < 3; j++)
                {
                    var c = GetChar(i * 3 + j - offset);
                    if (c == '\n')
                    {
                        remainder = 2 - j;
                        for (; j < 3; j++) data |= (GetIndex(' ') << (8 * j));
                        newLine = true;
                        break;
                    }
                    
                    data |= (GetIndex(c) << (8 * j));
                }
                
                DrawPart(pos + stepSize * (i - offsetPos), data, info.Color, info.Size, info.Rotation);

                if (newLine)
                {
                    pos += info.Rotation * (info.Size * Vector3.down);
                    offsetPos = i + 1;
                    
                    offset += remainder;
                    count++;
                }
            }
        }
        
        private static void DrawInternalText(string sourceText, float arg0, float arg1, float arg2, float arg3, float arg4, float arg5, float arg6, float arg7)
        {
            var argIndex = 0;
            var padding = 0;
            var decimalPrecision = 0;

            var readFlag = 0;

            var readIndex = 0;
            var writeIndex = 0;

            var indexStart = 0;
            var indexEnd = 0;
            var indexMax = 0;

            for (; readIndex < sourceText.Length; readIndex++)
            {
                var c = sourceText[readIndex];

                if (c == '{')
                {
                    readFlag = 1;
                    continue;
                }

                if (c == '}')
                {
                    // Add arg(index) to array
                    switch (argIndex)
                    {
                        case 0:
                            AddFloatToInternalTextBackingArray(arg0, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 1:
                            AddFloatToInternalTextBackingArray(arg1, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 2:
                            AddFloatToInternalTextBackingArray(arg2, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 3:
                            AddFloatToInternalTextBackingArray(arg3, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 4:
                            AddFloatToInternalTextBackingArray(arg4, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 5:
                            AddFloatToInternalTextBackingArray(arg5, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 6:
                            AddFloatToInternalTextBackingArray(arg6, padding, decimalPrecision, ref writeIndex);
                            break;
                        case 7:
                            AddFloatToInternalTextBackingArray(arg7, padding, decimalPrecision, ref writeIndex);
                            break;
                    }

                    argIndex = 0;
                    readFlag = 0;
                    padding = 0;
                    decimalPrecision = 0;
                    continue;
                }

                // Read Argument index
                if (readFlag == 1)
                {
                    if (c >= '0' && c <= '8')
                    {
                        argIndex = c - 48;
                        readFlag = 2;
                        continue;
                    }
                }

                // Read formatting for integral part of the value
                if (readFlag == 2)
                {
                    // Skip ':' separator
                    if (c == ':')
                        continue;

                    // Done reading integral formatting and value
                    if (c == '.')
                    {
                        readFlag = 3;
                        continue;
                    }

                    if (c == '#')
                    {
                        // do something
                        continue;
                    }

                    if (c == '0')
                    {
                        padding += 1;
                        continue;
                    }

                    if (c == ',')
                    {
                        // Use commas in the integral value
                        continue;
                    }

                    // Legacy mode
                    if (c >= '1' && c <= '9')
                    {
                        decimalPrecision = c - 48;
                        continue;
                    }
                }

                // Read Decimal Precision value
                if (readFlag == 3)
                {
                    if (c == '0')
                    {
                        decimalPrecision += 1;
                        continue;
                    }
                }

                // Calculate max width
                if (c == '\n')
                {
                    indexEnd = writeIndex;
                    var length = indexEnd - indexStart;
                    if (length > indexMax) indexMax = length;
                    
                    indexStart = writeIndex + 1;
                }
                
                // Write value
                _chars[writeIndex] = c;
                writeIndex += 1;
            }

            _chars[writeIndex] = 0;
            _charsLength = writeIndex;
            _charsMax = indexMax == 0 ? writeIndex : indexMax;
        }
        
        private static void AddFloatToInternalTextBackingArray(float value, int padding, int precision, ref int writeIndex)
        {
            if (value < 0)
            {
                _chars[writeIndex] = '-';
                writeIndex += 1;
                value = -value;
            }

            // Using decimal type due to floating point precision impacting formatting
            var valueD = (decimal)value;

            // Round up value to the specified prevision otherwise set precision to max.
            if (padding == 0 && precision == 0)
                precision = 9;
            else
                valueD += _power[Mathf.Min(9, precision)];

            var integer = (long)valueD;

            AddIntegerToInternalTextBackingArray(integer, padding, ref writeIndex);

            if (precision > 0)
            {
                valueD -= integer;

                // Add decimal point and values only if remainder is not zero.
                if (valueD != 0)
                {
                    // Add decimal point
                    _chars[writeIndex++] = '.';

                    for (var p = 0; p < precision; p++)
                    {
                        valueD *= 10;
                        var d = (long)valueD;

                        _chars[writeIndex++] = (char)(d + 48);
                        valueD -= d;

                        if (valueD == 0)
                            p = precision;
                    }
                }
            }
        }
        
        private static void AddIntegerToInternalTextBackingArray(double number, int padding, ref int writeIndex)
        {
            var integralCount = 0;
            var i = writeIndex;

            do
            {
                _chars[i++] = (char)(number % 10 + 48);
                number /= 10;
                integralCount += 1;
            } while (number > 0.999999999999999d || integralCount < padding);

            var lastIndex = i;

            // Reverse string
            while (writeIndex + 1 < i)
            {
                i -= 1;
                (_chars[writeIndex], _chars[i]) = (_chars[i], _chars[writeIndex]);
                writeIndex += 1;
            }
            writeIndex = lastIndex;
        }
    }
}