#if UIFX_BETA

//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(BlurZoomFilter), true)]
	[CanEditMultipleObjects]
	internal class BlurZoomFilterEditor : FilterBaseEditor
	{
		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, BlurFilterEditor.s_aboutInfo } );

		private static readonly GUIContent Content_FadeCurve = new GUIContent("Fade Curve");
		private static readonly GUIContent Content_Blur = new GUIContent("Blur");
		private static readonly GUIContent Content_Scale = new GUIContent("Scale");
		private static readonly GUIContent Content_CenterX = new GUIContent("Center X");
		private static readonly GUIContent Content_CenterY = new GUIContent("Center Y");

		private SerializedProperty _propScale;
		private SerializedProperty _propCenter;
		private SerializedProperty _propCenterX;
		private SerializedProperty _propCenterY;
		private SerializedProperty _propSide;
		private SerializedProperty _propWeights;
		private SerializedProperty _propDither;
		private SerializedProperty _propApplyAlphaCurve;
		private SerializedProperty _propAlphaCurve;
		private SerializedProperty _propTintColor;
		private SerializedProperty _propBlend;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;

		protected virtual void OnEnable()
		{
			_propScale = VerifyFindProperty("_scale");
			_propCenter = VerifyFindProperty("_center");
			_propCenterX = VerifyFindProperty("_center.x");
			_propCenterY = VerifyFindProperty("_center.y");
			_propSide = VerifyFindProperty("_side");
			_propWeights = VerifyFindProperty("_weights");
			_propDither = VerifyFindProperty("_dither");
			_propApplyAlphaCurve = VerifyFindProperty("_applyAlphaCurve");
			_propAlphaCurve = VerifyFindProperty("_alphaCurve");
			_propTintColor = VerifyFindProperty("_tintColor");
			_propBlend = VerifyFindProperty("_blend");
			_propStrength = VerifyFindProperty("_strength");
			_propRenderSpace = VerifyFindProperty("_renderSpace");
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			GUILayout.Label(Content_Blur, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			PropertyReset_Slider(_propScale, Content_Scale, 1f, 10f, 1f);
			PropertyReset_Slider(_propCenterX, Content_CenterX, -1f, 1f, 0f);
			PropertyReset_Slider(_propCenterY, Content_CenterY, -1f, 1f, 0f);
			EnumAsToolbar(_propSide);
			EnumAsToolbar(_propWeights);
			EditorGUILayout.PropertyField(_propDither);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propApplyAlphaCurve, Content_FadeCurve);
			if (_propApplyAlphaCurve.boolValue)
			{
				EditorGUILayout.PropertyField(_propAlphaCurve);
			}
			EditorGUILayout.PropertyField(_propTintColor);
			EnumAsToolbar(_propBlend);
			DrawStrengthProperty(_propStrength);
			EnumAsToolbarCompact(_propRenderSpace);
			EditorGUI.indentLevel--;

			if (OnInspectorGUI_Baking(filter))
			{
				return;
			}

			FilterBaseEditor.OnInspectorGUI_Debug(filter);

			serializedObject.ApplyModifiedProperties();
		}
	}
}

#endif