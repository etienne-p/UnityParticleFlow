Shader "Unlit/ParticleFlow/VelocityUpdate"
{
	Properties
	{
		_MainTex 			("Texture", 2D) 				= "white" {}
		_VelocityLookupTex 	("Velocity Lookup Texture", 2D) = "white" {}
		_DeltaTime			("Delta Time", float) 			= 1.0
		_BaseTime			("Base Time", float) 			= 1.0
		_AgingFactor		("Aging Factor", float) 		= 1.0
		_FieldSize			("Vector Field Size", float) 	= 1.0
		_MobilityThreshold	("Mobility Threshold", float) 	= 1.0
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
			sampler2D _VelocityLookupTex;
			float4 _MainTex_ST;
			float _DeltaTime;
			float _BaseTime;
			float _FieldSize;
			float _AgingFactor;
			float _MobilityThreshold;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float nrand(float2 uv)
			{
			    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
			}

			float4 frag (v2f i) : SV_Target
			{
				float4 position = tex2Dlod(_MainTex, float4(i.uv, .0, .0));

				float2 uv = float2((position.x + floor(position.z * _FieldSize)) / _FieldSize, position.y);
				float4 velocity = tex2Dlod(_VelocityLookupTex, float4(uv, .0, .0));
				velocity = (velocity - float4(.5, .5, .5, .0)) * 2.0;

				float4 updatedPosition = position + velocity * _DeltaTime;

				// went out of bounds?
				float outOfBoundsFactor = 
					max(.0, updatedPosition.x) *
					max(.0, updatedPosition.y) * 
					max(.0, updatedPosition.z) * 
					(1.0 - min(updatedPosition.x, 1.0)) *
					(1.0 - min(updatedPosition.y, 1.0)) *
					(1.0 - min(updatedPosition.z, 1.0));

				// or is dead?
				float alpha = max(position.a - _DeltaTime * _AgingFactor, .0);

				// or is immobile?
				float motion = max(length(velocity.xyz) - _MobilityThreshold, .0);

				if (outOfBoundsFactor * alpha * motion == .0)
				{
					updatedPosition.x = nrand(float2(updatedPosition.x, _BaseTime));
					updatedPosition.y = nrand(float2(updatedPosition.y, _BaseTime));
					updatedPosition.z = nrand(float2(updatedPosition.z, _BaseTime));
					alpha = frac(length(updatedPosition) * 10.0);
				}

				return float4(updatedPosition.xyz, alpha);
			}
			ENDCG
		}
	}
}
