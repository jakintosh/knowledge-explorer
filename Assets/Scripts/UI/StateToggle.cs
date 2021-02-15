using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace View {

	public class StateToggle : MonoBehaviour {

		[SerializeField] private Graphic _graphic1;
		[SerializeField] private Graphic _graphic2;

		[SerializeField] private Toggle _toggle1;
		[SerializeField] private Toggle _toggle2;

		private void Awake () {

			_toggle1.onValueChanged.AddListener( ToggleChanged );
		}

		private void ToggleChanged ( bool isOn ) {

			// var
		}
	}

}