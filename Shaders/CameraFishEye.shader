// Upgrade NOTE: upgraded instancing buffer 'CameraFishEye' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CameraFishEye"
{
	Properties
	{
		_MainTex("_MainTex", 2D) = "white" {}
		_Center("Center", Vector) = (0.5,0.5,0,0)
		_Center1("Center", Vector) = (0.5,0.5,0,0)
		[Toggle]_FishEyeEffect("FishEyeEffect", Float) = 1
		_ScaleCorrection("ScaleCorrection", Float) = 1
		[Toggle]_BicubicSampling("BicubicSampling", Float) = 0
		_HorizontalFOV("HorizontalFOV", Range( 0 , 180)) = 60
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Unlit keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _BicubicSampling;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _FishEyeEffect;
		float4 _MainTex_TexelSize;

		UNITY_INSTANCING_BUFFER_START(CameraFishEye)
			UNITY_DEFINE_INSTANCED_PROP(float2, _Center)
#define _Center_arr CameraFishEye
			UNITY_DEFINE_INSTANCED_PROP(float2, _Center1)
#define _Center1_arr CameraFishEye
			UNITY_DEFINE_INSTANCED_PROP(float, _ScaleCorrection)
#define _ScaleCorrection_arr CameraFishEye
			UNITY_DEFINE_INSTANCED_PROP(float, _HorizontalFOV)
#define _HorizontalFOV_arr CameraFishEye
		UNITY_INSTANCING_BUFFER_END(CameraFishEye)

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float2 MainTexCoordinates5 = uv_MainTex;
			float2 _Center_Instance = UNITY_ACCESS_INSTANCED_PROP(_Center_arr, _Center);
			float2 CenteredUV79 = ( MainTexCoordinates5 - _Center_Instance );
			float _ScaleCorrection_Instance = UNITY_ACCESS_INSTANCED_PROP(_ScaleCorrection_arr, _ScaleCorrection);
			float AspectRatioCorrectionX65 = ( _MainTex_TexelSize.y * _MainTex_TexelSize.z );
			float2 appendResult71 = (float2(( (CenteredUV79).x * AspectRatioCorrectionX65 ) , (CenteredUV79).y));
			float2 AspectRatioCorrectedUV83 = appendResult71;
			float _HorizontalFOV_Instance = UNITY_ACCESS_INSTANCED_PROP(_HorizontalFOV_arr, _HorizontalFOV);
			float FocalLength134 = ( tan( radians( ( _HorizontalFOV_Instance * 0.5 ) ) ) * 0.5 );
			float3 appendResult28 = (float3(AspectRatioCorrectedUV83 , FocalLength134));
			float3 normalizeResult44 = normalize( appendResult28 );
			float dotResult43 = dot( float3( 0,0,1 ) , normalizeResult44 );
			float DotProduct51 = dotResult43;
			float temp_output_152_0 = (CenteredUV79).x;
			float temp_output_153_0 = (CenteredUV79).y;
			float2 _Center1_Instance = UNITY_ACCESS_INSTANCED_PROP(_Center1_arr, _Center1);
			float2 LensDistortedUV87 = ( (CenteredUV79*( _ScaleCorrection_Instance * (( _FishEyeEffect )?( ( 1.0 / ( sqrt( ( ( 1.0 - ( temp_output_152_0 * temp_output_152_0 ) ) - ( temp_output_153_0 * temp_output_153_0 ) ) ) * FocalLength134 ) ) ):( DotProduct51 )) ) + 0.0) + _Center1_Instance );
			float localBicubicPrepare2_g3 = ( 0.0 );
			float2 Input_UV100_g3 = LensDistortedUV87;
			float2 UV2_g3 = Input_UV100_g3;
			float4 TexelSize2_g3 = _MainTex_TexelSize;
			float2 UV02_g3 = float2( 0,0 );
			float2 UV12_g3 = float2( 0,0 );
			float2 UV22_g3 = float2( 0,0 );
			float2 UV32_g3 = float2( 0,0 );
			float W02_g3 = 0;
			float W12_g3 = 0;
			{
			{
			 UV2_g3 = UV2_g3 * TexelSize2_g3.zw - 0.5;
			    float2 f = frac( UV2_g3 );
			    UV2_g3 -= f;
			    float4 xn = float4( 1.0, 2.0, 3.0, 4.0 ) - f.xxxx;
			    float4 yn = float4( 1.0, 2.0, 3.0, 4.0 ) - f.yyyy;
			    float4 xs = xn * xn * xn;
			    float4 ys = yn * yn * yn;
			    float3 xv = float3( xs.x, xs.y - 4.0 * xs.x, xs.z - 4.0 * xs.y + 6.0 * xs.x );
			    float3 yv = float3( ys.x, ys.y - 4.0 * ys.x, ys.z - 4.0 * ys.y + 6.0 * ys.x );
			    float4 xc = float4( xv.xyz, 6.0 - xv.x - xv.y - xv.z );
			 float4 yc = float4( yv.xyz, 6.0 - yv.x - yv.y - yv.z );
			    float4 c = float4( UV2_g3.x - 0.5, UV2_g3.x + 1.5, UV2_g3.y - 0.5, UV2_g3.y + 1.5 );
			    float4 s = float4( xc.x + xc.y, xc.z + xc.w, yc.x + yc.y, yc.z + yc.w );
			    float4 off = ( c + float4( xc.y, xc.w, yc.y, yc.w ) / s ) * TexelSize2_g3.xyxy;
			    UV02_g3 = off.xz;
			    UV12_g3 = off.yz;
			    UV22_g3 = off.xw;
			    UV32_g3 = off.yw;
			    W02_g3 = s.x / ( s.x + s.y );
			 W12_g3 = s.z / ( s.z + s.w );
			}
			}
			float4 lerpResult46_g3 = lerp( tex2D( _MainTex, UV32_g3 ) , tex2D( _MainTex, UV22_g3 ) , W02_g3);
			float4 lerpResult45_g3 = lerp( tex2D( _MainTex, UV12_g3 ) , tex2D( _MainTex, UV02_g3 ) , W02_g3);
			float4 lerpResult44_g3 = lerp( lerpResult46_g3 , lerpResult45_g3 , W12_g3);
			float4 Output_2D131_g3 = lerpResult44_g3;
			o.Emission = (( _BicubicSampling )?( Output_2D131_g3 ):( tex2D( _MainTex, LensDistortedUV87 ) )).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Standard"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
199;336;2207;946;3854.375;-357.5188;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;97;-1126.026,-681.6432;Inherit;False;666.6757;316.2967;MainTex;3;2;95;96;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode;2;-1076.026,-595.3465;Inherit;True;Property;_MainTex;_MainTex;0;0;Create;True;0;0;0;False;0;False;19555d7d9d114c7f1100f5ab44295342;ffeed14977dbaaa419a8c6960c5a5b52;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.CommentaryNode;99;-1206.841,-324.0723;Inherit;False;851.0886;214.3739;MainTexUVRaw;3;98;5;4;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;95;-704.3508,-631.6432;Inherit;False;MainText_Tex;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;98;-1156.841,-269.9885;Inherit;False;95;MainText_Tex;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-893.2347,-270.6984;Inherit;False;0;2;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;5;-640.7527,-274.0723;Inherit;False;MainTexCoordinates;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;82;-2475.927,-693.5129;Inherit;False;786.3761;406.3005;Centered UV;4;35;8;79;29;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;101;-1262.157,-74.91099;Inherit;False;972.0819;253.3117;MainTex Aspect ratio;6;64;65;63;100;107;108;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector2Node;29;-2355.656,-448.2126;Inherit;False;InstancedProperty;_Center;Center;1;0;Create;True;0;0;0;True;0;False;0.5,0.5;0.5,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;100;-1212.157,-24.91099;Inherit;False;95;MainText_Tex;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;8;-2425.927,-643.513;Inherit;True;5;MainTexCoordinates;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexelSizeNode;63;-971.2494,-23.59929;Inherit;False;-1;1;0;SAMPLER2D;;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;35;-2104.941,-538.5182;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;135;-2579.49,-1224.641;Inherit;False;1053.528;189.9999;FocalLength;6;134;130;126;128;133;132;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;128;-2570.253,-1172.495;Inherit;False;InstancedProperty;_HorizontalFOV;HorizontalFOV;6;0;Create;True;0;0;0;False;0;False;60;170;0;180;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;84;-2699.375,-234.7904;Inherit;False;1284.063;379.1704;AspectRatioCorrectedUV;7;67;72;80;68;73;71;83;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;64;-745.0756,-26.97971;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;79;-1932.551,-542.4523;Inherit;False;CenteredUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;65;-579.0756,-30.97971;Float;False;AspectRatioCorrectionX;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;80;-2673.375,-186.349;Inherit;False;79;CenteredUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;132;-2307.545,-1169.641;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;67;-2308.722,-180.6393;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RadiansOpNode;130;-2156.963,-1166.423;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;73;-2359.116,-86.69198;Inherit;False;65;AspectRatioCorrectionX;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TanOpNode;126;-2018.163,-1167.405;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;68;-2288.187,13.70556;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-2054.917,-145.2922;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;133;-1894.764,-1166.568;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;71;-1902.879,-38.62906;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;150;-4744.139,928.8657;Inherit;False;79;CenteredUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;152;-4466.005,921.4489;Inherit;False;True;False;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;134;-1752.872,-1173.994;Inherit;False;FocalLength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;86;-2682.91,183.5961;Inherit;False;1279.682;306.2246;DotProduct from center;6;28;44;43;51;85;136;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;83;-1724.312,-44.15677;Inherit;False;AspectRatioCorrectedUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;154;-4181.691,920.2126;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;-4176.746,821.321;Inherit;False;Constant;_Float0;Float 0;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;153;-4467.241,993.1453;Inherit;False;False;True;True;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;85;-2632.91,291.7104;Inherit;False;83;AspectRatioCorrectedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;136;-2570.85,392.2993;Inherit;False;134;FocalLength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;28;-2265.515,304.4493;Inherit;False;FLOAT3;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;-4176.747,1007.979;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;156;-4019.756,853.4609;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;158;-3886.251,939.9915;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;44;-2088.757,304.4249;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SqrtOpNode;151;-3718.136,935.0464;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;162;-3677.375,1066.519;Inherit;False;134;FocalLength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;43;-1882.93,235.2507;Inherit;True;2;0;FLOAT3;0,0,1;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;161;-3433.375,971.5188;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;159;-3362.375,862.5188;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;88;-2943.563,671.0936;Inherit;False;1854.753;394.8474;Lens distorted UV;9;52;61;54;59;81;46;74;87;92;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-1645.227,233.5961;Inherit;False;DotProduct;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;160;-3160.375,898.5188;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;52;-2927.091,784.3582;Inherit;False;51;DotProduct;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;54;-2472.763,817.3767;Inherit;False;Property;_FishEyeEffect;FishEyeEffect;3;0;Create;True;0;0;0;False;0;False;1;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;59;-2635.928,717.9676;Inherit;False;InstancedProperty;_ScaleCorrection;ScaleCorrection;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;61;-2197.069,791.9739;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;81;-2405.737,713.0936;Inherit;False;79;CenteredUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;46;-1773.101,773.1536;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;74;-1751.43,904.9409;Inherit;False;InstancedProperty;_Center1;Center;2;0;Fetch;False;0;0;0;False;0;False;0.5,0.5;0.5,0.5;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleAddOpNode;92;-1519.665,831.4633;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;96;-709.1121,-526.9031;Inherit;False;MainText_SS;-1;True;1;0;SAMPLERSTATE;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;87;-1348.81,825.7947;Inherit;True;LensDistortedUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;103;240.9174,-106.2411;Inherit;False;95;MainText_Tex;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;104;264.416,343.3688;Inherit;False;96;MainText_SS;1;0;OBJECT;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.GetLocalVarNode;106;257.416,203.3688;Inherit;False;95;MainText_Tex;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;102;254.9174,35.75887;Inherit;False;96;MainText_SS;1;0;OBJECT;;False;1;SAMPLERSTATE;0
Node;AmplifyShaderEditor.GetLocalVarNode;89;228.4217,-35.07095;Inherit;False;87;LensDistortedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;105;242.9203,272.539;Inherit;False;87;LensDistortedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;78;647.6955,-87.50877;Inherit;True;Property;_TextureSample0;Texture Sample 0;6;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;21;676.0538,192.1137;Inherit;True;Bicubic Sample;-1;;3;ce0e14d5ad5eac645b2e5892ab3506ff;2,92,0,72,0;7;99;SAMPLER2D;_Sampler9921;False;91;SAMPLER2DARRAY;0;False;93;FLOAT;0;False;97;FLOAT2;0,0;False;198;FLOAT4;0,0,0,0;False;199;FLOAT2;0,0;False;94;SAMPLERSTATE;0;False;5;COLOR;86;FLOAT;84;FLOAT;85;FLOAT;82;FLOAT;83
Node;AmplifyShaderEditor.DynamicAppendNode;140;-2241.927,-941.7833;Inherit;False;FLOAT4;4;0;FLOAT;0.5;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;107;-576.3477,67.71271;Float;False;AspectRatioCorrectionY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;90;1012.766,24.0455;Inherit;False;Property;_BicubicSampling;BicubicSampling;5;0;Create;True;0;0;0;False;0;False;0;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LengthOpNode;138;-2026.317,-942.8561;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;108;-751.3477,75.71271;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;143;-1812.015,-945.2203;Inherit;False;UVSizeCorrection;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;137;-2574.464,-901.0211;Inherit;False;134;FocalLength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1361,-153;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;CameraFishEye;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;Standard;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;95;0;2;0
WireConnection;4;2;98;0
WireConnection;5;0;4;0
WireConnection;63;0;100;0
WireConnection;35;0;8;0
WireConnection;35;1;29;0
WireConnection;64;0;63;2
WireConnection;64;1;63;3
WireConnection;79;0;35;0
WireConnection;65;0;64;0
WireConnection;132;0;128;0
WireConnection;67;0;80;0
WireConnection;130;0;132;0
WireConnection;126;0;130;0
WireConnection;68;0;80;0
WireConnection;72;0;67;0
WireConnection;72;1;73;0
WireConnection;133;0;126;0
WireConnection;71;0;72;0
WireConnection;71;1;68;0
WireConnection;152;0;150;0
WireConnection;134;0;133;0
WireConnection;83;0;71;0
WireConnection;154;0;152;0
WireConnection;154;1;152;0
WireConnection;153;0;150;0
WireConnection;28;0;85;0
WireConnection;28;2;136;0
WireConnection;155;0;153;0
WireConnection;155;1;153;0
WireConnection;156;0;157;0
WireConnection;156;1;154;0
WireConnection;158;0;156;0
WireConnection;158;1;155;0
WireConnection;44;0;28;0
WireConnection;151;0;158;0
WireConnection;43;1;44;0
WireConnection;161;0;151;0
WireConnection;161;1;162;0
WireConnection;51;0;43;0
WireConnection;160;0;159;0
WireConnection;160;1;161;0
WireConnection;54;0;52;0
WireConnection;54;1;160;0
WireConnection;61;0;59;0
WireConnection;61;1;54;0
WireConnection;46;0;81;0
WireConnection;46;1;61;0
WireConnection;92;0;46;0
WireConnection;92;1;74;0
WireConnection;96;0;2;1
WireConnection;87;0;92;0
WireConnection;78;0;103;0
WireConnection;78;1;89;0
WireConnection;78;7;102;0
WireConnection;21;99;106;0
WireConnection;21;97;105;0
WireConnection;21;94;104;0
WireConnection;140;2;137;0
WireConnection;107;0;108;0
WireConnection;90;0;78;0
WireConnection;90;1;21;86
WireConnection;138;0;140;0
WireConnection;108;0;63;1
WireConnection;108;1;63;4
WireConnection;143;0;138;0
WireConnection;0;2;90;0
ASEEND*/
//CHKSM=8304F8ADFEE4555C082273B5AF81A2D54AF1A4BC