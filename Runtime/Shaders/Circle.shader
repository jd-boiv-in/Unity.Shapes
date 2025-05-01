Shader "Hidden/Shapes/Circle"
{
	Properties
	{
	}
	SubShader
	{
		Tags {
			"RenderType" = "Transparent"
			"Queue" = "Transparent+173"
			"DisableBatching" ="true"
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
			
			#pragma multi_compile _ BORDER 
			#pragma multi_compile _ SECTOR
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

				#if SECTOR && BORDER
				float3 worldPos   : TEXCOORD3;
				#endif
			};

            UNITY_INSTANCING_BUFFER_START(CommonProps)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _FillColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _AASmoothing)
            UNITY_INSTANCING_BUFFER_END(CommonProps)

			#if BORDER
			UNITY_INSTANCING_BUFFER_START(BorderProps)
			     UNITY_DEFINE_INSTANCED_PROP(fixed4, _BorderColor)
			     UNITY_DEFINE_INSTANCED_PROP(float, _FillWidth)
			UNITY_INSTANCING_BUFFER_END(BorderProps)
			#endif
			
		    #if SECTOR
			UNITY_INSTANCING_BUFFER_START(SectorProps)
			     UNITY_DEFINE_INSTANCED_PROP(float4, _cutPlaneNormal1)
			     UNITY_DEFINE_INSTANCED_PROP(float4, _cutPlaneNormal2)
			     UNITY_DEFINE_INSTANCED_PROP(float, _AngleBlend)
			UNITY_INSTANCING_BUFFER_END(SectorProps)
            #endif
            
			v2f vert (appdata v)
			{
				v2f o;
				
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.vertex.xy;

				#if SECTOR && BORDER
            	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#endif
            	
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			    UNITY_SETUP_INSTANCE_ID(i);

			    float aaSmoothing = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _AASmoothing);
			    fixed4 fillColor = UNITY_ACCESS_INSTANCED_PROP(CommonProps, _FillColor);

			    float distanceToCenter = length(i.uv);

			    float distancePerPixel = fwidth(distanceToCenter);
			    float distanceAlphaFactor = 1.0 - smoothstep(1.0 - distancePerPixel * aaSmoothing, 1.0, distanceToCenter);
			    float halfSmoothFactor = 0.5f * distancePerPixel * aaSmoothing;

			    #if BORDER
			    float fillWidth = UNITY_ACCESS_INSTANCED_PROP(BorderProps, _FillWidth);
			    fixed4 borderColor = UNITY_ACCESS_INSTANCED_PROP(BorderProps, _BorderColor);

			    float fillToBorder = smoothstep(fillWidth - halfSmoothFactor, fillWidth + halfSmoothFactor, distanceToCenter);
			    fixed4 circleColor = lerp(fillColor, borderColor, fillToBorder);
			    #else
			    fixed4 circleColor = fillColor;
			    #endif

			    #if SECTOR
			    float4 cutPlaneNormal1 = UNITY_ACCESS_INSTANCED_PROP(SectorProps, _cutPlaneNormal1);
			    float4 cutPlaneNormal2 = UNITY_ACCESS_INSTANCED_PROP(SectorProps, _cutPlaneNormal2);
			    float angleBlend = UNITY_ACCESS_INSTANCED_PROP(SectorProps, _AngleBlend);

			    float2 pos = float2(i.uv.x, i.uv.y);

			    float distanceToPlane1 = dot(pos, cutPlaneNormal1);
			    float distanceToPlane1PerPixel = fwidth(distanceToPlane1);
			    float distanceToPlane1Alpha = 1.0 - smoothstep(0,0 + distanceToPlane1PerPixel * aaSmoothing, distanceToPlane1);

			    float distanceToPlane2 = dot(pos, cutPlaneNormal2);
			    float distanceToPlane2PerPixel = fwidth(distanceToPlane2);
			    float distanceToPlane2Alpha = 1.0 - smoothstep(0,0 + distanceToPlane2PerPixel * aaSmoothing, distanceToPlane2);

				// Is it faster than an "if"?
				float sectorAlpha = distanceToPlane1Alpha * distanceToPlane2Alpha * (1 - angleBlend) + max(distanceToPlane1Alpha, distanceToPlane2Alpha) * angleBlend;
				
				//float sectorAlpha = distanceToPlane1Alpha * distanceToPlane2Alpha;
				/*float sectorAlpha;
				if (angleBlend == 1) { // OR
			        sectorAlpha = max(distanceToPlane1Alpha, distanceToPlane2Alpha);
			    } else { // AND
			        sectorAlpha = distanceToPlane1Alpha * distanceToPlane2Alpha;
			    }*/

			    #if BORDER
			    fillWidth = -1 + fillWidth;

				// Slight adjustment to compensate for the outer border being naturally fading as the distance goes
				// Not sure why it isn't taken into account for the border on the sector, but this gives me exactly what I wanted
				// Hopefully this works in other cases?
				float distanceToCamera = distance(i.worldPos, _WorldSpaceCameraPos.xyz);
                float distanceFactor = max(1 - (max(distanceToCamera, 3) - 3) / 13.0, 0);
				fillWidth *= distanceFactor;

			    float borderOnPlane1 = smoothstep(fillWidth - halfSmoothFactor, halfSmoothFactor + fillWidth, distanceToPlane1);
			    float borderOnPlane2 = smoothstep(fillWidth - halfSmoothFactor, halfSmoothFactor + fillWidth, distanceToPlane2);

				// Is it faster than an "if"?
				float borderBlend = borderOnPlane1 * borderOnPlane2 * angleBlend + max(borderOnPlane1, borderOnPlane2) * (1 - angleBlend);
				
			    //float borderBlend = max(borderOnPlane1, borderOnPlane2);
				/*float borderBlend;
				if (angleBlend == 1) {
					borderBlend = borderOnPlane1 * borderOnPlane2;
				} else {
					borderBlend = max(borderOnPlane1, borderOnPlane2);
				}*/
				
			    circleColor = lerp(circleColor, borderColor, borderBlend * distanceFactor);
			    #endif

			    // Adjust the alpha of the circle color based on the sector
			    circleColor.a *= sectorAlpha;
			    #endif

			    circleColor.a *= distanceAlphaFactor;

			    return circleColor;
			}
			ENDCG
		}
	}
}
