Shader "Custom/Portal"{
    //show values to edit in inspector
    Properties{
        _Color("Tint", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _Mask("Mask", 2D) = "white" {}
        _MaskThreshold("Mask Threshold", Range(0, 1)) = 0.5
    }

        SubShader{
        //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry"}

        Pass{
            CGPROGRAM
            
            //include useful shader functions
            #include "UnityCG.cginc"

            //define vertex and fragment shader
            #pragma vertex vert
            #pragma fragment frag

            //texture and transforms of the texture
            sampler2D _MainTex;
            float4 _MainTex_ST;

            //mask texture and transforms
            sampler2D _Mask;
            float4 _Mask_ST;

            //threshold for the mask
            float _MaskThreshold;

            //tint of the texture
            fixed4 _Color;

            //the object data that's put into the vertex shader
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            //the data that's used to generate fragments and can be read by the fragment shader
            struct v2f {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };
            
            struct fragOut
            {
                fixed4 color : SV_TARGET;
            };

            //the vertex shader
            v2f vert(appdata v) {
                v2f o;
                //convert the vertex positions from object space to clip space so they can be rendered
                o.position = UnityObjectToClipPos(v.vertex);
                o.screenPosition = ComputeScreenPos(o.position);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            //the fragment shader
            fragOut frag(v2f i) {
                fragOut o;
                
                // 
                float2 uv = i.uv;
                float mask = tex2D(_Mask, uv).r;
                if (mask < _MaskThreshold) discard;

                float2 textureCoordinate = i.screenPosition.xy / i.screenPosition.w;
                
                fixed4 col = tex2D(_MainTex, textureCoordinate);                
                col *= _Color;
                o.color = col;

                return o;
            }

            ENDCG
        }
    }
}