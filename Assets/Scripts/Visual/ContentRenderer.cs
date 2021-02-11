using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using View;


public class ContentRenderer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

	[SerializeField] private TextMeshProUGUI _textContent;

	private Content _content;
	private Model.Style _style;

	public Model.Style Style {
		get => _style;
		set {
			if ( _style == value ) { return; }

			_style = value;
			_textContent.color = _style.Foreground;
		}
	}


	public void SetContent ( View.Content content ) {

		_content = content;
		_textContent.text = content.TMPString;
	}

	// link clicking
	void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

		var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _textContent, eventData.position, Camera.main, true );
		foreach ( var link in _content.Links ) {
			if ( link.ContainsChar( charIndex ) ) {
				Debug.Log( $"Link Clicked: {link.ID}" );
			}
		}
	}


	// hovering stuff
	private bool _isHovering;
	void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

		_isHovering = true;
	}
	void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

		_isHovering = false;
	}
	private void Update () {

		if ( _isHovering ) {
			var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _textContent, Input.mousePosition, Camera.main, true );
			foreach ( var link in _content.Links ) {
				if ( link.ContainsChar( charIndex ) ) {
					// do something about link hovering here
				}
			}
		}
	}
}
