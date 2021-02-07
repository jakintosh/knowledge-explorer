using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StyleRenderer : MonoBehaviour {

	[SerializeField] private List<Graphic> _accent = new List<Graphic>();
	[SerializeField] private List<Graphic> _foreground = new List<Graphic>();
	[SerializeField] private List<Graphic> _background = new List<Graphic>();

	public void SetStyle ( Style style ) {

		foreach ( var element in _accent ) { element.color = style.Accent; }
		foreach ( var element in _foreground ) { element.color = style.Foreground; }
		foreach ( var element in _background ) { element.color = style.Background; }
	}
}
