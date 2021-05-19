using UnityEngine;
using UnityEngine.EventSystems;

namespace TextEdit {

	[RequireComponent( typeof( Collider ) )]
	public class Text : MonoBehaviour,
		IPointerEnterHandler,
		IPointerDownHandler,
		IPointerUpHandler,
		IPointerExitHandler {

		public struct LineInfo {

			public struct Axis {
				public float Top { get; private set; }
				public float Bottom { get; private set; }
				public float Height => Top - Bottom;
				public Axis ( float top, float bottom ) {
					Top = top;
					Bottom = bottom;
				}
			}

			public Axis Line { get; private set; }
			public Axis Extents { get; private set; }

			public LineInfo ( float lineTop, float lineBottom, float extentsTop, float extentsBottom ) {
				Line = new Axis( lineTop, lineBottom );
				Extents = new Axis( extentsTop, extentsBottom );
			}
		}
		public struct CharBounds {

			public float Top { get; private set; }
			public float Bottom { get; private set; }
			public float Left { get; private set; }
			public float Right { get; private set; }
			public float Center => Left + ( Width / 2f );
			public float Width => Right - Left;

			public CharBounds ( float top, float bottom, float left, float right ) {
				Top = top;
				Bottom = bottom;
				Left = left;
				Right = right;
			}
		}
		public struct CaretBounds {

			public float Target { get; private set; }
			public int CharIndex { get; private set; }
			public float Top { get; private set; }
			public float Bottom { get; private set; }
			public float Left { get; private set; }
			public float Right { get; private set; }
			public float Center => Left + ( Width / 2f );
			public float Width => Right - Left;
			public float Height => Top - Bottom;

			public bool Contains ( Vector2 point ) {
				return point.x > Left && point.x < Right &&
					point.y < Top && point.y > Bottom;
			}

			public CaretBounds ( int charIndex, float target, float top, float bottom, float left, float right ) {
				CharIndex = charIndex;
				Target = target;
				Top = top;
				Bottom = bottom;
				Left = left;
				Right = right;
			}
		}

		public static float CalculateLineHeight ( UnityEngine.UI.Text text ) {
			var extents = text.cachedTextGenerator.rectExtents.size * 0.5f;
			var settings = text.GetGenerationSettings( extents );
			return text.cachedTextGeneratorForLayout.GetPreferredHeight( "A", settings );
		}


		[SerializeField] private UnityEngine.UI.Text _text;
		[SerializeField] private Caret _caret;

		public bool _isInside;
		public Vector3? _contactPoint;
		public float _caretIndex;
		public CaretBounds[] _caretBounds;

		private void RefreshCaratIndexForMousePosition () {

			_contactPoint = GetContactPoint( Input.mousePosition );
			_caretBounds.ForEach( bounds => {
				if ( bounds.Contains( _contactPoint.Value ) ) {
					_caretIndex = bounds.CharIndex;
					_caret.SetPosition(
						left: bounds.Target,
						top: bounds.Top,
						height: bounds.Height
					);
				}
			} );

			Debug.Log( $"Local space point: {_contactPoint}" );
		}
		private void ProcessAllChars ( UnityEngine.UI.Text text ) {

			var textGen = text.cachedTextGenerator;
			var lineHeight = CalculateLineHeight( text );

			var lines = textGen.GetLinesArray().Convert( lineInfo => (startIndex: lineInfo.startCharIdx, info: lineInfo) );
			var chars = textGen.GetCharactersArray();

			var linesArray = new (float top, float bottom)[lines.Count];
			for ( int i = 0; i < lines.Count; i++ ) {

				var line = lines[i];
				var top = chars[line.startIndex].cursorPos.y;
				linesArray[i] = (top: chars[line.startIndex].cursorPos.y, bottom: top - lineHeight);
			}

			var lineInfoArray = new LineInfo[lines.Count];
			for ( int i = 0; i < lines.Count; i++ ) {

				var line = linesArray[i];

				float top = ( i == 0 ) ?
					top = line.top : // if first line, top is line top
					top = line.top + ( ( linesArray[i - 1].bottom - line.top ) / 2f ); // else top is midpoint of this top and prev bottom 

				float bottom = ( i == lines.Count - 1 ) ?
					bottom = line.bottom : // if last line, bottom is line bottom
					bottom = line.bottom + ( ( linesArray[i + 1].top - line.bottom ) / 2f ); // else bottom is midpoint of this bottom and next top

				lineInfoArray[i] = new LineInfo(
					lineTop: line.top,
					lineBottom: line.bottom,
					extentsTop: top,
					extentsBottom: bottom
				);
			}

			var extents = textGen.rectExtents;
			var sizeDelta = text.rectTransform.sizeDelta;

			var extentsTop = extents.yMin - extents.center.y + ( sizeDelta.y / 2f );
			var extentsBottom = extents.yMax - extents.center.y - ( sizeDelta.y / 2f );
			var extentsLeft = extents.xMin - extents.center.x + ( sizeDelta.x / 2f );
			var extentsRight = extents.xMax - extents.center.x - ( sizeDelta.x / 2f );

			var lineIndex = 0;
			var charBounds = new CharBounds[chars.Length];
			_caretBounds = new CaretBounds[chars.Length + lines.Count];

			// some local functions
			bool IsLastLine () => lineIndex == lines.Count - 1;
			(int startIndex, UILineInfo info) NextLine () => lines[lineIndex + 1];

			for ( int i = 0; i < chars.Length; i++ ) {

				// some local functions
				bool IsFirstCharOnLine () => i == lines[lineIndex].startIndex;
				bool IsLastCharOnLine () => IsLastLine() ? i == chars.Length - 1 : i == NextLine().startIndex - 1;

				// if not on last line, advance lines until current index is at/above start index
				while ( !IsLastLine() && i >= NextLine().startIndex ) {
					lineIndex++;
				}

				var lineInfo = lineInfoArray[lineIndex];
				var charInfo = chars[i];

				var charTop = lineInfo.Extents.Top;
				var charBottom = lineInfo.Extents.Bottom;
				var charLeft = charInfo.cursorPos.x;
				var charRight = charLeft + charInfo.charWidth;
				charBounds[i] = new CharBounds(
					top: charTop,
					bottom: charBottom,
					left: charLeft,
					right: charRight
				);

				void MakeCaretPos ( int index, bool actualLast = false ) {

					var charIndex = index + ( actualLast ? 1 : 0 );
					var caretTarget = actualLast ?
						charBounds[index].Right :
						charBounds[index].Left;
					var caretTop = charTop;
					var caretBottom = charBottom;
					var caretLeft = IsFirstCharOnLine() ?
						extentsLeft :
						actualLast ?
							charBounds[index].Center :
							charBounds[index - 1].Center;
					var caretRight = IsLastCharOnLine() && actualLast ?
						extentsRight :
						charBounds[index].Center;
					_caretBounds[charIndex + lineIndex] = new CaretBounds(
						charIndex: charIndex,
						target: caretTarget,
						top: caretTop,
						bottom: caretBottom,
						left: caretLeft,
						right: caretRight
					);
				}

				// lol wtfff
				MakeCaretPos( index: i );
				if ( IsLastCharOnLine() ) {
					MakeCaretPos( index: i, actualLast: true );
				}
			}
		}


		private void Update () {

			_caret.gameObject.SetActive( _isInside );
			if ( _isInside ) {
				RefreshCaratIndexForMousePosition();
			} else {
				_contactPoint = null;
			}
		}
		private void OnDrawGizmos () {

			if ( Application.isPlaying ) {

				ProcessAllChars( _text );
				_caretBounds.ForEach( caretBounds => {

					var caretTop = transform.TransformPoint( new Vector3( caretBounds.Target, caretBounds.Top ) );
					var caretBottom = transform.TransformPoint( new Vector3( caretBounds.Target, caretBounds.Bottom ) );

					var boundsTopLeft = transform.TransformPoint( new Vector3( caretBounds.Left, caretBounds.Top ) );
					var boundsTopRight = transform.TransformPoint( new Vector3( caretBounds.Right, caretBounds.Top ) );
					var boundsBottomLeft = transform.TransformPoint( new Vector3( caretBounds.Left, caretBounds.Bottom ) );
					var boundsBottomRight = transform.TransformPoint( new Vector3( caretBounds.Right, caretBounds.Bottom ) );

					Gizmos.color = caretBounds.CharIndex == _caretIndex ? Color.cyan : new Color( 0.25f, 1f, 0.5f, 0.5f );
					Gizmos.DrawLine( boundsTopLeft, boundsTopRight );
					Gizmos.DrawLine( boundsTopLeft, boundsBottomLeft );
					Gizmos.DrawLine( boundsTopRight, boundsBottomRight );
					Gizmos.DrawLine( boundsBottomLeft, boundsBottomRight );
					Gizmos.color = caretBounds.CharIndex == _caretIndex ? Color.cyan : Color.red;
					Gizmos.DrawLine( caretTop, caretBottom );
					Gizmos.color = Color.white;
				} );

				if ( _contactPoint != null ) {
					Gizmos.color = Color.red;
					var worldPoint = transform.TransformPoint( _contactPoint.Value );
					Gizmos.DrawSphere( worldPoint, 1f );
					Gizmos.color = Color.white;
				}
			}
		}

		private Plane GetPlane () => new Plane( inNormal: -transform.forward, inPoint: transform.position );
		private Ray GetRay ( Vector2 mousePosition ) => Camera.main.ScreenPointToRay( mousePosition );
		private Vector3 GetContactPoint ( Vector2 mousePosition ) {

			var ray = GetRay( mousePosition );
			GetPlane().Raycast( ray, out float distance );
			var contactPoint = ray.origin + ( ray.direction * distance );
			return transform.InverseTransformPoint( contactPoint );
		}

		// ********** IPointer Event Handlers **********

		void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

			_isInside = true;
			Debug.Log( $"Is inside: { _isInside}" );
		}
		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {
			// hmm
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {
			// hmm
		}
		void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

			_isInside = false;
			Debug.Log( $"Is inside: { _isInside}" );
		}
	}


	// [RequireComponent( typeof( Collider ) )]
	// public class Text2 : MonoBehaviour,
	// 	IPointerEnterHandler,
	// 	IPointerDownHandler,
	// 	IPointerUpHandler,
	// 	IPointerExitHandler {


	// 	[SerializeField] private TextMeshProUGUI _text;
	// 	[SerializeField] private UnityEngine.UI.Text _textU;
	// 	[SerializeField] private Caret _caret;

	// 	private void Start () {

	// 		ProcessAllChars();
	// 	}
	// 	public static float CalculateLineHeight ( UnityEngine.UI.Text text ) {
	// 		var extents = text.cachedTextGenerator.rectExtents.size * 0.5f;
	// 		return text.cachedTextGeneratorForLayout.GetPreferredHeight( "A", text.GetGenerationSettings( extents ) );
	// 	}

	// 	private struct LineInfo {

	// 		public struct Axis {
	// 			public float Top { get; private set; }
	// 			public float Bottom { get; private set; }
	// 			public float Height => Top - Bottom;
	// 			public Axis ( float top, float bottom ) {
	// 				Top = top;
	// 				Bottom = bottom;
	// 			}
	// 		}

	// 		public Axis Line { get; private set; }
	// 		public Axis Extents { get; private set; }

	// 		public LineInfo ( float lineTop, float lineBottom, float extentsTop, float extentsBottom ) {
	// 			Line = new Axis( lineTop, lineBottom );
	// 			Extents = new Axis( extentsTop, extentsBottom );
	// 		}
	// 	}
	// 	private struct CharBounds {

	// 		public float Top { get; private set; }
	// 		public float Bottom { get; private set; }
	// 		public float Left { get; private set; }
	// 		public float Right { get; private set; }
	// 		public float Center => Left + ( Width / 2f );
	// 		public float Width => Right - Left;

	// 		public CharBounds ( float top, float bottom, float left, float right ) {
	// 			Top = top;
	// 			Bottom = bottom;
	// 			Left = left;
	// 			Right = right;
	// 		}
	// 	}
	// 	private struct CaretBounds {

	// 		public float Target { get; private set; }
	// 		public float Top { get; private set; }
	// 		public float Bottom { get; private set; }
	// 		public float Left { get; private set; }
	// 		public float Right { get; private set; }
	// 		public float Center => Left + ( Width / 2f );
	// 		public float Width => Right - Left;

	// 		public CaretBounds ( float target, float top, float bottom, float left, float right ) {
	// 			Target = target;
	// 			Top = top;
	// 			Bottom = bottom;
	// 			Left = left;
	// 			Right = right;
	// 		}
	// 	}

	// 	private void OnDrawGizmos () {

	// 		if ( Application.isPlaying ) {

	// 			ProcessAllChars();
	// 			_caretBounds.ForEach( caretBounds => {


	// 				var caretTop = transform.TransformPoint( new Vector3( caretBounds.Target, caretBounds.Top ) );
	// 				var caretBottom = transform.TransformPoint( new Vector3( caretBounds.Target, caretBounds.Bottom ) );

	// 				var boundsTopLeft = transform.TransformPoint( new Vector3( caretBounds.Left, caretBounds.Top ) );
	// 				var boundsTopRight = transform.TransformPoint( new Vector3( caretBounds.Right, caretBounds.Top ) );
	// 				var boundsBottomLeft = transform.TransformPoint( new Vector3( caretBounds.Left, caretBounds.Bottom ) );
	// 				var boundsBottomRight = transform.TransformPoint( new Vector3( caretBounds.Right, caretBounds.Bottom ) );

	// 				Gizmos.color = new Color( 0.25f, 1f, 0.5f, 0.5f );
	// 				Gizmos.DrawLine( boundsTopLeft, boundsTopRight );
	// 				Gizmos.DrawLine( boundsTopLeft, boundsBottomLeft );
	// 				Gizmos.DrawLine( boundsTopRight, boundsBottomRight );
	// 				Gizmos.DrawLine( boundsBottomLeft, boundsBottomRight );

	// 				Gizmos.color = Color.red;
	// 				Gizmos.DrawLine( caretTop, caretBottom );


	// 				Gizmos.color = Color.white;
	// 			} );

	// 		}

	// 		return;
	// 		if ( Application.isPlaying ) {

	// 			var textGen = _textU.cachedTextGenerator;

	// 			var lines = textGen.GetLinesArray().Convert( lineInfo => (startIndex: lineInfo.startCharIdx, info: lineInfo) );
	// 			var chars = textGen.GetCharactersArray();

	// 			var linesArray = new (float top, float bottom)[lines.Count];
	// 			for ( int i = 0; i < lines.Count; i++ ) {

	// 				var line = lines[i];
	// 				var top = chars[line.startIndex].cursorPos.y;
	// 				var height = CalculateLineHeight( _textU );
	// 				linesArray[i] = (top: chars[line.startIndex].cursorPos.y, bottom: top - height);

	// 			}

	// 			var lineInfoArray = new LineInfo[lines.Count];
	// 			for ( int i = 0; i < lines.Count; i++ ) {

	// 				var line = linesArray[i];

	// 				// if first line, top is top of first char
	// 				// if not first line, top is mid point of this top and prev bottom
	// 				float top;
	// 				if ( i == 0 ) {
	// 					top = line.top;
	// 				} else {
	// 					var prev = linesArray[i - 1];
	// 					top = prev.bottom + ( ( line.top - prev.bottom ) / 2f );
	// 				}

	// 				// if last line, bottom is top of first char + line height
	// 				// if not last line, bottom is midpoint of (top of first + line height) and top of next line
	// 				float bottom;
	// 				if ( i == lines.Count - 1 ) {
	// 					bottom = line.bottom;
	// 				} else {
	// 					var next = linesArray[i + 1];
	// 					bottom = line.bottom + ( ( next.top - line.bottom ) / 2f );
	// 				}

	// 				lineInfoArray[i] = new LineInfo(
	// 					lineTop: line.top,
	// 					lineBottom: line.bottom,
	// 					extentsTop: top,
	// 					extentsBottom: bottom
	// 				);
	// 			}

	// 			// var rt = ( transform as RectTransform );
	// 			// for ( int i = 0; i < lines.Count; i++ ) {

	// 			// 	var info = lineInfoArray[i];
	// 			// 	var line = lines[i];

	// 			// 	var topLeft = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x, info.Extents.Top ) );
	// 			// 	var topRight = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x + rt.sizeDelta.x + _textU.rectTransform.sizeDelta.x, info.Extents.Top ) );

	// 			// 	var bottomLeft = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x, info.Extents.Bottom ) );
	// 			// 	var bottomRight = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x + rt.sizeDelta.x + _textU.rectTransform.sizeDelta.x, info.Extents.Bottom ) );

	// 			// 	Gizmos.color = Color.white;
	// 			// 	Gizmos.DrawLine( topLeft, topRight );
	// 			// 	Gizmos.color = Color.blue;
	// 			// 	Gizmos.DrawLine( bottomLeft, bottomRight );
	// 			// 	Gizmos.color = Color.red;

	// 			// 	Debug.Log( $"Line {i} height: {info.Extents.Height}" );
	// 			// }

	// 			var extentsTop = textGen.rectExtents.yMin - textGen.rectExtents.center.y + ( _textU.rectTransform.sizeDelta.y / 2f );
	// 			var extentsBottom = textGen.rectExtents.yMax - textGen.rectExtents.center.y - ( _textU.rectTransform.sizeDelta.y / 2f );
	// 			var extentsLeft = textGen.rectExtents.xMin - textGen.rectExtents.center.x + ( _textU.rectTransform.sizeDelta.x / 2f );
	// 			var extentsRight = textGen.rectExtents.xMax - textGen.rectExtents.center.x - ( _textU.rectTransform.sizeDelta.x / 2f );
	// 			if ( true ) {

	// 				var topLeft = transform.TransformPoint( new Vector3( extentsLeft, extentsTop ) );
	// 				var topRight = transform.TransformPoint( new Vector3( extentsRight, extentsTop ) );
	// 				var bottomLeft = transform.TransformPoint( new Vector3( extentsLeft, extentsBottom ) );
	// 				var bottomRight = transform.TransformPoint( new Vector3( extentsRight, extentsBottom ) );

	// 				Gizmos.color = Color.blue;
	// 				Gizmos.DrawLine( topLeft, topRight );
	// 				Gizmos.DrawLine( topLeft, bottomLeft );
	// 				Gizmos.DrawLine( topRight, bottomRight );
	// 				Gizmos.DrawLine( bottomLeft, bottomRight );
	// 				Gizmos.color = Color.white;
	// 			}

	// 			// some local functions
	// 			var charBounds = new CharBounds[chars.Length];
	// 			var caretBounds = new CaretBounds[chars.Length + lines.Count];
	// 			var lineIndex = 0;
	// 			bool IsLastLine () => lineIndex == lines.Count - 1;
	// 			(int startIndex, UILineInfo info) NextLine () => lines[lineIndex + 1];
	// 			for ( int i = 0; i < chars.Length; i++ ) {

	// 				// some local functions
	// 				bool IsFirstCharOnLine () => i == lines[lineIndex].startIndex;
	// 				bool IsLastCharOnLine () => IsLastLine() ? i == chars.Length - 1 : i == NextLine().startIndex - 1;

	// 				// if not on last line, advance lines until current index is at/above start index
	// 				while ( !IsLastLine() && i >= NextLine().startIndex ) {
	// 					lineIndex++;
	// 				}

	// 				var lineInfo = lineInfoArray[lineIndex];
	// 				var charInfo = chars[i];

	// 				var charTop = lineInfo.Extents.Top;
	// 				var charBottom = lineInfo.Extents.Bottom;
	// 				var charLeft = charInfo.cursorPos.x;
	// 				var charRight = charLeft + charInfo.charWidth;
	// 				charBounds[i] = new CharBounds(
	// 					top: charTop,
	// 					bottom: charBottom,
	// 					left: charLeft,
	// 					right: charRight
	// 				);
	// 				var charTopLeft = transform.TransformPoint( new Vector3( charLeft, charTop ) );
	// 				var charTopRight = transform.TransformPoint( new Vector3( charRight, charTop ) );
	// 				var charBottomLeft = transform.TransformPoint( new Vector3( charLeft, charBottom ) );
	// 				var charBottomRight = transform.TransformPoint( new Vector3( charRight, charBottom ) );

	// 				Gizmos.color = new Color( 1f, 1f, 1f, 0.25f );
	// 				Gizmos.DrawLine( charTopLeft, charTopRight );
	// 				Gizmos.DrawLine( charTopLeft, charBottomLeft );
	// 				Gizmos.DrawLine( charTopRight, charBottomRight );
	// 				Gizmos.DrawLine( charBottomLeft, charBottomRight );

	// 				void MakeCaretPos ( int index, bool actualLast = false ) {

	// 					var caretTarget = charBounds[index - lineIndex].Left;
	// 					var caretTop = charTop;
	// 					var caretBottom = charBottom;
	// 					var caretLeft = IsFirstCharOnLine() ? extentsLeft : charBounds[index - lineIndex - 1].Center;
	// 					var caretRight = IsLastCharOnLine() && actualLast ? extentsRight : charBounds[index - lineIndex].Center;
	// 					caretBounds[index] = new CaretBounds(
	// 						target: caretTarget,
	// 						top: caretTop,
	// 						bottom: caretBottom,
	// 						left: caretLeft,
	// 						right: caretRight
	// 					);

	// 					var caretTopLeft = transform.TransformPoint( new Vector3( caretLeft, caretTop ) );
	// 					var caretTopRight = transform.TransformPoint( new Vector3( caretRight, caretTop ) );
	// 					var caretBottomLeft = transform.TransformPoint( new Vector3( caretLeft, caretBottom ) );
	// 					var caretBottomRight = transform.TransformPoint( new Vector3( caretRight, caretBottom ) );

	// 					Gizmos.color = new Color( 0.25f, 1f, 0.5f, 0.5f );
	// 					Gizmos.DrawLine( caretTopLeft, caretTopRight );
	// 					Gizmos.DrawLine( caretTopLeft, caretBottomLeft );
	// 					Gizmos.DrawLine( caretTopRight, caretBottomRight );
	// 					Gizmos.DrawLine( caretBottomLeft, caretBottomRight );
	// 					Gizmos.color = Color.white;
	// 				}

	// 				// lol wtfff
	// 				MakeCaretPos( index: i + lineIndex );
	// 				if ( IsLastCharOnLine() ) {
	// 					MakeCaretPos( index: i + lineIndex + 1, actualLast: true );
	// 				}
	// 			}

	// 			// lines.ForEach( line => {

	// 			// 	var rt = ( transform as RectTransform );
	// 			// 	var height = CalculateLineHeight( _textU );
	// 			// 	var leading = line.info.leading;

	// 			// 	// _textU.

	// 			// 	var topLeft = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x, line.info.topY ) );
	// 			// 	var topRight = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x + rt.sizeDelta.x + _textU.rectTransform.sizeDelta.x, line.info.topY ) );

	// 			// 	var bottomLeft = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x, line.info.topY - height ) );
	// 			// 	var bottomRight = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x + rt.sizeDelta.x + _textU.rectTransform.sizeDelta.x, line.info.topY - height ) );

	// 			// 	var leadingLeft = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x, line.info.topY - height - leading ) );
	// 			// 	var leadingRight = transform.TransformPoint( new Vector3( chars[line.startIndex].cursorPos.x + rt.sizeDelta.x + _textU.rectTransform.sizeDelta.x, line.info.topY - height - leading ) );

	// 			// 	Gizmos.color = Color.white;
	// 			// 	Gizmos.DrawLine( topLeft, topRight );
	// 			// 	Gizmos.color = Color.blue;
	// 			// 	Gizmos.DrawLine( bottomLeft, bottomRight );
	// 			// 	Gizmos.color = Color.red;
	// 			// 	Gizmos.DrawLine( leadingLeft, leadingRight );
	// 			// 	Gizmos.color = Color.white;

	// 			// 	Debug.Log( $"Line {lineIndex} height: {height}; leading: {leading}" );
	// 			// 	lineIndex++;
	// 			// } );

	// 			// var charInfo = new List<CharacterInfo>();
	// 			// chars.ForEach( info => {
	// 			// 	while ( lineIndex < ( lines.Count - 1 ) && charIndex >= lines[lineIndex].startIndex ) {
	// 			// 		lineIndex++;
	// 			// 	}
	// 			// 	var line = lines[lineIndex].info;

	// 			// 	charInfo.Add(
	// 			// 		new CharacterInfo(
	// 			// 			bounds: new Rect(
	// 			// 				x: info.cursorPos.x,
	// 			// 				y: info.cursorPos.y,
	// 			// 				width: info.charWidth,
	// 			// 				height: -line.height
	// 			// 			)
	// 			// 		)
	// 			// 	);

	// 			// 	// var line = lines[lineIndex].info;
	// 			// 	// var height = line.height + line.leading;

	// 			// 	// charInfo.Add(
	// 			// 	// 	new CharacterInfo(
	// 			// 	// 		bounds: new Rect(
	// 			// 	// 			x: info.cursorPos.x,
	// 			// 	// 			y: line.topY + height,
	// 			// 	// 			width: info.charWidth,
	// 			// 	// 			height: -height
	// 			// 	// 		)
	// 			// 	// 	)
	// 			// 	// );

	// 			// 	charIndex++;
	// 			// } );




	// 			// var charInfo = ProcessAllChars( _text );
	// 			// charInfo.ForEach( info => {
	// 			// 	var topLeft = transform.TransformPoint( new Vector3( info.Bounds.xMin, info.Bounds.yMin ) );
	// 			// 	var topRight = transform.TransformPoint( new Vector3( info.Bounds.xMax, info.Bounds.yMin ) );
	// 			// 	var bottomLeft = transform.TransformPoint( new Vector3( info.Bounds.xMin, info.Bounds.yMax ) );
	// 			// 	var bottomRight = transform.TransformPoint( new Vector3( info.Bounds.xMax, info.Bounds.yMax ) );
	// 			// 	Gizmos.DrawLine( topLeft, topRight );
	// 			// 	Gizmos.DrawLine( topRight, bottomRight );
	// 			// 	Gizmos.DrawLine( bottomRight, bottomLeft );
	// 			// 	Gizmos.DrawLine( bottomLeft, topLeft );
	// 			// 	// Debug.Log( info.Bounds );
	// 			// } );
	// 		}
	// 	}

	// 	private CharacterInfo[] _charInfo;
	// 	private struct CharacterInfo {
	// 		// positioning
	// 		public Rect Bounds { get; private set; }
	// 		public float Width => Bounds.width;
	// 		public float Height => Bounds.height;

	// 		// details
	// 		// public char Character { get; private set; }

	// 		public CharacterInfo ( Rect bounds ) {
	// 			Bounds = bounds;
	// 			// Character = character;
	// 		}
	// 	}

	// 	private CaretBounds[] _caretBounds;
	// 	private void ProcessAllChars () {

	// 		var textGen = _textU.cachedTextGenerator;

	// 		var lines = textGen.GetLinesArray().Convert( lineInfo => (startIndex: lineInfo.startCharIdx, info: lineInfo) );
	// 		var chars = textGen.GetCharactersArray();

	// 		var linesArray = new (float top, float bottom)[lines.Count];
	// 		for ( int i = 0; i < lines.Count; i++ ) {

	// 			var line = lines[i];
	// 			var top = chars[line.startIndex].cursorPos.y;
	// 			var height = CalculateLineHeight( _textU );
	// 			linesArray[i] = (top: chars[line.startIndex].cursorPos.y, bottom: top - height);

	// 		}

	// 		var lineInfoArray = new LineInfo[lines.Count];
	// 		for ( int i = 0; i < lines.Count; i++ ) {

	// 			var line = linesArray[i];

	// 			// if first line, top is top of first char
	// 			// if not first line, top is mid point of this top and prev bottom
	// 			float top;
	// 			if ( i == 0 ) {
	// 				top = line.top;
	// 			} else {
	// 				var prev = linesArray[i - 1];
	// 				top = prev.bottom + ( ( line.top - prev.bottom ) / 2f );
	// 			}

	// 			// if last line, bottom is top of first char + line height
	// 			// if not last line, bottom is midpoint of (top of first + line height) and top of next line
	// 			float bottom;
	// 			if ( i == lines.Count - 1 ) {
	// 				bottom = line.bottom;
	// 			} else {
	// 				var next = linesArray[i + 1];
	// 				bottom = line.bottom + ( ( next.top - line.bottom ) / 2f );
	// 			}

	// 			lineInfoArray[i] = new LineInfo(
	// 				lineTop: line.top,
	// 				lineBottom: line.bottom,
	// 				extentsTop: top,
	// 				extentsBottom: bottom
	// 			);
	// 		}

	// 		var extentsTop = textGen.rectExtents.yMin - textGen.rectExtents.center.y + ( _textU.rectTransform.sizeDelta.y / 2f );
	// 		var extentsBottom = textGen.rectExtents.yMax - textGen.rectExtents.center.y - ( _textU.rectTransform.sizeDelta.y / 2f );
	// 		var extentsLeft = textGen.rectExtents.xMin - textGen.rectExtents.center.x + ( _textU.rectTransform.sizeDelta.x / 2f );
	// 		var extentsRight = textGen.rectExtents.xMax - textGen.rectExtents.center.x - ( _textU.rectTransform.sizeDelta.x / 2f );

	// 		var lineIndex = 0;
	// 		var charBounds = new CharBounds[chars.Length];
	// 		_caretBounds = new CaretBounds[chars.Length + lines.Count];
	// 		// Debug.Log( $"chars: {chars.Length}; lines: {lines.Count}; carets: {_caretBounds.Length}" );

	// 		// some local functions
	// 		bool IsLastLine () => lineIndex == lines.Count - 1;
	// 		(int startIndex, UILineInfo info) NextLine () => lines[lineIndex + 1];

	// 		for ( int i = 0; i < chars.Length; i++ ) {

	// 			// some local functions
	// 			bool IsFirstCharOnLine () => i == lines[lineIndex].startIndex;
	// 			bool IsLastCharOnLine () => IsLastLine() ? i == chars.Length - 1 : i == NextLine().startIndex - 1;

	// 			// if not on last line, advance lines until current index is at/above start index
	// 			while ( !IsLastLine() && i >= NextLine().startIndex ) {
	// 				lineIndex++;
	// 			}

	// 			var lineInfo = lineInfoArray[lineIndex];
	// 			var charInfo = chars[i];

	// 			var charTop = lineInfo.Extents.Top;
	// 			var charBottom = lineInfo.Extents.Bottom;
	// 			var charLeft = charInfo.cursorPos.x;
	// 			var charRight = charLeft + charInfo.charWidth;
	// 			charBounds[i] = new CharBounds(
	// 				top: charTop,
	// 				bottom: charBottom,
	// 				left: charLeft,
	// 				right: charRight
	// 			);

	// 			void MakeCaretPos ( int index, bool actualLast = false ) {

	// 				var caretTarget = actualLast ? charBounds[index].Right : charBounds[index].Left;
	// 				var caretTop = charTop;
	// 				var caretBottom = charBottom;
	// 				var caretLeft = IsFirstCharOnLine() ? extentsLeft : charBounds[index - 1].Center;
	// 				var caretRight = IsLastCharOnLine() && actualLast ? extentsRight : charBounds[index].Center;
	// 				_caretBounds[index + lineIndex + ( actualLast ? 1 : 0 )] = new CaretBounds(
	// 					target: caretTarget,
	// 					top: caretTop,
	// 					bottom: caretBottom,
	// 					left: caretLeft,
	// 					right: caretRight
	// 				);
	// 			}

	// 			// lol wtfff
	// 			// Debug.Log( $"i: {i}; lineIndex: {lineIndex}" );
	// 			MakeCaretPos( index: i );
	// 			if ( IsLastCharOnLine() ) {
	// 				MakeCaretPos( index: i, actualLast: true );
	// 			}
	// 		}
	// 	}


	// 	private void Update () {

	// 		if ( _isInside ) {
	// 			RefreshCaratIndexForMousePosition();
	// 		}
	// 	}
	// 	private void RefreshCaratIndexForMousePosition () {

	// 	}
	// 	// private void RefreshCaratIndexForMousePosition () {

	// 	// 	var cam = Camera.main;
	// 	// 	_lineIndex = TMP_TextUtilities.FindNearestLine( _text, Input.mousePosition, cam );
	// 	// 	_charIndex = TMP_TextUtilities.FindNearestCharacterOnLine( _text, Input.mousePosition, _lineIndex, cam, false );


	// 	// 	_topLeft = _text.textInfo.characterInfo[_charIndex].vertex_TL.position;
	// 	// 	_bottomRight = _text.textInfo.characterInfo[_charIndex].vertex_BR.position;

	// 	// 	_lineHeight = _lineIndex >= 0 ? _text.textInfo.lineInfo[_lineIndex].lineHeight : -1;



	// 	// 	if ( _charIndex >= 0 ) {
	// 	// 		SetCaratIndex( _charIndex );
	// 	// 	}
	// 	// }
	// 	// private void SetCaratIndex ( int index ) {

	// 	// 	var charInfo = _text.textInfo.characterInfo[index];
	// 	// 	var lineIndex = charInfo.lineNumber;
	// 	// 	var lineInfo = _text.textInfo.lineInfo[lineIndex];

	// 	// 	var lineHeight = _text.textInfo.lineInfo[lineIndex].lineHeight;
	// 	// 	var top = lineInfo.ascender;
	// 	// 	var bottom = top - lineHeight;
	// 	// 	var left = charInfo.topLeft.x;

	// 	// 	_caret.SetPosition( left, top, lineHeight );
	// 	// }


	// 	public bool _isInside;
	// 	public int _charIndex;
	// 	public int _lineIndex;

	// 	public Vector3 _topLeft = Vector3.zero;
	// 	public Vector3 _bottomRight = Vector3.zero;
	// 	public float _lineHeight;

	// 	public Vector3 _contactPoint = Vector3.zero;
	// 	public Vector3 _lastPosition = Vector3.zero;


	// 	// private functions
	// 	// private Plane GetPlane () =>
	// 	// 	new Plane( inNormal: -transform.forward, inPoint: transform.position );

	// 	// private Ray GetRay ( Vector2 position ) =>
	// 	// 	Camera.main.ScreenPointToRay( position );

	// 	// private Vector3 GetContactPoint ( Vector2 position ) {

	// 	// 	var ray = GetRay( position );
	// 	// 	GetPlane().Raycast( ray, out float distance );
	// 	// 	return ray.origin + ( ray.direction * distance );
	// 	// }


	// 	// ********** IPointer Event Handlers **********

	// 	void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

	// 		_isInside = true;
	// 		Debug.Log( $"Is inside: { _isInside}" );
	// 	}
	// 	void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {
	// 		// hmm
	// 	}
	// 	void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {
	// 		// hmm
	// 	}
	// 	void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

	// 		_isInside = false;
	// 		Debug.Log( $"Is inside: { _isInside}" );
	// 	}
	// }

}

