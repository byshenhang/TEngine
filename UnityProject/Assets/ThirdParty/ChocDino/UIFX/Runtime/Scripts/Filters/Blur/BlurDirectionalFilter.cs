//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum BlurDirectionalBlend
	{
		Replace,
		Behind,
		Over,
		Additive,
	}

	public enum BlurDirectionalWeighting
	{
		Linear,
		Falloff,
	}

	public enum BlurDirectionalSide
	{
		One,
		Both,
	}

	/// <summary>
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Blur Directional Filter")]
	public class BlurDirectionalFilter : FilterBase
	{
		[Tooltip("The clockwise angle. Range is [0..360]. Default is 135.0")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 135f;

		[Tooltip("The length of the blur in pixels. Range is [-512..512]. Default is 32.0")]
		[Range(-512f, 512f)]
		[SerializeField] float _length = 32f;

		[Tooltip("The type of weights to use for the blur, this changes the visual appearance with Falloff looking higher quality, for little extra cost.")]
		[SerializeField] BlurDirectionalWeighting _weights = BlurDirectionalWeighting.Falloff;

		[Tooltip("")]
		[SerializeField] BlurDirectionalSide _side = BlurDirectionalSide.Both;

		[Tooltip("The amount of dithering to apply, useful to hide banding artifacts and also for styling. Range is [0..1]. Default is 0.0")]
		[Range(0f, 1f)]
		[SerializeField] float _dither = 0f;

		[Tooltip("Toggle the use of the alpha curve to fade to transparent as Strength increases.")]
		[SerializeField] bool _applyAlphaCurve = false;

		[Tooltip("An optional curve to allow the Graphic to fade to transparent as the Strength property increases.")]
		[SerializeField] AnimationCurve _alphaCurve = new AnimationCurve(new Keyframe(0f, 1f, -1f, -1f), new Keyframe(1f, 0f, -1f, -1f));

		[Tooltip("Tint (multiply) the blurred color by this for styling.")]
		[SerializeField] Color _tintColor = Color.white;

		[Tooltip("How the source graphic and the blurred graphic are blended/composited together.")]
		[SerializeField] BlurDirectionalBlend _blend = BlurDirectionalBlend.Replace;

		/// <summary>The clockwise angle. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary>The length of the blur in pixels. Range is [-256..256]. Default is 16.0</summary>
		public float Length { get { return _length; } set { ChangeProperty(ref _length, value); } }

		/// <summary>The type of weights to use for the blur, this changes the visual appearance with Falloff looking higher quality, for little extra cost.</summary>
		public BlurDirectionalWeighting Weights { get { return _weights; } set { ChangeProperty(ref _weights, value); } }

		/// <summary></summary>
		public BlurDirectionalSide Side { get { return _side; } set { ChangeProperty(ref _side, value); } }

		/// <summary>The amount of dithering to apply, useful to hide banding artifacts and also for styling. Range is [0..1]. Default is 0.0</summary>
		public float Dither { get { return _dither; } set { ChangeProperty(ref _dither, value); } }

		/// <summary>Toggle the use of the alpha curve to fade to transparent as Strength increases.</summary>
		public bool ApplyAlphaCurve { get { return _applyAlphaCurve; } set { ChangeProperty(ref _applyAlphaCurve, value); } }

		/// <summary>An optional curve to allow the Graphic to fade to transparent as the Strength property increases.</summary>
		public AnimationCurve AlphaCurve { get { return _alphaCurve; } set { ChangePropertyRef(ref _alphaCurve, value); } }

		/// <summary>Tint (multiply) the blurred color by this for styling.</summary>
		public Color TintColor { get { return _tintColor; } set { ChangeProperty(ref _tintColor, value); } }

		/// <summary>How the source graphic and the blurred graphic are blended/composited together.</summary>
		public BlurDirectionalBlend Blend { get { return _blend; } set { ChangeProperty(ref _blend, value); } }

		class BlurShader
		{
			const string Path = "Hidden/ChocDino/UIFX/Blur-Directional";

			static class Prop
			{
				public static int TexelStep = Shader.PropertyToID("_TexelStep");
				public static int KernelRadius = Shader.PropertyToID("_KernelRadius");
				public static int Dither = Shader.PropertyToID("_Dither");
			}
			static class Pass
			{
				public const int Linear = 0;
				public const int Falloff = 1;
			}
			static class Keyword
			{
				public const string UseDither = "USE_DITHER";
				public const string DirBoth = "DIR_BOTH";
			}

			private RenderTexture _rt;
			private Material _material;

			void CreateResources()
			{
				Shader shader = Shader.Find(Path);
				if (shader != null)
				{
					_material = new Material(shader);
				}
			}

			internal void FreeResources()
			{
				ObjectHelper.Destroy(ref _material);
				RenderTextureHelper.ReleaseTemporary(ref _rt);
			}

			internal RenderTexture Render(RenderTexture sourceTexture, float radius, Vector2 texelStep, bool weightsLinear, float dither, BlurDirectionalSide side)
			{
				int width = sourceTexture.width;
				int height = sourceTexture.height;
				if (_rt != null && (_rt.width != width || _rt.height != height))
				{
					RenderTextureHelper.ReleaseTemporary(ref _rt);
				}
				if (_rt == null)
				{
					RenderTextureFormat format = sourceTexture.format;
					if ((Filters.PerfHint & PerformanceHint.UseLessPrecision) != 0)
					{
						// TODO: create based on the input texture format, but just with less precision
						if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Default))
						{
							format = RenderTextureFormat.Default;
						}
					}
					else if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
					{
						// TODO: create based on the input texture format, but just with more precision
						if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
						{
							format = RenderTextureFormat.ARGBHalf;
						}
					}
					_rt = RenderTexture.GetTemporary(width, height, 0, format, RenderTextureReadWrite.Linear);
				}

				CreateResources();

				_material.SetVector(Prop.TexelStep, texelStep);
				_material.SetFloat(Prop.KernelRadius, radius);
				
				if (dither > 0f)
				{
					_material.SetFloat(Prop.Dither, dither);
					_material.EnableKeyword(Keyword.UseDither);
				}
				else
				{
					_material.DisableKeyword(Keyword.UseDither);
				}

				switch (side)
				{
					case BlurDirectionalSide.One:
					_material.DisableKeyword(Keyword.DirBoth);
					break;
					case BlurDirectionalSide.Both:
					_material.EnableKeyword(Keyword.DirBoth);
					break;
				}

				Graphics.Blit(sourceTexture, _rt, _material, weightsLinear ? Pass.Linear : Pass.Falloff);
				_rt.IncrementUpdateCount();

				return _rt;
			}
		}

		private BlurShader _blurShader;

		protected static new class ShaderProp
		{
			public readonly static int TintColor = Shader.PropertyToID("_TintColor");
		}

		static class ShaderKeyword
		{
			public const string BlendBehind = "BLEND_BEHIND";
			public const string BlendOver = "BLEND_OVER";
			public const string BlendAdditive = "BLEND_ADDITIVE";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Composite";

		protected override string GetDisplayShaderPath()
		{
			return BlendShaderPath;
		}

		protected override bool DoParametersModifySource()
		{
			if (base.DoParametersModifySource())
			{
				if (_length == 0f && _tintColor == Color.white) return false;
				return true;
			}
			return false;
		}

		protected override void OnEnable()
		{
			_rectAdjustOptions.padding = 2;
			_rectAdjustOptions.roundToNextMultiple = 16;
			_blurShader = new BlurShader();
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (_blurShader != null)
			{
				_blurShader.FreeResources();
				_blurShader = null;
			}
			base.OnDisable();
		}

		protected override float GetAlpha()
		{
			float alpha = 1f;
			if (_alphaCurve != null && _applyAlphaCurve)
			{
				if (_alphaCurve.length > 0)
				{
					alpha = _alphaCurve.Evaluate(_strength);
				}
			}
			return alpha;
		}

		private Vector2 GetDirection()
		{
			return new Vector2(Mathf.Sin(-_angle * Mathf.Deg2Rad), Mathf.Cos(-_angle * Mathf.Deg2Rad + Mathf.PI));
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			float maxDistance = _length * ResolutionScalingFactor * _strength;
			if (maxDistance != 0f)
			{
				Vector2 offset = GetDirection() * maxDistance;
				if (_side == BlurDirectionalSide.Both)
				{
					int x = Mathf.CeilToInt(Mathf.Abs(offset.x));
					int y = Mathf.CeilToInt(Mathf.Abs(offset.y));
					leftDown += new Vector2Int(x, y);
					rightUp += new Vector2Int(x, y);
				}
				else
				{
					offset *= -1f;
					leftDown += new Vector2Int(Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, offset.x))), Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, offset.y))));
					rightUp += new Vector2Int(Mathf.CeilToInt(Mathf.Max(0f, offset.x)), Mathf.CeilToInt(Mathf.Max(0f, offset.y)));
				}
			}
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			if (GetAlpha() > 0f)
			{
				Vector2 texelStep = GetDirection() * new Vector2(1.0f / source.width, 1.0f / source.height);

				return _blurShader.Render(source, Mathf.Abs(_length) * ResolutionScalingFactor * _strength, texelStep * Mathf.Sign(_length), _weights == BlurDirectionalWeighting.Linear, _dither * _strength * 0.1f, _side);
			}
			return null;
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			_displayMaterial.SetFloat(FilterBase.ShaderProp.Strength, _strength);
			_displayMaterial.SetColor(ShaderProp.TintColor, Color.Lerp(Color.white, _tintColor, _strength));
			switch (_blend)
			{
				default:
				case BlurDirectionalBlend.Replace:
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendBehind);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendOver);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendAdditive);
				break;
				case BlurDirectionalBlend.Behind:
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendBehind);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendOver);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendAdditive);
				break;
				case BlurDirectionalBlend.Over:
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendBehind);
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendOver);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendAdditive);
				break;
				case BlurDirectionalBlend.Additive:
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendBehind);
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendOver);
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendAdditive);
				break;
			}
			base.SetupDisplayMaterial(source, result);
		}
	}
}