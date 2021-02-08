using Data;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


public struct Content {

	// data structures
	public struct Link {

		public int StartIndex;
		public int EndIndex;
		public string Content;
		public string Destination;

		public bool Contains ( int index ) {
			return ( index >= StartIndex && index <= EndIndex );
		}

		public override string ToString () {
			return $"Start: {StartIndex}; End: {EndIndex}; Content: {Content}; Destination: {Destination};";
		}
	}

	// data
	public string VisibleString;
	public string FormattedString;
	public string RawString;
	public List<Link> Links;

	// constructors
	public static Content FromText ( string text, Style style ) {

		var (visibleString, links) = ProcessTextContent( text );
		var formattedString = FormatLinksInText( visibleString, links, style );

		// debug info
		Debug.Log( $"Found Links:{ links.Reduce( "", ( total, link ) => total + "\n" + link.ToString() ) }" );

		var content = new Content();
		content.VisibleString = visibleString;
		content.FormattedString = formattedString;
		content.RawString = text;
		content.Links = links;
		return content;
	}

	// helpers
	private static (string visible, List<Link> links) ProcessTextContent ( string content ) {

		var visibleString = new StringBuilder();
		var links = new List<Link>();

		for ( int i = 0; i < content.Length; i++ ) {

			var c = content[i];

			// try to parse link
			if ( c == '[' ) {
				var (nextIndex, link) = ParseLink( content, startIndex: i );
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

	enum LinkParserState {
		Content,
		Destination,
		Complete
	}
	private static (int nextIndex, Link? link) ParseLink ( string content, int startIndex ) {

		var link = new Link();

		var s = new StringBuilder();
		var state = LinkParserState.Content;
		for ( int i = startIndex + 1; i < content.Length; i++ ) {

			switch ( state ) {

				case LinkParserState.Content:
					if ( content[i] == ']' ) {
						if ( content[i + 1] == '(' ) {
							i++; // skip next char
							state = LinkParserState.Destination;
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

				case LinkParserState.Destination:
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

	private static string FormatLinksInText ( string text, List<Link> links, Style style ) {

		var reverseOrderedLinks = new List<Link>( links );
		reverseOrderedLinks.Sort( ( a, b ) => b.EndIndex.CompareTo( a.EndIndex ) );
		foreach ( var link in reverseOrderedLinks ) {
			text = FormatSubstring( text, link.StartIndex, link.EndIndex, true, style.Accent );
		}
		return text;
	}

	private static string FormatSubstring ( string text, int startIndex, int endIndex, bool underlined, Color color ) {

		var prefix = underlined ? $"<#{ColorUtility.ToHtmlStringRGBA( color )}><u>" : $"<#{ColorUtility.ToHtmlStringRGBA( color )}>";
		var postfix = underlined ? "</u></color>" : "</color";

		text = text.Insert( endIndex, postfix );
		text = text.Insert( startIndex, prefix );
		return text;
	}
}

public class ContentRenderer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

	[SerializeField] private TextMeshProUGUI _textContent;

	private Content _content;
	private Style _style;

	public Style Style {
		get => _style;
		set {
			if ( _style == value ) { return; }

			_style = value;
			_textContent.color = _style.Foreground;
		}
	}


	public void SetBody ( string body ) {

		_content = Content.FromText( body, _style );
		_textContent.text = _content.FormattedString;
	}

	// link clicking
	void IPointerClickHandler.OnPointerClick ( PointerEventData eventData ) {

		var charIndex = TMP_TextUtilities.FindIntersectingCharacter( _textContent, eventData.position, Camera.main, true );
		foreach ( var link in _content.Links ) {
			if ( link.Contains( charIndex ) ) {
				Debug.Log( $"Link Clicked: {link.Destination}" );
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
				if ( link.Contains( charIndex ) ) {
					// do something about link hovering here
				}
			}
		}
	}
}
