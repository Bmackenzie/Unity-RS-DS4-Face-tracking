Shader "Custom/TranspaentOccluder"
{
	Properties
	{
		thickness ("Thickness", Float) = 0.15
	}

	SubShader {
		Tags { "Queue" = "Geometry+1"}

		Pass
		{
			// Writes to Z - Buffer
			ZWrite On
			// Blend = 1 - alpha
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag 

			uniform float thickness;

			struct VertexOut {
				float4 coords 	: SV_POSITION;
				float4 texCord 	: TEXCOORD0;
			};
			
			VertexOut vert(float4 vertex: POSITION)
			{
				VertexOut outVert;
				outVert.coords  = mul(UNITY_MATRIX_MVP, vertex);
				outVert.texCord = mul(_Object2World, vertex);
				return outVert;
			}

			float4 frag(VertexOut vOut) : COLOR
			{
			    float space=0.02;			    
				if ((frac(vOut.texCord.x/space) < thickness) || (frac(vOut.texCord.y/space) < thickness))
					return float4(0.5, 1.0, 1.0, 0.5); 
				return float4(0,0,0,0);
			}

			ENDCG
		}
	}
}