Shader "Unlit/GeoFixedLineShader"
{

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "ProjectionShader.cginc"
            #include "GeodesyShader.cginc"

            float4 _CurrentENUOriginWGS;
            float4 _ProjectionCenterInWorldSpace;
            float4 _CylinderCenterInWorldSpace;
            float _CylinderRadius;
            int _ProjectionEnabled;

            // The various UNITY_INSTANCE_ID, UNITY_VERTEX_OUTPUT_STEREO, etc
            // are required to perform Single-Pass Rendering on the Hololens
            // https://docs.unity3d.com/Manual/SinglePassStereoRenderingHoloLens.html

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;              
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 pointEcef = v.vertex;
                float4 pointEnu = ecef2enu(pointEcef, _CurrentENUOriginWGS);
                float4 pointUnity = enu2unity(pointEnu);

                if (_ProjectionEnabled == 1) {
                    pointUnity = mul(unity_ObjectToWorld, pointUnity);
                    float4 projected = projectToCylinder(pointUnity, _ProjectionCenterInWorldSpace, _CylinderCenterInWorldSpace, _CylinderRadius);
                    o.vertex = mul(UNITY_MATRIX_VP, projected);
                }
                else {
                    o.vertex = UnityObjectToClipPos(pointUnity);
                }

                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return i.color;
            }

            ENDCG
        }
    }
}
