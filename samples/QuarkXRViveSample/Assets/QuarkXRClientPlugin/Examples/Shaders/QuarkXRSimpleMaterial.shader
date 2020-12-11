Shader "QuarkXR/SimpleMaterial"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent+2000" "RenderType" = "Opaque"}
		LOD 100

		Cull off
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off

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
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.y = 1 - o.uv.y;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 result = tex2D(_MainTex, i.uv);
				result.a = 1.0;
				return result;
			}
			ENDCG
		}
	}
}
