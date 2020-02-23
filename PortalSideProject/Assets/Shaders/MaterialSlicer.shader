Shader "Custom/MaterialSlicer"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "IgnoreProjector" = "True"  "RenderType" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard addshadow
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float3 _SliceCenter;
        float3 _SliceNormal;
        float _SliceOffsetDist;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 slicePos = _SliceCenter + _SliceNormal * _SliceOffsetDist;
            float sliceSide = dot(slicePos - IN.worldPos, _SliceNormal);
            clip(sliceSide);

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "VertexLit"
}
