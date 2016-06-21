Shader "Unlit/MyUnlitShader"
{
	Properties
	{
		_DepthTex ("Depth Map Texture (RGB565)", 2D) = "black" {}
		_DepthTexWidth ("Depth Map Texture Width", Float) = 1.0
		_DepthTexHeight ("Depth Map Texture Height", Float) = 1.0

		_ColorTex ("Color Map Texture (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM

			// Use the method name 'vert' for the vertex shader
    		#pragma vertex vert            
    		// Use the method named 'frag' for the fragment shader 
    		#pragma fragment frag

    		#pragma target 4.0

    		// Include some magic from unity
    		#include "UnityCG.cginc"

    		sampler2D _DepthTex;
    		float4 _DepthTex_ST;
    		float _DepthTexWidth;
    		float _DepthTexHeight;

    		sampler2D _ColorTex;

    		// Object gets instantiated and passed into the vertex shader method.
    		struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

    		// Represents a class that gets instantiated in the vertex shader, populated 
    		// and then passed as the input into the fragment shader
		    struct v2f {
          		float4 pos : SV_POSITION;
          		float2 uv : TEXCOORD0;
          		fixed4 color : COLOR;		// don't know if this is actually required?!
      		};

		

//		    float decodeFloatRGB565(float3 enc) {
//		    	    	
//		    }

//			float decodeFloatR16(float3 enc) {
//				float r = enc.r;
//			}

      		// The vertex shader - handles the position of the vertex 
		    v2f vert(appdata v) {
		        v2f o;
		        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		        o.uv = v.uv;

		        float4 depthRGBA = tex2Dlod(_DepthTex, float4(v.uv, 0.0, 0.0));

		        float r = depthRGBA.r;  // no data in this channel
		        float g = depthRGBA.g;  // no data in this channel
		        float b = depthRGBA.b;  // no data in this channel
		        float a = depthRGBA.a;  // data!

		        float val = a*16;
		        o.pos.y = val;

//				float scale = 1.0/255.0;
//				float val = depth * scale;

				o.color = float4(val,val,val,0.0);

//				if(val > 0.05) {
//				//red - above limit
//				o.color = float4(1.0,0.0,0.0,0.0);
//				} 
//				else if (val == 0.0) {
//				//blue - equal to 0
//				o.color = float4(0.0,0.0,1.0,0.0);
//				} else {
//				//green - between 0 and limit
//				o.color = float4(0.0,1.0,0.0,0.0);
//				} 

		        return o;
		    }

		    // The fragment shader - handles the color of vertex
		    fixed4 frag (v2f i) : SV_Target { 
		    	return i.color; 
		    }


		    ENDCG
		}
	}
}
