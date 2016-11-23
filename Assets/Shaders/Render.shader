Shader "Unlit/ParticleFlow/Render"
{
	Properties
	{
		_MainTex 			("Texture", 2D) 				= "white" {}
		_NoiseTex 			("Noise Texture", 2D) 			= "white" {}
		_NoiseFactor		("Noise Factor", float) 		= 1.0
		_PerlinTime			("Perlin Time", float) 			= 1.0
		_PositionBufferTex 	("Position Texture", 2D) 		= "white" {}
		_ColorLookupTex 	("Color Lookup Texture", 2D) 	= "white" {}
		_VelocityLookupTex 	("Velocity Lookup Texture", 2D) = "white" {}
		_FieldSize			("Vector Field Size", float) 	= 1.0
		_SizeFactorOffset	("Size Factor Offset", float) 	= 1.0
		_SizeFactorMul		("Size Factor Mul", float) 		= 1.0
		_QuadSize 			("Quad Size", float) 			= .01
	}
	SubShader
	{
		Cull Off
		Lighting Off
		ZWrite Off

		Tags { "Queue" = "Transparent" }	
		
		// alpha blending
		//Blend SrcAlpha OneMinusSrcAlpha
		// additive blending
		Blend SrcAlpha One
	
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			#define M_2_PI 6.28318530718

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _PositionBufferTex;
			sampler2D _ColorLookupTex;
			sampler2D _VelocityLookupTex;
			float _SizeFactorOffset;
			float _SizeFactorMul;
			float _FieldSize;
			float4 _MainTex_ST;
			float _QuadSize;

			float fast_sigmoid(float x) 
			{
			  if (x >= 1.0) return 1.0;
			  else if (x <= -1.0) return 0.0;
			  else return 0.5 + x * (1.0 - abs(x) * 0.5);
			}

			v2f vert (appdata v)
			{
				v2f o;

				float4 position = tex2Dlod(_PositionBufferTex, float4(v.vertex.xy, .0, .0));
				position.w = v.vertex.w;

				// this block is all about modulating the particle size acording to the velocity field
				float2 uv = float2((position.x + floor(position.z * _FieldSize)) / _FieldSize, position.y);
				float4 velocity = tex2Dlod(_VelocityLookupTex, float4(uv, .0, .0));
				velocity = (velocity - float4(.5, .5, .5, .0)) * 2.0;
				float sizeFactor = length(velocity.xyz);					

				sizeFactor = clamp((sizeFactor + _SizeFactorOffset) * _SizeFactorMul, .0, 1.0f);

				sizeFactor = fast_sigmoid(sizeFactor * 2.0 - 1.0);

				// offset position according to the angle passed in the z coordinate to build a quad
				float4 posOffsetQuad = float4(cos(v.vertex.z), sin(v.vertex.z), .0, .0) * _QuadSize * sizeFactor;

				position -= float4(.5, .5, .5, .0);
				o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_MV, position) - posOffsetQuad);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv2 = uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_ColorLookupTex, i.uv2) * tex2D(_MainTex, i.uv);
			}
			ENDCG
		}
	}
}
