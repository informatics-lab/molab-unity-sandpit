Shader "Unlit/MyUnlitShader"
{
	Properties
	{
		_DepthTex ("Depth Map Texture (RGB565)", 2D) = "red" {}
		_DepthTexWidth ("Depth Map Texture Width", Float) = 1.0
		_DepthTexHeight ("Depth Map Texture Height", Float) = 1.0
		_DepthMatrix ("Height Matrix",Vector) = (1.,1.,1.,1.)

		_Scale ("Size",Vector) = (1,1,1,1)

		_ColorTex ("Color Map Texture (RGB)", 2D) = "white" {}


		_MAX_KINECT_VALUE ("top of kinect range", int) = 1
		_MIN_KINECT_VALUE ("bottom of kinect range", int) = 0


//		matrix MirroredIdentity = {
//			    { 0, 0, 0, 1 },
//			    { 0, 0, 1, 0 },
//			    { 0, 1, 0, 0 },
// 			    { 1, 0, 0, 0 }
//			};


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

    		float4 _DepthMatrix;
    		float4 _DepthTex_TexelSize;
			float4 _Scale;

    		sampler2D _ColorTex;

    		float _MAX_KINECT_VALUE;
	        float _MIN_KINECT_VALUE;


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
				v.uv[1] = abs(1-v.uv[1]);
		        float4 pos = float4(v.uv, 0.0, 0.0); // changing v.uv to v.vertex.xz splits the image
		        float4 depth = tex2Dlod(_DepthTex, pos); 

   		        // this rescales the texture value
				float scale = (_MAX_KINECT_VALUE-_MIN_KINECT_VALUE) / 255.0;
   		        float4 height = float4(1-depth.r,0,0,0);



   		        // change the vertex height based on the color
   		        v.vertex.y = height * scale * 20;
   		        //v.vertex.x = abs(1-v.vertex.x) - 1					;
   		        //v.vertex.z = abs(1-v.vertex.z) - 1					;

   		        // sample the colorTexture (land sea colour scheme)
   		        o.color = tex2Dlod(_ColorTex, height);
		        // magically transfers the 3d verteces to the 2d display
		        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);


		        return o;
		    }





		    // The fragment shader - handles the color of vertex
		    fixed4 frag (v2f i) : SV_Target { 
		       //	i.color = float4 ( tex2D (_ColorTex, i.uv).rgb, 1.0);
		       	return i.color;
		    }




		    // SAVED VERSIONS!!!!
      		// The vertex shader - handles the position of the vertex 
//		    v2f vert(appdata v) {
//		        v2f o;
//		        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		        float4 pos = float4(v.uv, 0.0, 0.0);
//   		        float4 depthRGBA = tex2Dlod(_DepthTex, pos); 
//			    int scale = 1 / ( 255 / (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) ) ; 
//   		        float red = depthRGBA.r * scale;
//   		        float4 height = float4(red,0,0,0);
//   		        o.pos.y = o.pos.y * height/scale; /// scale; 
//   		        float4 heightInverse = float4((1*scale)-red,0,0,0);
//   		        //o.color =  heightInverse;
//   		        o.color =  tex2Dlod(_ColorTex, heightInverse/scale);
//
//		        return o;
//		    }
//
//		    // The fragment shader - handles the color of vertex
//		    fixed4 frag (v2f i) : SV_Target { 
//		       //	i.color = float4 ( tex2D (_ColorTex, i.uv).rgb, 1.0);
//		       	return i.color;
//		    }




		    ENDCG
		}
	}
}
