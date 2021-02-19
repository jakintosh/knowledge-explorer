using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace View {

	public struct Link {

		public string ID;
		public int StartIndex;
		public int EndIndex;

		public bool ContainsChar ( int charIndex ) {
			return ( charIndex >= StartIndex && charIndex <= EndIndex );
		}

		public override string ToString () {
			return $"ID: {ID}, Char Range: {{{StartIndex} -> {EndIndex}}}";
		}
	}

	public class Content {

		public string TMPString;
		public List<Link> Links;

		private Model.Content _content;
		private Model.Style _style;

		public Content ( Model.Content content, Model.Style style ) {

			Links = new List<Link>();

			_style = style;
			_content = content;
			Refresh();
		}

		public void SetStyle ( Model.Style style ) {

			_style = style;
			Refresh();
		}
		public void SetContentModel ( Model.Content content ) {

			_content = content;
			Refresh();
		}

		private void Refresh () {

			if ( _content == null ) {
				return;
			}
			TMPString = DoSomething( _content.ModelString );
		}

		private string DoSomething ( string modelString ) {

			if ( modelString == null ) {
				return "";
			}

			// "This is a $0123abc that has a $4567def." => "This is a <#0000FFFF><u>string</u></color> that has a <#0000FFFF><u>link</u></color>.", with links

			var visibleString = new StringBuilder( capacity: modelString.Length );
			int visibleIndex = 0;
			for ( int i = 0; i < modelString.Length; i++ ) {

				var c = modelString[i];

				if ( c == '$' ) { // found link

					// get id
					var idStringBuilder = new StringBuilder();
					var idLength = Model.Bucket.ID_LENGTH;
					for ( int j = 0; j < idLength; j++ ) {
						var idChar = modelString[i + 1 + j];
						idStringBuilder.Append( idChar ); ;
					}

					// save link id
					var id = idStringBuilder.ToString();

					// resolve title
					var title = Model.Bucket.Instance.GetTitleForID( id );
					if ( title != null ) {

						// create a visual link
						var link = new Link();
						link.ID = id;
						link.StartIndex = visibleIndex;
						link.EndIndex = visibleIndex + title.Length;
						Links.Add( link );

						Debug.Log( $"Found link for {title} in range {link.StartIndex} to {link.EndIndex}" );

						// add formatted title to string
						var color = _style.Accent;
						var underlined = true;
						var prefix = underlined ? $"<#{ColorUtility.ToHtmlStringRGBA( color )}><u>" : $"<#{ColorUtility.ToHtmlStringRGBA( color )}>";
						var postfix = underlined ? "</u></color>" : "</color";
						visibleString.Append( $"{prefix}{title}{postfix}" );
						visibleIndex += title.Length;

					} else {

						Debug.LogError( $"View.Content.FindLinks: Invalid link found {{{id}}}" );
						var appendix = $"${id}";
						visibleString.Append( appendix );
						visibleIndex += appendix.Length;
					}

					// move progress along
					i = i + idLength;

				} else {

					// just add character
					visibleString.Append( c );
					visibleIndex++;
				}
			}

			return visibleString.ToString();
		}

		private void HandleTitleChange ( string id, string newTitle ) {

			if ( Links.Exists( l => l.ID == id ) ) {
				Refresh();
			}
		}
	}
}
