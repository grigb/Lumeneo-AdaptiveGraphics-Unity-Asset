// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	#include "UnityCG.cginc"

	uniform sampler2D       _MainTex;
	uniform half4 			_MainTex_TexelSize;


	#ifdef UNITY_SINGLE_PASS_STEREO
	uniform half4			_MainTex_ST;
	#endif

    struct appdata {
    	half4 vertex : POSITION;
		half2 texcoord : TEXCOORD0;
    };
    
	struct v2f {
	    half4 pos  : SV_POSITION;
	    half2 uv   : TEXCOORD0;
	    #if defined(SHARPEN)
	    half2 uv1  : TEXCOORD1;
	    half2 uv2  : TEXCOORD2;
	    half2 uv3  : TEXCOORD3;
	    #endif
	};



	v2f vert(appdata v) {
    	v2f o;
    	o.pos = UnityObjectToClipPos(v.vertex);

    	#ifdef UNITY_SINGLE_PASS_STEREO
    	o.uv = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
    	#else
		o.uv = v.texcoord;
		#endif
		#if defined(SHARPEN)
		half3 inc       = half3(_MainTex_TexelSize.x, _MainTex_TexelSize.y, 0);
		o.uv1 = o.uv + inc.yz;
		o.uv2 = o.uv - inc.yz;
		o.uv3 = o.uv - inc.xz;
		#endif
    	return o;
	}



	#if defined(SHARPEN)		
	inline half getLuma(half4 rgb) { 
		const half4 lum = half4(0.299, 0.587, 0.114, 0);
		return dot(rgb, lum);
	}



	void fastSharpen(v2f i, inout half4 rgbM) {
	    half4 rgbN       = tex2D(_MainTex, i.uv1);
		half4 rgbS       = tex2D(_MainTex, i.uv2);
	    half4 rgbW       = tex2D(_MainTex, i.uv3);
	    half  lumaM      = getLuma(rgbM);
    	half  lumaN      = getLuma(rgbN);
    	half  lumaW      = getLuma(rgbW);
    	half  lumaS      = getLuma(rgbS);
    	half  maxLuma    = max(lumaN,lumaS);
    	      maxLuma    = max(maxLuma, lumaW);
	    half  minLuma    = min(lumaN,lumaS);
	          minLuma    = min(minLuma, lumaW);
	    half  lumaPower  = 2 * lumaM - minLuma - maxLuma;
		      rgbM.rgb  *= 1.0 + clamp(lumaPower, -0.01, 0.01) * 3.0;
	}


	#endif



	half4 frag(v2f i) : SV_Target {

		half4 pixel = tex2D(_MainTex, i.uv);
		#if defined(SHARPEN)
		fastSharpen(i, pixel);
		#endif
		return pixel;
	}

