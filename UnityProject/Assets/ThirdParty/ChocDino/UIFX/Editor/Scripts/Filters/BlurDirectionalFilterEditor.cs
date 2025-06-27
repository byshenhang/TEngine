//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(BlurDirectionalFilter), true)]
	[CanEditMultipleObjects]
	internal class BlurDirectionalFilterEditor : FilterBaseEditor
	{
		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeToBundle, BlurFilterEditor.s_aboutInfo } );

		private static readonly GUIContent Content_FadeCurve = new GUIContent("Fade Curve");
		private static readonly GUIContent Content_Blur = new GUIContent("Blur");

		private SerializedProperty _propAngle;
		private SerializedProperty _propLength;
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
			_propAngle = VerifyFindProperty("_angle");
			_propLength = VerifyFindProperty("_length");
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
			EditorGUILayout.PropertyField(_propAngle);
			EditorGUILayout.PropertyField(_propLength);
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