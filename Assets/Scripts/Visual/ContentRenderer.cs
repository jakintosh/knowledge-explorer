using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ContentRenderer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

	[SerializeField] private TextMeshProUGUI _textContent;

	private List<Link> _links;

	private Style _style;

	public void SetStyle ( Style style ) {

		_style = style;
		_textContent.color = _textContent.color;
	}
	public void SetContent ( Content content ) {

		string visibleText;
		(visibleText, _links) = ProcessTextContent( content.Body );
		Debug.Log( $"Found Links:{ _links.Reduce( "", ( total, link ) => total + "\n" + link.ToString() ) }" );
		_textContent.text = FormatText( visibleText, _links );

	}

	private bool _isHovering;
	void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

		var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _textContent, eventData.position, Camera.main, true );
		foreach ( var link in _links ) {
			if ( link.Contains( charIndex ) ) {
				Debug.Log( $"Link Clicked: {link.Destination}" );
			}
		}
	}
	void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

		_isHovering = true;
	}
	void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

		_isHovering = false;
	}

	private void Update () {

		if ( _isHovering ) {
			var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _textContent, Input.mousePosition, Camera.main, true );
			foreach ( var link in _links ) {
				if ( link.Contains( charIndex ) ) {
					// do something about link hovering here
				}
			}
		}
	}




	private string FormatText ( string text, List<Link> links ) {

		var reverseOrderedLinks = new List<Link>( links );
		reverseOrderedLinks.Sort( ( a, b ) => b.EndIndex.CompareTo( a.EndIndex ) );
		foreach ( var link in reverseOrderedLinks ) {
			text = FormatText( text, link.StartIndex, link.EndIndex, true, _style.Accent );
		}
		return text;
	}

	private string FormatText ( string text, int startIndex, int endIndex, bool underlined, Color color ) {

		var prefix = underlined ? $"<#{ColorUtility.ToHtmlStringRGBA( color )}><u>" : $"<#{ColorUtility.ToHtmlStringRGBA( color )}>";
		var postfix = underlined ? "</u></color>" : "</color";

		text = text.Insert( endIndex, postfix );
		text = text.Insert( startIndex, prefix );
		return text;
	}



	public struct Link {
		public int StartIndex;
		public int EndIndex;
		public string Content;
		public string Destination;
		public override string ToString () {
			return $"Start: {StartIndex}; End: {EndIndex}; Content: {Content}; Destination: {Destination};";
		}

		public bool Contains ( int index ) {
			return ( index >= StartIndex && index <= EndIndex );
		}
	}

	private (string visible, List<Link> links) ProcessTextContent ( string content ) {

		var visibleString = new StringBuilder();
		var links = new List<Link>();

		for ( int i = 0; i < content.Length; i++ ) {

			var c = content[i];

			// try to process link
			if ( c == '[' ) {
				var (nextIndex, link) = ProcessLink( content, startIndex: i );
				i = nextIndex;
				if ( link.HasValue ) {
					var l = link.Value;
					l.StartIndex = visibleString.Length;
					l.EndIndex = l.StartIndex + l.Content.Length;
					links.Add( l );
					foreach ( var linkChar in l.Content ) {
						visibleString.Append( linkChar );
					}
					continue;
				}
			}

			visibleString.Append( c );
		}

		return (visible: visibleString.ToString(), links: links);
	}

	enum LinkProcessorState {
		Content,
		Destination,
		Complete
	}
	private (int nextIndex, Link? link) ProcessLink ( string content, int startIndex ) {

		var link = new Link();

		var s = new StringBuilder();
		var state = LinkProcessorState.Content;
		for ( int i = startIndex + 1; i < content.Length; i++ ) {

			switch ( state ) {

				case LinkProcessorState.Content:
					if ( content[i] == ']' ) {
						if ( content[i + 1] == '(' ) {
							i++; // skip next char
							state = LinkProcessorState.Destination;
							link.Content = s.ToString();
							s.Clear();
						} else {
							// bad format/not a link
							return (nextIndex: startIndex, link: null);
						}
					} else {
						s.Append( content[i] );
					}
					break;

				case LinkProcessorState.Destination:
					if ( content[i] == ')' ) {
						link.Destination = s.ToString();
						return (nextIndex: i, link: link);
					} else {
						s.Append( content[i] );
					}
					break;
			}
		}

		// bad format/not a link
		return (nextIndex: startIndex, link: null);
	}

}
