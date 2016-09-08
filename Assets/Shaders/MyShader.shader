Shader "Custom/MyShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_DepthTex ("DepthTex",2D) = "black" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0

		//_DepthTex ("Depth Map Texture (RGB565)", 2D) = "red" {}
		_DepthTexWidth ("Depth Map Texture Width", Float) = 1.0
		_DepthTexHeight ("Depth Map Texture Height", Float) = 1.0
		_DepthMatrix ("Height Matrix",Vector) = (1.,1.,1.,1.)

		_Scale ("Size",Vector) = (1,1,1,1)

		_ColorTex ("Color Map Texture (RGB)", 2D) = "white" {}

	    _MAX_KINECT_VALUE ("Max Kinect Value", int)  = 4000 
	    _MIN_KINECT_VALUE ("Min Kinect Value", int)  = 800

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		// Use the method name 'vert' for the vertex shader
    	#pragma vertex vert            

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
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

    		int _MAX_KINECT_VALUE;
	        int _MIN_KINECT_VALUE;


			struct Input {
				float2 uv_MainTex;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;






      	void vert(inout appdata_full v) {

			float4 pos = float4( TRANSFORM_TEX( v.vertex.xz, _DepthTex ), 1, 1 );
			float4 height = tex2Dlod(_DepthTex,pos);
			int scale = 1 / ( 255 / (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) ) ; 
   	        float red = height * scale;

			// sets vertex yaxis to new height
			//v.vertex.y = dot( _DepthMatrix, red );
			v.color = tex2Dlod(_ColorTex, red);

			//=====-----=====-----=====-----=====-----=====

//		    v.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		    float4 pos = float4(v.uv, 0.0, 0.0);
//   		    float4 depthRGBA = tex2Dlod(_DepthTex, pos); 
//		    int scale = 1 / ( 255 / (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) ) ; 
//   	        float red = depthRGBA.r * scale;
//   	        float4 redfloat = float4(red,0,0,0);
//   	        v.vertex.y = v.vertex.y * red/scale; /// scale; 
//   		    float4 heightInverse = float4((1*scale)-red,0,0,0);
//   		    //o.color =  heightInverse;
//   		    v.color =  tex2Dlod(_ColorTex, heightInverse/scale);
//


	      		//-----
	      		//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		        float4 pos = float4(TRANSFORM_TEX( v.vertex.xz, _DepthTex ), 1, 1);
//   		        float4 depthRGBA = tex2Dlod(_DepthTex, pos); 
//
//	      		int scale = 1 / ( 255 / (_MAX_KINECT_VALUE - _MIN_KINECT_VALUE) ) ; 
//	   		    float red = depthRGBA.r * scale;
//	            float4 height = float4(red,0,0,0);
//	   	        //v.vertex.y = v.vertex.y * height/scale; /// scale; 
//	   	        float4 heightInverse = float4((1*scale)-red,0,0,0);
//	   		    v.color =  tex2Dlod(_ColorTex, heightInverse/scale);
//
//	   		  
      		}

      		// The vertex shader - handles the position of the vertex 
//		    v2f vert(appdata v) {
//		        v2f o;
//		        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		        float4 pos = float4(v.uv, 0.0, 0.0);
//   		        float4 depthRGBA = tex2Dlod(_DepthTex, pos); 
//
////   		        float4 forward = float4(_DepthTex_TexelSize.x,_DepthTex_TexelSize.y,0,0);
////				float4 right =   float4(_DepthTex_TexelSize.x,_DepthTex_TexelSize.y,0,0);
////				// define the difference  between the current position + 1 step and - 1 step 
////				float3 forwardHeightDelta = tex2Dlod(_DepthTex, pos+forward) - tex2Dlod(_DepthTex, pos-forward);
////				// do the same for left and right
////				float3 rightHeightDelta = tex2Dlod(_DepthTex, pos+right) - tex2Dlod(_DepthTex, pos-right);
////				// scaling factor (multiple of texel size)
////				// is this mapping the model space to the texture space so that they fit each other?
////				float3 unit = _Scale * float3(_DepthTex_TexelSize.x,2.0,_DepthTex_TexelSize.y);
//
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




		    // The fragment shader - handles the color of vertex
//		    fixed4 frag (v2f i) : SV_Target { 
//		       //	i.color = float4 ( tex2D (_ColorTex, i.uv).rgb, 1.0);
//		       	return i.color;
//		    }





		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
		//	fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
		//	o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
		//	o.Metallic = _Metallic;
		//	o.Smoothness = _Glossiness;
		//	o.Alpha = c.a;

		//    o.color;
		}


		ENDCG
	}
	FallBack "Diffuse"
}
