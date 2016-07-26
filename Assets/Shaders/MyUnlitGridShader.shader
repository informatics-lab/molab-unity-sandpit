Shader "Unlit/MyUnlitGridShader"
{
	Properties
	{
		_TerrainTex ("Terrain Map Texture (RGB565)", 2D) = "red" {}

		_TerrainTexWidth ("Terrain Map Texture Width", Float) = 1.0
		_TerrainTexHeight ("Terrain Map Texture Height", Float) = 1.0
		_TerrainMatrix ("Height Matrix",Vector) = (1.,1.,1.,1.)

		_Scale ("Size",Vector) = (1,1,1,1)

		_ColorTex ("Color Map Texture (RGB)", 2D) = "white" {}
		_WaterTex ("Color Map Texture (RGB)", 2D) = "white" {}


		_MAX_KINECT_VALUE ("top of kinect range", int) = 1
		_MIN_KINECT_VALUE ("bottom of kinect range", int) = 0

        _Grid_x_Index ("Grid x Index", int) = 3
	    _Grid_z_Index ("Grid z Index", int) = 2

	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			CGPROGRAM

    		#pragma vertex vert     		// Use the method name 'vert' for the vertex shader     
    		#pragma fragment frag     		// Use the method named 'frag' for the fragment shader 
    		#pragma target 4.0

    		// Include some magic from unity
    		#include "UnityCG.cginc"

    		sampler2D _TerrainTex;
    		float4 _TerrainTex_ST;
    		float _TerrainTexWidth;
    		float _TerrainTexHeight;

    		float4 _TerrainMatrix;
    		float4 _TerrainTex_TexelSize;
			float4 _Scale;

    		sampler2D _ColorTex;

    		float _MAX_KINECT_VALUE;
	        float _MIN_KINECT_VALUE;

	        int _Grid_x_Index; 
	        int _Grid_z_Index; 


    		// Object gets instantiated and passed into the vertex shader method.
    		struct appdata
			{
				float4 vertex : POSITION; // position in model space
				float2 uv : TEXCOORD0;    // position 0-1 xy in texture space
			};

    		// Represents a class that gets instantiated in the vertex shader, populated 
    		// and then passed as the input into the fragment shader
		    struct v2f {
          		float4 pos : SV_POSITION;   // position of vertex on screen
          		float2 uv : TEXCOORD0;		// 0-1 xy equivalent on texture
          		fixed4 color : COLOR;		

      		};


      		// The vertex shader - handles the position of the vertex 
		    v2f vert(appdata v) {
		        v2f o;

				// get the value of the texture (depthtex) that corresponds to the texture position (v.uv)
				// v.uv vals are from 0-1.
				// Swap z axis around to get rid of mirroring
				v.uv[1] = abs(1 - v.uv[1]);

				// put frames of the total texture onto the gridded terrain
				if ( _Grid_x_Index == 0 && _Grid_z_Index == 0){
					v.uv[0] = v.uv[0] / 3;
					v.uv[1] = v.uv[1] / 2;
				}				
				if ( _Grid_x_Index == 1 && _Grid_z_Index == 0){
					v.uv[0] = 0.33333 + (v.uv[0] / 3);
					v.uv[1] = v.uv[1] / 2;
				}
				if ( _Grid_x_Index == 2 && _Grid_z_Index == 0){
					v.uv[0] = 0.66666 + (v.uv[0] / 3);
					v.uv[1] = v.uv[1] / 2;
				}
				if ( _Grid_x_Index == 0 && _Grid_z_Index == 1){
					v.uv[0] = v.uv[0] / 3;
					v.uv[1] = 0.5 + (v.uv[1] / 2);
				}
				if ( _Grid_x_Index == 1 && _Grid_z_Index == 1){
					v.uv[0] = 0.33333 + (v.uv[0] / 3);
					v.uv[1] = 0.5     + (v.uv[1] / 2);
				}
				if ( _Grid_x_Index == 2 && _Grid_z_Index == 1){
					v.uv[0] = 0.66666 + (v.uv[0] / 3);
					v.uv[1] = 0.5     + (v.uv[1] / 2);
				}

		        float4 depth    = tex2Dlod (_TerrainTex, float4(v.uv, 0.0, 0.0)); 
		        float4 height   = float4 (1 - depth.r, 0, 0, 0);
		        float4 sealevel = float4 (1 - depth.b, 0, 0, 0);

		        // if the blue channel value is greater than the red channel value then do the blue channel instead
//				if (depth.b < depth.r){
//					o.color    = float4(0,0,1,0); //tex2Dlod(_WaterTex, sealevel);
//					v.vertex.y = sealevel * (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) * 170;
//				} else 
				if (depth.r  <= 0) { 			// i.e. if the value sampled corresponds to the 2047 (transformed to a 0)
   		        	o.color    = float4(0.73, 0.73, 0.35, 1.);
   		        } else {
   		           	v.vertex.y = height * (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) * 170;// change the vertex height 
					o.color = tex2Dlod(_ColorTex, height);  // sample the colorTexture (land sea colour scheme)
				}
				// magically transfers the 3d verteces to the 2d display
		        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
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
