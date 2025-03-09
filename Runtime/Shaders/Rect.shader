﻿Shader "Hidden/Shapes/Rect"
{
	Properties
	{
	}
	SubShader
	{
		Tags {
			"RenderType" = "Transparent"
			"Queue" = "Transparent+175"
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
            #pragma multi_compile _ BORDER

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
			
			UNITY_INSTANCING_BUFFER_START(CommonProps)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _FillColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _AASmoothing)
            UNITY_INSTANCING_BUFFER_END(CommonProps)
			
			#if BORDER
            UNITY_INSTANCING_BUFFER_START(BorderProps)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BorderColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _FillWidth)
                UNITY_DEFINE_INSTANCED_PROP(float, _FillHeight)
            UNITY_INSTANCING_BUFFER_END(BorderProps)
            #endif
			
			v2f vert (appdata v)
			{
				v2f o;
				
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID(i);
                
                float aaSmoothing = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _AASmoothing);
			    fixed4 fillColor = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _FillColor);

				float distanceToCenter = i.uv.x;
			    
			    float distancePerPixel = fwidth(distanceToCenter);
			    float distanceAlphaFactor = 1.0 - smoothstep(1.0-distancePerPixel*aaSmoothing,1.0,distanceToCenter);
				
				#if BORDER
				float fillWidth = UNITY_ACCESS_INSTANCED_PROP(BorderProps, _FillWidth);
				float fillHeight = UNITY_ACCESS_INSTANCED_PROP(BorderProps, _FillHeight);
				float _HorizontalBorderThickness = fillWidth;
				float _VerticalBorderThickness = fillHeight;
				float4 _BorderColor = float4(0, 0, 0, 1);
				float4 _Color = fillColor;
				
				// Determine if the pixel is within the border area
                bool isHorizontalBorder = i.uv.y < -1 + _HorizontalBorderThickness || i.uv.y > (1.0 - _HorizontalBorderThickness);
                bool isVerticalBorder = i.uv.x < -1 + _VerticalBorderThickness || i.uv.x > (1.0 - _VerticalBorderThickness);

				// TODO: Too naive? Use step?
                if (isHorizontalBorder || isVerticalBorder)
                {
                    fillColor = _BorderColor;
                }
                else
                {
                    fillColor = _Color;
                }
				#endif

				fillColor.a *= distanceAlphaFactor;
			    return fillColor;
			}
			ENDCG
		}
	}
}