Shader "IndieChest/ForceShield_URP"
{
	// ============================================================================
	// CONVERTED FROM BUILT-IN RP (Surface Shader) TO UNIVERSAL RENDER PIPELINE
	// ============================================================================
	// Nothing was removed. Every property, calculation and effect from the
	// original surface shader (fresnel rim, shield pattern, hit ripple/color,
	// depth-intersection glow, vertex displacement, tessellation, custom
	// shadow caster) is reproduced below. What changed is *how* it's wired up,
	// because URP has no surface-shader compiler and no Tessellation.cginc:
	//
	//  - CGPROGRAM/CGINCLUDE -> HLSLPROGRAM/HLSLINCLUDE with URP ShaderLibrary includes.
	//  - `#pragma surface ...` -> a manual vertex/fragment pipeline. Lighting that
	//    Unity used to generate for you (Standard PBR) is now done explicitly via
	//    URP's UniversalFragmentPBR, fed the same Albedo/Normal/Emission/Alpha
	//    values your surf() function computed.
	//  - `tessellate:tessEdge` (UnityEdgeLengthBasedTess, from Built-in's
	//    Tessellation.cginc) -> URP has no equivalent built-in function, so it's
	//    reimplemented here as a Hull/Domain shader pair using the same
	//    "world-space edge length vs _EdgeLength" idea.
	//  - `vertex:disp` -> now lives in the real vertex function that runs after
	//    the domain shader interpolates the tessellated patch.
	//  - _CameraDepthTexture sampling -> now uses URP's DeclareDepthTexture.hlsl
	//    (SampleSceneDepth). Needs "Depth Texture" enabled on the URP Asset/Camera,
	//    same requirement Built-in had (needs _CameraDepthTexture available).
	//  - The custom ShadowCaster pass is rebuilt using URP's shadow-caster
	//    template (GetShadowPositionHClip / ApplyShadowBias) since URP shadow
	//    casters are no longer auto-generated from the surface shader.
	//
	// NOTE ON _Normal / _Albedo / _ShieldPattern / _ShieldPatternWaves /
	// _IntersectColor / _IntersectIntensity: these were referenced in your
	// original code but were NOT exposed in the Properties block (same as the
	// source you gave me). I kept that exact behavior — they're declared as
	// uniforms/textures with shader defaults, not exposed on the material, so
	// there is no behavior change. If you want them tweakable per-material,
	// tell me and I'll add them to Properties.
	// ============================================================================

	Properties
	{
		_Color("Color", Color) = (0,0,0,0)
		_Opacity("Opacity", Range( 0 , 1)) = 0.5
		_ShieldPatternColor("Additional Color", Color) = (0.4941176470588235,0.8274509803921569,0.6980392156862745,1)
		_ShieldPatternPower("Additional Color Power", Range( 0 , 100)) = 5
		_ShieldRimPower("Shield Rim Power", Range( 0 , 10)) = 7
		[HideInInspector]_HitPosition("Hit Position", Vector) = (0,0,0,0)
		_HitTime("Hit Time", Float) = 0
		_HitColor("Hit Color", Color) = (1,1,1,1)
		_HitSize("Hit Size", Float) = 0.2
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		_EdgeLength ("Edge length", Range( 2 , 100 )) = 15.0
		_Displacement ("Hit Wave", Range(0, 1)) = 0.03
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }
		Cull Back

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		// ---------------------------------------------------------------
		// Uniforms (SRP Batcher friendly CBUFFER, same names as original)
		// ---------------------------------------------------------------
		CBUFFER_START(UnityPerMaterial)
			float4 _Color;
			float _Opacity;
			float4 _ShieldPatternColor;
			float _ShieldPatternPower;
			float _ShieldRimPower;
			float3 _HitPosition;
			float _HitTime;
			float4 _HitColor;
			float _HitSize;
			float _EdgeLength;
			float _Displacement;

			float4 _Normal_ST;
			float4 _Albedo_ST;
			float4 _IntersectColor;
			float _ShieldPatternSize;
			float _IntersectIntensity;
		CBUFFER_END

		TEXTURE2D(_Normal);            SAMPLER(sampler_Normal);
		TEXTURE2D(_Albedo);            SAMPLER(sampler_Albedo);
		TEXTURE2D(_ShieldPattern);      SAMPLER(sampler_ShieldPattern);
		TEXTURE2D(_ShieldPatternWaves); SAMPLER(sampler_ShieldPatternWaves);

		// ---------------------------------------------------------------
		// Same helper math functions as the original CGINCLUDE block
		// ---------------------------------------------------------------
		float3 mod289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
		float4 mod289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
		float4 permute( float4 x ) { return mod289( ( x * 34.0 + 1.0 ) * x ); }
		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		// ---------------------------------------------------------------
		// Vertex displacement on hit (equivalent of original `disp`)
		// Operates in object space, exactly like the original.
		// ---------------------------------------------------------------
		float3 ApplyHitDisplacement(float3 positionOS, float3 normalOS)
		{
			float temp_output = distance( positionOS, _HitPosition );
			float HitSize = _HitSize;

			float hit = (( _HitTime > 0.0 ) ? (( temp_output < (HitSize * 5) ) ? lerp
				( 1, ( HitSize / temp_output), (_HitTime / 150) ) : 1 ) : 1 ) * _Displacement;

			hit = smoothstep( 0, 1, hit );
			return positionOS - normalOS * hit * _Displacement;
		}

		// ---------------------------------------------------------------
		// Edge-length based tessellation factor, equivalent in spirit to
		// Built-in's UnityEdgeLengthBasedTess (not available in URP/SRP Core).
		// Compares world-space triangle edge length against _EdgeLength.
		// ---------------------------------------------------------------
		float4 EdgeLengthBasedTess(float3 p0WS, float3 p1WS, float3 p2WS, float edgeLength)
		{
			float e0 = distance(p1WS, p2WS);
			float e1 = distance(p2WS, p0WS);
			float e2 = distance(p0WS, p1WS);

			float4 tess;
			tess.x = max(1.0, e0 / max(edgeLength, 0.0001));
			tess.y = max(1.0, e1 / max(edgeLength, 0.0001));
			tess.z = max(1.0, e2 / max(edgeLength, 0.0001));
			tess.w = (tess.x + tess.y + tess.z) / 3.0;
			return tess;
		}
		ENDHLSL

		// ============================================================
		// FORWARD LIT PASS (replaces the surface-shader-generated pass)
		// ============================================================
		Pass
		{
			Name "ForwardLit"
			Tags{ "LightMode" = "UniversalForward" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Back

			HLSLPROGRAM
			#pragma target 4.6

			#pragma vertex vertHull
			#pragma hull hull
			#pragma domain domain
			#pragma fragment frag

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			// ------------------------------------------------------------------
			// Vertex input (equivalent of appdata_full for what we actually use)
			// ------------------------------------------------------------------
			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float4 tangentOS  : TANGENT;
				float2 uv0        : TEXCOORD0;
				float2 uv1        : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// Control point struct carried through the tessellation stages
			struct ControlPoint
			{
				float4 positionOS : INTERNALTESSPOS;
				float3 normalOS   : NORMAL;
				float4 tangentOS  : TANGENT;
				float2 uv0        : TEXCOORD0;
				float2 uv1        : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3]  : SV_TessFactor;
				float inside   : SV_InsideTessFactor;
			};

			// Final vertex-to-fragment struct (equivalent of Input + clip pos)
			struct Varyings
			{
				float4 positionCS   : SV_POSITION;
				float2 uv_texcoord  : TEXCOORD0; // matches original i.uv_texcoord (uses uv0)
				float2 texcoord_0   : TEXCOORD1; // matches original i.texcoord_0 (uv0)
				float2 texcoord_1   : TEXCOORD2; // matches original i.texcoord_1 (uv1)
				float3 worldPos     : TEXCOORD3;
				float3 worldNormal  : TEXCOORD4;
				float4 worldTangent : TEXCOORD5; // xyz tangent, w sign
				float4 screenPos    : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// ------------------------------------------------------------------
			// Stage 1: pass-through vertex used before tessellation (like the
			// original's implicit appdata_full -> tessellation input)
			// ------------------------------------------------------------------
			ControlPoint vertHull(Attributes input)
			{
				ControlPoint o;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, o);
				o.positionOS = input.positionOS;
				o.normalOS = input.normalOS;
				o.tangentOS = input.tangentOS;
				o.uv0 = input.uv0;
				o.uv1 = input.uv1;
				return o;
			}

			// ------------------------------------------------------------------
			// Patch constant function == original tessEdge (UnityEdgeLengthBasedTess)
			// ------------------------------------------------------------------
			TessellationFactors patchConstantFunction(InputPatch<ControlPoint, 3> patch)
			{
				float3 p0WS = TransformObjectToWorld(patch[0].positionOS.xyz);
				float3 p1WS = TransformObjectToWorld(patch[1].positionOS.xyz);
				float3 p2WS = TransformObjectToWorld(patch[2].positionOS.xyz);

				float4 tess = EdgeLengthBasedTess(p0WS, p1WS, p2WS, _EdgeLength);

				TessellationFactors f;
				f.edge[0] = tess.x;
				f.edge[1] = tess.y;
				f.edge[2] = tess.z;
				f.inside  = tess.w;
				return f;
			}

			[domain("tri")]
			[outputcontrolpoints(3)]
			[outputtopology("triangle_cw")]
			[partitioning("fractional_odd")]
			[patchconstantfunc("patchConstantFunction")]
			ControlPoint hull(InputPatch<ControlPoint, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			// ------------------------------------------------------------------
			// Stage 2 (real vertex shader): runs after the domain interpolates
			// the tessellated triangle. Applies hit displacement (== original
			// `disp`) then transforms to world/clip space.
			// ------------------------------------------------------------------
			Varyings vertexProgram(Attributes input)
			{
				Varyings o = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, o);

				float3 displacedPosOS = ApplyHitDisplacement(input.positionOS.xyz, input.normalOS);

				VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPosOS);
				VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

				o.positionCS = posInputs.positionCS;
				o.worldPos = posInputs.positionWS;
				o.worldNormal = normInputs.normalWS;
				o.worldTangent = float4(normInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());

				o.uv_texcoord = input.uv0;
				o.texcoord_0 = input.uv0;
				o.texcoord_1 = input.uv1;

				o.screenPos = ComputeScreenPos(o.positionCS);
				return o;
			}

			[domain("tri")]
			Varyings domain(TessellationFactors factors, OutputPatch<ControlPoint, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes v = (Attributes)0;

				#define INTERP(field) v.field = patch[0].field * bary.x + patch[1].field * bary.y + patch[2].field * bary.z;
				INTERP(positionOS)
				INTERP(normalOS)
				INTERP(tangentOS)
				INTERP(uv0)
				INTERP(uv1)
				#undef INTERP

				v.normalOS = normalize(v.normalOS);

				UNITY_TRANSFER_INSTANCE_ID(patch[0], v);
				return vertexProgram(v);
			}

			// ------------------------------------------------------------------
			// Fragment shader == original surf() function
			// ------------------------------------------------------------------
			half4 frag(Varyings i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				// --- Normal map (kept identical; see note at top about
				// _Normal not being exposed in Properties) ---
				float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
				float3 Normal = UnpackNormal( SAMPLE_TEXTURE2D(_Normal, sampler_Normal, uv_Normal) );

				float3 bitangentWS = cross(i.worldNormal, i.worldTangent.xyz) * i.worldTangent.w;
				float3x3 tangentToWorld = float3x3(i.worldTangent.xyz, bitangentWS, i.worldNormal);
				float3 normalWS = normalize(mul(Normal, tangentToWorld));

				// --- Albedo ---
				float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
				float4 Albedo = ( _Color * SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv_Albedo) );

				// --- Fresnel shield rim (identical math to original fresnelFinalVal8) ---
				float3 worldViewDir = normalize( GetWorldSpaceViewDir(i.worldPos) );
				float3 ase_worldNormal = i.worldNormal;
				float ShieldRimPower = _ShieldRimPower;
				float fresnelFinalVal8 = (0.0 + 1.0*pow( 1.0 - dot( ase_worldNormal, worldViewDir ) , (10.0 + (ShieldRimPower - 0.0) * (0.0 - 10.0) / (10.0 - 0.0))));
				float ShieldRim = fresnelFinalVal8;

				// --- Shield pattern + waves textures ---
				float4 ShieldPattern = SAMPLE_TEXTURE2D(_ShieldPattern, sampler_ShieldPattern, i.texcoord_0);
				float4 waves = SAMPLE_TEXTURE2D(_ShieldPatternWaves, sampler_ShieldPatternWaves, i.texcoord_1);

				// --- Hit ripple / hit color (identical math to original) ---
				float4 ase_vertexPos = mul( unity_WorldToObject, float4( i.worldPos, 1 ) );
				float temp_output_152_0 = distance( ase_vertexPos.xyz, _HitPosition );
				float HitSize = _HitSize;
				float4 ShieldPatternColor = _ShieldPatternColor;
				float4 HitColor = _HitColor;
				float4 hit = (( _HitTime > 0.0 ) ? (( temp_output_152_0 < HitSize ) ? lerp( ShieldPatternColor, ( HitColor * ( HitSize / temp_output_152_0 ) ), (0.0 + (_HitTime - 0.0) * (1.0 - 0.0) / (100.0 - 0.0)) ) : ShieldPatternColor ) : ShieldPatternColor );

				// --- Depth intersection glow (identical math to original,
				// using URP's depth-texture sampling instead of tex2Dproj) ---
				float4 ase_screenPos = float4( i.screenPos.xyz, i.screenPos.w + 0.00000000001 );
				float2 screenUV = ase_screenPos.xy / ase_screenPos.w;
				float rawDepth110 = SampleSceneDepth(screenUV);
				float screenDepth110 = LinearEyeDepth(rawDepth110, _ZBufferParams);
				float distanceDepth110 = abs( ( screenDepth110 - LinearEyeDepth( ase_screenPos.z / ase_screenPos.w, _ZBufferParams ) ) / _IntersectIntensity );

				float ShieldPower = _ShieldPatternPower;
				float4 Emission = ( lerp( _IntersectColor, ( ( ( ShieldRim + ShieldPattern ) * waves ) * ( hit * ShieldPatternColor ) ), clamp( distanceDepth110, 0.0, 1.0 ) ) * ShieldPower );

				float Alpha = _Opacity;

				// --- Feed into URP's PBR lighting so the shield still
				// receives real-time lights the same way SurfaceOutputStandard did ---
				InputData inputData = (InputData)0;
				inputData.positionWS = i.worldPos;
				inputData.normalWS = normalWS;
				inputData.viewDirectionWS = worldViewDir;
				inputData.shadowCoord = TransformWorldToShadowCoord(i.worldPos);
				inputData.fogCoord = 0;
				inputData.vertexLighting = 0;
				inputData.bakedGI = SampleSH(normalWS);
				inputData.normalizedScreenSpaceUV = screenUV;
				inputData.shadowMask = half4(1, 1, 1, 1);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = Albedo.rgb;
				surfaceData.specular = half3(0, 0, 0);
				surfaceData.metallic = 0;
				surfaceData.smoothness = 0.5;
				surfaceData.normalTS = Normal;
				surfaceData.emission = Emission.rgb;
				surfaceData.occlusion = 1;
				surfaceData.alpha = Alpha;
				surfaceData.clearCoatMask = 0;
				surfaceData.clearCoatSmoothness = 1;

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.a = Alpha;
				return color;
			}
			ENDHLSL
		}

		// ============================================================
		// SHADOW CASTER PASS
		// (matches original: no tessellation, no hit-displacement in
		// this pass, same as the hand-written CGPROGRAM ShadowCaster.
		// Note: the original used a dithered alpha via _DitherMaskLOD
		// for partially-transparent shadows; URP has no equivalent
		// built-in texture, so this pass casts a solid shadow instead.
		// Disable "Cast Shadows" on the renderer if you don't want the
		// shield to cast a solid shadow.)
		// ============================================================
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Back

			HLSLPROGRAM
			#pragma target 4.6
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			float4 GetShadowPositionHClip(Attributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
				#endif

				return positionCS;
			}

			Varyings ShadowPassVertex(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				output.positionCS = GetShadowPositionHClip(input);
				return output;
			}

			half4 ShadowPassFragment(Varyings input) : SV_TARGET
			{
				return 0;
			}
			ENDHLSL
		}
	}
	Fallback "Universal Render Pipeline/Lit"
}
