Shader "UI/RoundedCorners/RoundedCorners" {
    Properties {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}

        // --- Mask support ---
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        // Definition in Properties section is required to Mask works properly
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (0,0,0,0)
        _OuterUV ("image outer uv", Vector) = (0, 0, 1, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        _BorderWidth ("Border Width", Float) = 0
        [HideInInspector] _ContentAlpha ("Graphic Color Alpha", Range(0, 1)) = 1
        [HideInInspector] _BorderShape ("Border Shape", Float) = 0
        [HideInInspector] _SpriteRect ("Sprite Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _OutlineAlphaThreshold ("Outline Alpha Threshold", Float) = 0.01
        // ---
    }
    
    SubShader {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        // --- Mask support ---
        Stencil {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        // ---
        
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"          
            #include "SDFUtils.cginc"
            #include "ShaderSetup.cginc"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            float4 _WidthHeightRadius;
            float4 _OuterUV;
            float4 _BorderColor;
            float _BorderWidth;
            float _ContentAlpha;
            float _BorderShape;
            float4 _SpriteRect;
            float _OutlineAlphaThreshold;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // Reject samples outside this sprite before sampling the atlas. Clamp filtering
            // to texel centers so adjacent packed sprites cannot contribute to the outline.
            half4 SampleOutlineSprite(float2 uv, float2 uvDx, float2 uvDy) {
                float2 uvSpan = max(_OuterUV.zw - _OuterUV.xy, float2(0.0001, 0.0001));
                // Canvas batching can transform vertex positions into canvas space.
                // UVs remain attached to the sprite, including on the expanded quad.
                float2 normalized = (uv - _OuterUV.xy) / uvSpan;
                float2 pixelSize = max((abs(uvDx) + abs(uvDy)) / uvSpan, float2(0.0001, 0.0001));
                float2 edgeCoverage = saturate(min(normalized, 1.0 - normalized) / pixelSize + 0.5);
                float boundsCoverage = min(edgeCoverage.x, edgeCoverage.y);
                if (boundsCoverage <= 0.0) return half4(0, 0, 0, 0);
                float2 inset = min(abs(_MainTex_TexelSize.xy) * 0.5, (_OuterUV.zw - _OuterUV.xy) * 0.5);
                uv = clamp(uv, _OuterUV.xy + inset, _OuterUV.zw - inset);
                half4 sampleColor = tex2Dgrad(_MainTex, uv, uvDx, uvDy) + _TextureSampleAdd;
                sampleColor.a *= boundsCoverage;
                return sampleColor;
            }

            float OutlineCoverage(float alpha) {
                float threshold = max(_OutlineAlphaThreshold, 0.001);
                float feather = min(threshold, 0.02);
                return smoothstep(threshold - feather, threshold + feather, alpha);
            }

            half4 SpriteOutline(v2f i) {
                float2 uvDx = ddx(i.uv);
                float2 uvDy = ddy(i.uv);
                half4 source = SampleOutlineSprite(i.uv, uvDx, uvDy);
                // Convert UI-local stroke distances to atlas UV distances per axis.
                // Only the sprite size is used; its position must not affect sampling.
                float2 uvPerUnit = (_OuterUV.zw - _OuterUV.xy) / max(_SpriteRect.zw, float2(0.0001, 0.0001));
                float centerCoverage = OutlineCoverage(source.a);
                float expandedCoverage = centerCoverage;
                if (_BorderWidth > 0.0) {
                    // Sample a disk, not just its circumference: rings also catch narrow
                    // features that would fall between the center and the outer ring.
                    [unroll] for (int ring = 1; ring <= 4; ++ring) {
                        float radius = _BorderWidth * (ring / 4.0);
                        [unroll] for (int direction = 0; direction < 16; ++direction) {
                            float angle = direction * 0.3926990817;
                            float2 offset = float2(cos(angle), sin(angle)) * radius * uvPerUnit;
                            float alpha = SampleOutlineSprite(i.uv + offset, uvDx, uvDy).a;
                            expandedCoverage = max(expandedCoverage, OutlineCoverage(alpha));
                        }
                    }
                }
                half4 spriteColor = source * i.color;
                // Shape coverage uses the original alpha so fading the content does
                // not change the outline or turn it into a fill behind the sprite.
                float contentA = source.a * _ContentAlpha;
                float borderA = _BorderWidth > 0.0 ? expandedCoverage * (1.0 - centerCoverage) * _BorderColor.a : 0.0;
                // Composite the sprite over the stroke in premultiplied space, then
                // convert back to straight alpha for the existing Canvas blend mode.
                borderA *= 1.0 - contentA;
                float totalA = contentA + borderA;
                half3 rgb = (spriteColor.rgb * contentA + _BorderColor.rgb * borderA) / max(totalA, 0.0001);
                totalA *= i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                totalA *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(totalA - 0.001);
                #endif
                return half4(rgb, totalA);
            }

            fixed4 frag (v2f i) : SV_Target {
                if (_BorderShape > 0.5) return SpriteOutline(i);
                // Determine normalized 0~1 coordinate across the base area
                float2 uvSample = i.uvRect;
                if (fwidth(i.uvRect.x) == 0.0 && fwidth(i.uvRect.y) == 0.0) {
                    uvSample = i.uv;
                    if (_OuterUV.z > _OuterUV.x && _OuterUV.w > _OuterUV.y) {
                        uvSample.x = (uvSample.x - _OuterUV.x) / (_OuterUV.z - _OuterUV.x);
                        uvSample.y = (uvSample.y - _OuterUV.y) / (_OuterUV.w - _OuterUV.y);
                    }
                }

                half4 spriteColor = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                spriteColor.a *= _ContentAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                half clipFactor = UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                spriteColor.a *= clipFactor;
                #endif

                float alphaOuter = CalcAlpha(uvSample, _WidthHeightRadius.xy, _WidthHeightRadius.z);

                if (_BorderWidth <= 0.0) {
                    spriteColor.a = min(spriteColor.a, alphaOuter);
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(spriteColor.a - 0.001);
                    #endif
                    return spriteColor;
                }

                float alphaInner = CalcInnerAlpha(uvSample, _WidthHeightRadius.xy, _WidthHeightRadius.z, _BorderWidth);

                half4 borderColor = _BorderColor;
                borderColor.a *= i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                borderColor.a *= clipFactor;
                #endif

                // borderWeight: 1 in border area, 0 inside content
                float borderWeight = 1.0 - alphaInner;
                // contentWeight: 0 in border area, 1 inside content
                float contentWeight = alphaInner;

                float borderA = borderColor.a * borderWeight;
                float spriteA = spriteColor.a * contentWeight;
                float totalA = borderA + spriteA;

                half3 finalRGB = (borderColor.rgb * borderA + spriteColor.rgb * spriteA) / max(0.0001, totalA);
                half4 finalColor = half4(finalRGB, min(totalA, alphaOuter));

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            
            ENDCG
        }
    }
}
