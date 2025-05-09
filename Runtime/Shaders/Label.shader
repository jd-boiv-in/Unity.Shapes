Shader "Hidden/Shapes/Label"
{
	Properties {}
	SubShader
	{
		Tags {
			"RenderType" = "Transparent"
			"Queue" = "Transparent+199"
			"DisableBatching" = "true"
		}
		LOD 100

		Pass
		{
			ZWrite Off
			ZTest Off
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			sampler2D _LabelTex;

			UNITY_INSTANCING_BUFFER_START(CommonProps)
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _FillColor)
				UNITY_DEFINE_INSTANCED_PROP(uint, _index)
			UNITY_INSTANCING_BUFFER_END(CommonProps)

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.vertex.xy;
				return o;
			}

			float invLerp(float from, float to, float value)
			{
				return (value - from) / (to - from);
			}

			float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
			{
				float rel = invLerp(origFrom, origTo, value);

				return lerp(targetFrom, targetTo, rel);
			}

			float2 IndexToUV(int x, int y, float2 uvs)
			{
				float sx = 1 / 32.0;
				float sy = 1 / 32.0;

				float iuvx = 1 - uvs.x;
				float iuvy = uvs.y;

				float uv_x = lerp(0, sx, iuvx) + (x * sx * 2);
				float uv_y = lerp(0, sy, iuvy) - sy - (y * sy * 2);

				return float2(uv_x, uv_y);
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				fixed4 fillColor = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _FillColor);
				int index = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _index);

				int3x2 ii = int3x2
				(
					index		& 0xF, 
					index >> 4	& 0xF, 
					index >> 8	& 0xF, 
					index >> 12	& 0xF, 
					index >> 16	& 0xF, 
					index >> 20	& 0xF //, 
					//index >> 24	& 0xF , 
					//index >> 28	& 0xF 
				);
				
				float2 UV = float2(1 - (i.uv.x + 1) / 2, (i.uv.y + 1) / 2);
				float2 uvs = float2(remap(-1, 1, -3, 3, i.uv.x), i.uv.y); // in Label.cs IndexOf() + 1

				float2 uvs0 = IndexToUV(ii[0][0], ii[0][1], uvs);
				fixed4 c0 = tex2D(_LabelTex, uvs0);
				c0 = UV.x >= 0.333333 ? 0 : c0;

				float2 uvs1 = IndexToUV(ii[1][0] - 1, ii[1][1], uvs);
				fixed4 c1 = tex2D(_LabelTex, uvs1);
				c1 = UV.x <= 0.333333 || UV.x >= 0.666666 ? 0 : c1;

				float2 uvs2 = IndexToUV(ii[2][0] - 2, ii[2][1], uvs);
				fixed4 c2 = tex2D(_LabelTex, uvs2);
				c2 = UV.x <= 0.666666 ? 0 : c2;

				fixed4 c = ( c0 + c1 + c2 ); // +c3 );

				c = UV.y > 0.99999 || UV.y < 0.00001 ? 0 : c;
				c = UV.x > 0.99999 || UV.x < 0.00001 ? 0 : c;

				c.a = c; // alpha is zero when color is black 

				c *= fillColor;

				//fixed4 background = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _Back);
				//c = c.a == 0 ? background : c;

				//float distanceToCenter = i.uv.x;
				//float distancePerPixel = fwidth(distanceToCenter);
				//float distanceAlphaFactor = 1.0 - smoothstep(1.0 - distancePerPixel * _AASmoothing, 1.0, distanceToCenter);
				//c.a *= distanceAlphaFactor;
				
				return c;
			}
			ENDCG
		}
	}
}
