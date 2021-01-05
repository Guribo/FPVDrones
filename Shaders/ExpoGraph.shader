// Upgrade NOTE: upgraded instancing buffer 'GuriboFPVDronesUIExpoGraph' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Guribo/FPVDrones/UI/ExpoGraph"
{
	Properties
	{
		_MainColor("MainColor", Color) = (0,0,0,0)
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Expo("Expo", Range( 0 , 2)) = 0
		_AxisInput("AxisInput", Range( -1 , 1)) = 0
		_InputWidth("InputWidth", Range( 0 , 1)) = 0
		_AxisColor("AxisColor", Color) = (0,0,0,0)
		_InputRemap("InputRemap", Vector) = (0,0,0,0)
		_MaxRate("MaxRate", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Unlit keepalpha noshadow 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(GuriboFPVDronesUIExpoGraph)
			UNITY_DEFINE_INSTANCED_PROP(float4, _AxisColor)
#define _AxisColor_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
#define _MainColor_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float2, _InputRemap)
#define _InputRemap_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float, _AxisInput)
#define _AxisInput_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float, _InputWidth)
#define _InputWidth_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float, _Expo)
#define _Expo_arr GuriboFPVDronesUIExpoGraph
			UNITY_DEFINE_INSTANCED_PROP(float, _MaxRate)
#define _MaxRate_arr GuriboFPVDronesUIExpoGraph
		UNITY_INSTANCING_BUFFER_END(GuriboFPVDronesUIExpoGraph)

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 _InputRemap_Instance = UNITY_ACCESS_INSTANCED_PROP(_InputRemap_arr, _InputRemap);
			float temp_output_24_0 = (_InputRemap_Instance.x + (i.uv_texcoord.x - 0.0) * (_InputRemap_Instance.y - _InputRemap_Instance.x) / (1.0 - 0.0));
			float _AxisInput_Instance = UNITY_ACCESS_INSTANCED_PROP(_AxisInput_arr, _AxisInput);
			float _InputWidth_Instance = UNITY_ACCESS_INSTANCED_PROP(_InputWidth_arr, _InputWidth);
			float temp_output_38_0 = ( _InputWidth_Instance * 0.5 );
			float temp_output_40_0 = ( _AxisInput_Instance - temp_output_38_0 );
			float temp_output_41_0 = ( _AxisInput_Instance + temp_output_38_0 );
			float4 _AxisColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_AxisColor_arr, _AxisColor);
			float4 _MainColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_MainColor_arr, _MainColor);
			o.Emission = (( temp_output_24_0 >= temp_output_40_0 && temp_output_24_0 <= temp_output_41_0 ) ? _AxisColor_Instance :  _MainColor_Instance ).rgb;
			o.Alpha = 1;
			float _Expo_Instance = UNITY_ACCESS_INSTANCED_PROP(_Expo_arr, _Expo);
			float _MaxRate_Instance = UNITY_ACCESS_INSTANCED_PROP(_MaxRate_arr, _MaxRate);
			clip( (( temp_output_24_0 >= temp_output_40_0 && temp_output_24_0 <= temp_output_41_0 ) ? 1.0 :  ( ( abs( ( ( ( 1.0 - _Expo_Instance ) * temp_output_24_0 * temp_output_24_0 * temp_output_24_0 ) + ( _Expo_Instance * temp_output_24_0 ) ) ) * _MaxRate_Instance ) > i.uv_texcoord.y ? 1.0 : 0.0 ) ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Unlit/Color"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
63;445;1451;753;1354.406;141.4957;1;True;True
Node;AmplifyShaderEditor.RangedFloatNode;46;-1651.27,332.8383;Inherit;False;Constant;_Float3;Float 3;7;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;3;-1498.448,179.9284;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;49;-1800.641,457.2174;Inherit;False;InstancedProperty;_InputRemap;InputRemap;6;0;Create;True;0;0;0;False;0;False;0,0;-1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;25;-1831.948,291.5131;Inherit;False;Constant;_Float2;Float 2;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-1253.818,-109.0758;Inherit;False;InstancedProperty;_Expo;Expo;2;0;Create;True;0;0;0;False;0;False;0;0.2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;5;-870.8177,7.924176;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;24;-1473.649,321.8775;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-628.8546,259.9595;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-636.8177,24.92418;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-447.7235,189.6664;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.AbsOpNode;33;-349.5781,281.1964;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-594.9891,394.6505;Inherit;False;InstancedProperty;_MaxRate;MaxRate;7;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-2100.626,-273.2391;Inherit;False;InstancedProperty;_InputWidth;InputWidth;4;0;Create;True;0;0;0;False;0;False;0;0.025;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-1991.396,-190.2375;Inherit;False;Constant;_Float5;Float 5;5;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-287.0457,399.0336;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-1803.096,-262.0259;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-709.6528,629.8688;Inherit;False;Constant;_Float1;Float 1;3;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;34;-2083.281,-446.5087;Inherit;False;InstancedProperty;_AxisInput;AxisInput;3;0;Create;True;0;0;0;False;0;False;0;0.43;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-714.2077,517.52;Inherit;False;Constant;_Float0;Float 0;3;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;-1644.731,-307.6658;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;40;-1660.111,-417.5194;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-302.6625,74.36633;Inherit;False;Constant;_Float6;Float 6;6;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;42;-1121.828,-418.6179;Inherit;False;InstancedProperty;_AxisColor;AxisColor;5;0;Create;True;0;0;0;False;0;False;0,0,0,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;1;-881.8177,-185.0758;Inherit;False;InstancedProperty;_MainColor;MainColor;0;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,1,0.04155419,0.01568628;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Compare;20;-50.48621,318.1241;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareWithRange;37;-554.7426,-394.9144;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCCompareWithRange;43;-75.22176,40.09927;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;188.8826,-117.3644;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Guribo/FPVDrones/UI/ExpoGraph;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;True;TransparentCutout;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;Unlit/Color;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;5;0;6;0
WireConnection;24;0;3;1
WireConnection;24;1;25;0
WireConnection;24;2;46;0
WireConnection;24;3;49;1
WireConnection;24;4;49;2
WireConnection;10;0;6;0
WireConnection;10;1;24;0
WireConnection;8;0;5;0
WireConnection;8;1;24;0
WireConnection;8;2;24;0
WireConnection;8;3;24;0
WireConnection;9;0;8;0
WireConnection;9;1;10;0
WireConnection;33;0;9;0
WireConnection;51;0;33;0
WireConnection;51;1;50;0
WireConnection;38;0;35;0
WireConnection;38;1;39;0
WireConnection;41;0;34;0
WireConnection;41;1;38;0
WireConnection;40;0;34;0
WireConnection;40;1;38;0
WireConnection;20;0;51;0
WireConnection;20;1;3;2
WireConnection;20;2;21;0
WireConnection;20;3;22;0
WireConnection;37;0;24;0
WireConnection;37;1;40;0
WireConnection;37;2;41;0
WireConnection;37;3;42;0
WireConnection;37;4;1;0
WireConnection;43;0;24;0
WireConnection;43;1;40;0
WireConnection;43;2;41;0
WireConnection;43;3;44;0
WireConnection;43;4;20;0
WireConnection;0;2;37;0
WireConnection;0;10;43;0
ASEEND*/
//CHKSM=A8FB3236C7F0CBCDB69AD4013FC83E5C486DDCD0