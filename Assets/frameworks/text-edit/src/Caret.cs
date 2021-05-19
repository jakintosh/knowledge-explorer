using Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace TextEdit {

	public class Caret : MonoBehaviour {

		[SerializeField] private Image _image;

		public void SetPosition ( float left, float top, float height ) {

			// set position
			Debug.Log( $"Caret.SetPosition( left: {left}, top: {top}, height: {height} )" );
			_image.rectTransform.anchoredPosition = new Vector2( left, top );

			// set height
			var sizeDelta = _image.rectTransform.sizeDelta;
			sizeDelta.y = height;
			_image.rectTransform.sizeDelta = sizeDelta;

		}
	}
}