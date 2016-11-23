Shader "Unlit/ParticleFlow/ColorUpdate"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ColorField3DTex ("Color Field Texture", 2D) = "white" {}
		_PositionBufferTex ("Position Texture", 2D) = "white" {}
		_DeltaTime("Delta Time", float) = 1.0
		_BaseTime("Base Time", float) = 1.0
		_FieldSize("Vector Field Size", float) = 1.0
	}
	SubShader
	{
		Cull Off
		Lighting Off
		ZWrite Off
		Blend One Zero
		Fog { Mode off }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _ColorField3DTex;
			sampler2D _PositionBufferTex;
			float4 _MainTex_ST;
			float _DeltaTime;
			float _BaseTime;
			float _FieldSize;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 position = tex2D(_PositionBufferTex, i.uv);
				float2 uv = float2((position.x + floor(position.z * _FieldSize)) / _FieldSize, position.y);
				float4 sampledFieldColor = tex2D(_ColorField3DTex, uv);
				float4 particleColor = tex2D(_MainTex, i.uv);

				return float4(lerp(particleColor.xyz, sampledFieldColor.xyz, particleColor.a * _DeltaTime), particleColor.a);
			}
			ENDCG
		}
	}
}
