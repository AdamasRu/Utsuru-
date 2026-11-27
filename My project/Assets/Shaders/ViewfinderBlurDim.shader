Shader "UI/ViewfinderBlurDim"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Dim Tint Color", Color) = (0.05, 0.05, 0.08, 0.45)
        _BlurSize ("Blur Spread", Range(0.0, 5.0)) = 1.8
        
        // UI Mask / Stencil properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        GrabPass
        {
            "_CameraViewfinderGrab"
        }

        Pass
        {
            Name "ViewfinderBlurDim"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 grabPos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _TintColor;
            float _BlurSize;
            sampler2D _CameraViewfinderGrab;
            float4 _CameraViewfinderGrab_TexelSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.grabPos.xy / i.grabPos.w;
                float2 texel = _CameraViewfinderGrab_TexelSize.xy * _BlurSize;

                // 9-tap Gaussian distribution blur
                half4 col = half4(0, 0, 0, 0);

                col += tex2D(_CameraViewfinderGrab, uv + float2(-texel.x, -texel.y)) * 0.075;
                col += tex2D(_CameraViewfinderGrab, uv + float2(0.0,      -texel.y)) * 0.125;
                col += tex2D(_CameraViewfinderGrab, uv + float2( texel.x, -texel.y)) * 0.075;

                col += tex2D(_CameraViewfinderGrab, uv + float2(-texel.x,  0.0))     * 0.125;
                col += tex2D(_CameraViewfinderGrab, uv + float2(0.0,       0.0))     * 0.200;
                col += tex2D(_CameraViewfinderGrab, uv + float2( texel.x,  0.0))     * 0.125;

                col += tex2D(_CameraViewfinderGrab, uv + float2(-texel.x,  texel.y)) * 0.075;
                col += tex2D(_CameraViewfinderGrab, uv + float2(0.0,       texel.y)) * 0.125;
                col += tex2D(_CameraViewfinderGrab, uv + float2( texel.x,  texel.y)) * 0.075;

                // Combine with tint and UI vertex color
                fixed4 finalTint = _TintColor * i.color;
                col.rgb = lerp(col.rgb, finalTint.rgb, finalTint.a);

                return fixed4(col.rgb, i.color.a);
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
