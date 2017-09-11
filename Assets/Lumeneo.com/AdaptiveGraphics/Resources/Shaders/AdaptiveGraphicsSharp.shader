Shader "AdaptiveGraphics/AdaptiveGraphics Sharp" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

Subshader {	

   Pass {
	  ZTest Always Cull Off ZWrite Off
	  Fog { Mode Off }
	  
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma fragmentoption ARB_precision_hint_fastest
      #define SHARPEN
      #include "AdaptiveGraphics.cginc"
      ENDCG
  }

}
FallBack Off
}
