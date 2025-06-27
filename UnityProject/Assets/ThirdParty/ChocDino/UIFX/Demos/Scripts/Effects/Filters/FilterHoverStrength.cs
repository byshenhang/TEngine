//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChocDino.UIFX;

namespace ChocDino.UIFX.Demos
{
	[RequireComponent(typeof(RectTransform))]
	public class FilterHoverStrength : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] FilterBase _filter = null;
		[Header("Speed")]
		[SerializeField] float _upSpeed = 8f;
		[SerializeField] float _downSpeed = 6f;
		[Header("Range")]
		[SerializeField, Range(0f, 1f)] float _minValue = 0f;
		[SerializeField, Range(0f, 1f)] float _maxValue = 1f;

		private bool _isOver = false;

		void Awake()
		{
			if (_filter == null)
			{
				_filter = GetComponent<FilterBase>();
			}
			UpdateAnimation(true);
		}

		void Start()
		{
			UpdateAnimation(true);
		}

		void Update()
		{
			UpdateAnimation(false);
		}

		void UpdateAnimation(bool force)
		{
			if (!isActiveAndEnabled || _filter == null || !_filter.isActiveAndEnabled) return;
			
			float target = _minValue;
			float dampSpeed = _downSpeed;
			if (_isOver)
			{
				target = _maxValue;
				dampSpeed = _upSpeed;
			}

			if (force)
			{
				_filter.Strength = target;
			}
			else if (Mathf.Abs(_filter.Strength - target) > 0.001f)
			{
				_filter.Strength = MathUtils.DampTowards(_filter.Strength, target, dampSpeed, Time.deltaTime);
			}
			else
			{
				_filter.Strength = target;
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_isOver = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_isOver = false;
		}
	}
}