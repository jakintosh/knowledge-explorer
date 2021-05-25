using Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TextEdit {

	// types and static funcs
	public partial class Text {

		public enum Directions {
			Up,
			Down,
			Left,
			Right
		}

		private static int NULL_WIDTH_SPACE_ASCII_CODE = 8203;

		[Serializable]
		public struct Span : IEquatable<Span> {

			public int Min { get; private set; }
			public int Max { get; private set; }
			public int Length => Max - Min;

			public bool Equals ( Span other ) => Min.Equals( other.Min ) && Max.Equals( other.Max );
			public bool IsValid () => Min >= 0 && Max >= 0;
			public bool Contains ( int i ) => i >= Min && i <= Max;

			public Span ( int a, int b ) {

				Min = a < b ? a : b;
				Max = a > b ? a : b;
			}
		}

		[Serializable]
		public struct Bounds {

			public float Left { get; private set; }
			public float Right { get; private set; }
			public float Center => Left + ( Width / 2f );
			public float Width => Right - Left;

			public float Top { get; private set; }
			public float Bottom { get; private set; }
			public float Middle => Bottom + ( Height / 2f );
			public float Height => Top - Bottom;

			public bool Contains ( Vector2 point ) => point.x > Left && point.x < Right && point.y < Top && point.y > Bottom;

			public Bounds ( float top, float bottom, float left, float right ) {

				Top = top;
				Bottom = bottom;
				Left = left;
				Right = right;
			}
			public static Bounds FromRect ( Rect rect ) =>
				new Bounds(
					top: rect.yMax,
					bottom: rect.yMin,
					left: rect.xMin,
					right: rect.xMax
				);
		}

		[Serializable]
		public struct CharacterInfo {

			public char Character { get; private set; }
			public Bounds Extents { get; private set; }
			public Bounds Margin { get; private set; }

			public CharacterInfo ( char character, Bounds margin, Bounds extents ) {

				Character = character;
				Margin = margin;
				Extents = extents;
			}
		}

		[Serializable]
		public struct LineInfo {

			public Bounds Extents { get; private set; }
			public Bounds Margin { get; private set; }
			public Span CharacterSpan { get; private set; }
			public Span CaretSpan { get; private set; }

			public LineInfo ( Bounds margin, Bounds extents, Span chars, Span carets ) {

				Margin = margin;
				Extents = extents;
				CharacterSpan = chars;
				CaretSpan = carets;
			}
		}

		[Serializable]
		public struct CaretInfo {

			public Bounds HitBox;
			public Bounds Target;
			public int LineIndex;
			public int CharIndex;

			public CaretInfo ( int charIndex, int lineIndex, Bounds hitBox, Bounds target ) {

				LineIndex = lineIndex;
				CharIndex = charIndex;
				HitBox = hitBox;
				Target = target;
			}
		}
	}

	// component
	public partial class Text : MonoBehaviour,
		IPointerEnterHandler,
		IPointerDownHandler,
		IDragHandler,
		IPointerUpHandler,
		IPointerExitHandler {

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _textMesh;
		[SerializeField] private Image _leadingCaret;
		[SerializeField] private Image _trailingCaret;

		[Header( "UI Assets" )]
		[SerializeField] private Color _caretColor;
		[SerializeField] private Color _highlightColor;

		[Header( "UI Debug" )]
		[SerializeField] private bool _visualizeLineMargins;
		[SerializeField] private bool _visualizeLineExtents;
		[SerializeField] private bool _visualizeCharacterMargins;
		[SerializeField] private bool _visualizeCharExtents;
		[SerializeField] private bool _visualizeCaretBounds;
		[SerializeField] private bool _showContactPoint;

		// private data
		[Space]
		public Span _caretRange;
		public LineInfo[] _lineInfo;
		public CharacterInfo[] _charInfo;
		public CaretInfo[] _caretInfo;


		// observable
		private Observable<bool> _isInside;
		private Observable<bool> _isTrackingCursor;
		private Observable<Vector3?> _contactPoint;
		private Observable<int> _caretAnchorIndex;
		private Observable<int> _caretFloatIndex;
		private Observable<float> _cursorPositionX;
		private Observable<float> _cursorPositionY;


		// observable data
		/*
			* whenever the anchor index is changed, the float index changes too
			* the float index is the “active” cursor index for determining position
			* the selection changes when the text changes, or the cursor range changes
			* when the selection changes, the cursor positions are updated
			* when click goes down, position x/y are set
			* when click drags, position x/y are updated
			* when click goes up, position x/y are resolved to contained caret
			* when arrow horizontal, position x/y are set to new pos
			* when arrow vertical position y is set to new pos (x unchanged)
		*/


		// mono lifecycle
		private void Awake () {

			UnityEngine.InputSystem.Keyboard.current.onTextInput += Insert;

			// set up carets
			_leadingCaret.rectTransform.anchorMin = ( transform as RectTransform ).pivot;
			_leadingCaret.rectTransform.anchorMax = ( transform as RectTransform ).pivot;
			_trailingCaret.rectTransform.anchorMin = ( transform as RectTransform ).pivot;
			_trailingCaret.rectTransform.anchorMax = ( transform as RectTransform ).pivot;
			_leadingCaret.gameObject.SetActive( false );
			_trailingCaret.gameObject.SetActive( false );

			_caretRange = new Span( -1, -1 );

			_isInside = new Observable<bool>(
				initialValue: false,
				onChange: inside => { }
			);
			_isTrackingCursor = new Observable<bool>(
				initialValue: false,
				onChange: trackingCursor => { }
			);
			_contactPoint = new Observable<Vector3?>(
				initialValue: null,
				onChange: contactPoint => { }
			);

			_caretFloatIndex = new Observable<int>(
				initialValue: -1,
				onChange: floatIndex => {
					if ( _caretAnchorIndex == null ) { return; }
					_caretRange = new Span( _caretAnchorIndex.Get(), floatIndex );
					RefreshSelection();
				}
			);
			_caretAnchorIndex = new Observable<int>(
				initialValue: -1,
				onChange: anchorIndex => {
					_caretFloatIndex.Set( anchorIndex );
					_caretRange = new Span( anchorIndex, _caretFloatIndex.Get() );
					RefreshSelection();
				}
			);
			_cursorPositionX = new Observable<float>(
				initialValue: 0f,
				onChange: x => {

				}
			);
			_cursorPositionY = new Observable<float>(
				initialValue: 0f,
				onChange: y => {

				}
			);

			_textMesh.ForceMeshUpdate();
			ProcessAllChars( _textMesh );
		}
		private void OnDestroy () {

			UnityEngine.InputSystem.Keyboard.current.onTextInput -= Insert;
		}
		private void Update () {

			if ( _isTrackingCursor.Get() ) {
				_contactPoint.Set( GetContactPoint( Input.mousePosition ) );
			}

			// delete
			if ( Input.GetKeyDown( KeyCode.Delete ) || Input.GetKeyDown( KeyCode.Backspace ) ) {
				Delete();
			}

			if ( Input.GetKeyDown( KeyCode.UpArrow ) ) {
				MoveCaret( Directions.Up );
			}
			if ( Input.GetKeyDown( KeyCode.DownArrow ) ) {
				MoveCaret( Directions.Down );
			}
			if ( Input.GetKeyDown( KeyCode.LeftArrow ) ) {
				MoveCaret( Directions.Left );
			}
			if ( Input.GetKeyDown( KeyCode.RightArrow ) ) {
				MoveCaret( Directions.Right );
			}
		}
		private void OnDrawGizmos () {

			if ( !Application.isPlaying && ( _visualizeCharacterMargins || _visualizeCaretBounds || _showContactPoint || _visualizeLineMargins || _visualizeLineExtents ) ) {
				ProcessAllChars( _textMesh );
			}

			// draw line margin
			if ( _visualizeLineMargins ) {

				_lineInfo.ForEach( lineInfo => {

					var boundsTopLeft = transform.TransformPoint( new Vector3( lineInfo.Margin.Left, lineInfo.Margin.Top ) );
					var boundsTopRight = transform.TransformPoint( new Vector3( lineInfo.Margin.Right, lineInfo.Margin.Top ) );
					var boundsBottomLeft = transform.TransformPoint( new Vector3( lineInfo.Margin.Left, lineInfo.Margin.Bottom ) );
					var boundsBottomRight = transform.TransformPoint( new Vector3( lineInfo.Margin.Right, lineInfo.Margin.Bottom ) );

					Gizmos.color = Color.blue;
					Gizmos.DrawLine( boundsTopLeft, boundsTopRight );
					Gizmos.DrawLine( boundsTopLeft, boundsBottomLeft );
					Gizmos.DrawLine( boundsTopRight, boundsBottomRight );
					Gizmos.DrawLine( boundsBottomLeft, boundsBottomRight );
					Gizmos.color = Color.white;
				} );
			}

			// draw line margin
			if ( _visualizeLineExtents ) {

				_lineInfo.ForEach( lineInfo => {

					var boundsTopLeft = transform.TransformPoint( new Vector3( lineInfo.Extents.Left, lineInfo.Extents.Top ) );
					var boundsTopRight = transform.TransformPoint( new Vector3( lineInfo.Extents.Right, lineInfo.Extents.Top ) );
					var boundsBottomLeft = transform.TransformPoint( new Vector3( lineInfo.Extents.Left, lineInfo.Extents.Bottom ) );
					var boundsBottomRight = transform.TransformPoint( new Vector3( lineInfo.Extents.Right, lineInfo.Extents.Bottom ) );

					Gizmos.color = Color.blue;
					Gizmos.DrawLine( boundsTopLeft, boundsTopRight );
					Gizmos.DrawLine( boundsTopLeft, boundsBottomLeft );
					Gizmos.DrawLine( boundsTopRight, boundsBottomRight );
					Gizmos.DrawLine( boundsBottomLeft, boundsBottomRight );
					Gizmos.color = Color.white;
				} );
			}

			// draw char margins
			if ( _visualizeCharacterMargins ) {

				_charInfo.ForEach( charInfo => {

					var tl = transform.TransformPoint( new Vector3( charInfo.Margin.Left, charInfo.Margin.Top ) );
					var tr = transform.TransformPoint( new Vector3( charInfo.Margin.Right, charInfo.Margin.Top ) );
					var bl = transform.TransformPoint( new Vector3( charInfo.Margin.Left, charInfo.Margin.Bottom ) );
					var br = transform.TransformPoint( new Vector3( charInfo.Margin.Right, charInfo.Margin.Bottom ) );

					Gizmos.color = Color.blue;
					Gizmos.DrawLine( tl, tr );
					Gizmos.DrawLine( tl, bl );
					Gizmos.DrawLine( tr, br );
					Gizmos.DrawLine( bl, br );
					Gizmos.color = Color.white;
				} );
			}

			// draw char margins
			if ( _visualizeCharExtents ) {

				_charInfo.ForEach( charInfo => {

					var tl = transform.TransformPoint( new Vector3( charInfo.Extents.Left, charInfo.Extents.Top ) );
					var tr = transform.TransformPoint( new Vector3( charInfo.Extents.Right, charInfo.Extents.Top ) );
					var bl = transform.TransformPoint( new Vector3( charInfo.Extents.Left, charInfo.Extents.Bottom ) );
					var br = transform.TransformPoint( new Vector3( charInfo.Extents.Right, charInfo.Extents.Bottom ) );

					Gizmos.color = Color.blue;
					Gizmos.DrawLine( tl, tr );
					Gizmos.DrawLine( tl, bl );
					Gizmos.DrawLine( tr, br );
					Gizmos.DrawLine( bl, br );
					Gizmos.color = Color.white;
				} );
			}

			// draw caret bounds
			if ( _visualizeCaretBounds ) {

				int caretIndex = 0;
				_caretInfo.ForEach( caretInfo => {

					var caretTop = transform.TransformPoint( new Vector3( caretInfo.Target.Center, caretInfo.Target.Top ) );
					var caretBottom = transform.TransformPoint( new Vector3( caretInfo.Target.Center, caretInfo.Target.Bottom ) );
					var tl = transform.TransformPoint( new Vector3( caretInfo.HitBox.Left, caretInfo.HitBox.Top ) );
					var tr = transform.TransformPoint( new Vector3( caretInfo.HitBox.Right, caretInfo.HitBox.Top ) );
					var bl = transform.TransformPoint( new Vector3( caretInfo.HitBox.Left, caretInfo.HitBox.Bottom ) );
					var br = transform.TransformPoint( new Vector3( caretInfo.HitBox.Right, caretInfo.HitBox.Bottom ) );

					Gizmos.color = Color.black;
					Gizmos.DrawLine( tl, tr );
					Gizmos.DrawLine( tl, bl );
					Gizmos.DrawLine( tr, br );
					Gizmos.DrawLine( bl, br );
					Gizmos.color = new Color( 0f, 0.5f, 0.5f, 0.5f );
					Gizmos.DrawLine( caretTop, caretBottom );
					Gizmos.color = Color.white;
					caretIndex++;
				} );
			}

			// draw contact point
			if ( _showContactPoint && _contactPoint?.Get() != null ) {

				Gizmos.color = Color.red;
				var worldPoint = transform.TransformPoint( _contactPoint.Get().Value );
				Gizmos.DrawSphere( worldPoint, 1f / 256f );
				Gizmos.color = Color.white;
			}
		}

		// private methods
		private void Insert ( char c ) => Insert( c.ToString() );
		private void Insert ( string text ) {

			/*
				all chars in selection should be removed, and then
				all chars in text should be inserted, and caret should go to
				directly after the last char of the insertion
			*/

			// if no caret, abort
			if ( !_caretRange.IsValid() ) { return; }

			// cull garbage, and abort if empty now
			text = Regex.Replace( text, @"\p{C}+", string.Empty );
			if ( text.IsNullOrEmpty() ) { return; }

			var charStart = _caretInfo[_caretRange.Min].CharIndex;
			var charEnd = _caretInfo[_caretRange.Max].CharIndex;
			var range = charEnd - charStart;

			// update text and update mesh
			_textMesh.text = _textMesh.text
				.Remove( startIndex: charStart, count: range )
				.Insert( startIndex: charStart, text );
			_textMesh.ForceMeshUpdate();
			ProcessAllChars( _textMesh );

			// set new caret position
			var caretIdx = GetCaretIndex( charStart + text.Length );
			_caretAnchorIndex.Set( caretIdx );
			_caretFloatIndex.Set( caretIdx );
			RefreshSelection();
		}
		private void Delete () {

			/*
				if there is a selection, all characters selected should be removed
				and the caret should condense on the original index

				if there is no selection, the character behind the caret should be
				removed, and the caret index should be decremented
			*/


			if ( !_caretRange.IsValid() ) { return; }

			// get indices and range for deletion
			var charStart = _caretInfo[_caretRange.Min].CharIndex;
			var charEnd = _caretInfo[_caretRange.Max].CharIndex;
			var range = ( charEnd - charStart );
			int charIdx;
			if ( range == 0 ) { // delete prev char
				range = 1;
				charIdx = charStart - 1;
				if ( charIdx < 0 ) { return; } // we're at the beginning, nothing to delete
			} else {
				charIdx = charStart;
			}

			// update text and update mesh
			_textMesh.text = _textMesh.text.Remove( startIndex: charIdx, count: range );
			_textMesh.ForceMeshUpdate();
			ProcessAllChars( _textMesh );

			int caretIdx = GetCaretIndex( charIdx );
			_caretAnchorIndex.Set( caretIdx );
			_caretFloatIndex.Set( caretIdx );
			RefreshSelection();
		}

		private Vector2 _moveThingy = new Vector2();
		private bool _isReset;
		private void MoveCaret ( Directions direction ) {

			if ( !_caretRange.IsValid() ) { return; }

			// the floating caret is the one moving
			// we only update an "axis" when that's the one moving

			var expandSelection = Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift );
			var macroJump = Input.GetKey( KeyCode.LeftAlt ) || Input.GetKey( KeyCode.RightAlt );
			var horizontal = direction == Directions.Left || direction == Directions.Right;
			var vertical = direction == Directions.Up || direction == Directions.Down;

			var caretIndex = _caretFloatIndex.Get();
			if ( horizontal ) {
				if ( direction == Directions.Left ) {
					caretIndex--;
				}
				if ( direction == Directions.Right ) {
					caretIndex++;
				}
				caretIndex = Mathf.Clamp( caretIndex, 0, _caretInfo.Length - 2 );
			}

			if ( vertical ) {
				var lineNum = _caretInfo[_caretFloatIndex.Get()].LineIndex;
				if ( direction == Directions.Up ) {
					lineNum--;
				}
				if ( direction == Directions.Down ) {
					lineNum++;
				}
				if ( lineNum < 0 ) {
					caretIndex = 0;
				} else if ( lineNum < _lineInfo.Length ) {
					caretIndex = GetCaretIndex( new Vector2( _moveThingy.x, _lineInfo[lineNum].Extents.Middle ) );
				} else {
					caretIndex = _caretInfo.Length - 2;
				}
			}

			if ( !expandSelection ) {
				_caretAnchorIndex.Set( caretIndex );
			}
			_caretFloatIndex.Set( caretIndex );

			if ( horizontal ) { // only reset when we moved horizontally
				_moveThingy.x = _caretInfo[_caretFloatIndex.Get()].Target.Center;
			}
			_moveThingy.y = _caretInfo[_caretFloatIndex.Get()].HitBox.Middle;
		}

		private void ProcessAllChars ( TextMeshProUGUI textMesh ) {

			var frame = ( transform as RectTransform ).rect;

			// look at text info
			var textInfo = textMesh.text.IsNullOrEmpty() ?
				textMesh.GetTextInfo( Convert.ToChar( NULL_WIDTH_SPACE_ASCII_CODE ).ToString() ) :
				textMesh.textInfo;
			var lines = textInfo.lineInfo;
			var chars = textInfo.characterInfo;
			var numLines = textInfo.lineCount;
			var numChars = textInfo.characterCount;

			_lineInfo = new LineInfo[numLines];
			_charInfo = new CharacterInfo[numChars];
			_caretInfo = new CaretInfo[numChars + numLines + 1];

			for ( int lineNum = 0; lineNum < numLines; lineNum++ ) {

				var line = lines[lineNum];
				var prevLineNum = lineNum - 1;
				var nextLineNum = lineNum + 1;
				var isFirstLine = lineNum == 0;
				var isLastLine = lineNum == numLines - 1;

				var top = isFirstLine ?
					frame.yMax :
					line.ascender + ( ( lines[prevLineNum].descender - line.ascender ) / 2f );
				var bottom = isLastLine ?
					line.descender :
					line.descender + ( ( lines[nextLineNum].ascender - line.descender ) / 2f );

				var lineMargin = new Bounds(
					top: ConvertYToLocalSpace( frame, line.ascender ),
					bottom: ConvertYToLocalSpace( frame, line.descender ),
					left: ConvertXToLocalSpace( frame, textMesh.rectTransform.rect.xMin ),
					right: ConvertXToLocalSpace( frame, textMesh.rectTransform.rect.xMax )
				);
				var lineExtents = new Bounds(
					top: ConvertYToLocalSpace( frame, top ),
					bottom: ConvertYToLocalSpace( frame, bottom ),
					left: frame.xMin,
					right: frame.xMax
				);
				var lineCharacterSpan = new Span(
					a: line.firstCharacterIndex,
					b: line.lastCharacterIndex
				);
				var lineCaretSpan = new Span(
					a: line.firstCharacterIndex + lineNum,
					b: line.lastCharacterIndex + lineNum + 1
				);

				var lineInfo = new LineInfo(
					margin: lineMargin,
					extents: lineExtents,
					chars: lineCharacterSpan,
					carets: lineCaretSpan
				);
				_lineInfo[lineNum] = lineInfo;

				// step through each character
				for ( int charIndex = lineInfo.CharacterSpan.Min; charIndex <= lineInfo.CharacterSpan.Max; charIndex++ ) {

					var charInfo_TMP = chars[charIndex];
					var isFirstChar = charIndex == lineCharacterSpan.Min;
					var isLastChar = charIndex == lineCharacterSpan.Max;

					// create character info
					var right = isLastChar && charInfo_TMP.character == ' ' ?
						charInfo_TMP.origin + 3.544f : // manually move over for spaces
						charInfo_TMP.xAdvance; // TODO: remove this once spaces are fixed
					var charMargin = new Bounds(
						top: lineMargin.Top,
						bottom: lineMargin.Bottom,
						left: ConvertXToLocalSpace( frame, charInfo_TMP.origin ),
						right: ConvertXToLocalSpace( frame, right )
					//  right: ConvertXToLocalSpace( frame, charInfo_TMP.xAdvance )  // TODO: replace once spaces are fixed
					);
					var charExtents = new Bounds(
						top: lineExtents.Top,
						bottom: lineExtents.Bottom,
						left: ConvertXToLocalSpace( frame, charInfo_TMP.origin ),
						right: ConvertXToLocalSpace( frame, right )
					//  right: ConvertXToLocalSpace( frame, charInfo_TMP.xAdvance )  // TODO: replace once spaces are fixed
					);
					var charInfo = new CharacterInfo(
						character: charInfo_TMP.character,
						margin: charMargin,
						extents: charExtents
					);
					_charInfo[charIndex] = charInfo;

					// create caret info
					var caretHitBox = new Bounds(
						top: charExtents.Top,
						bottom: charExtents.Bottom,
						left: isFirstChar ? frame.xMin : _charInfo[charIndex - 1].Margin.Center,
						right: charMargin.Center
					);
					var caretBounds = new Bounds(
						top: charExtents.Top,
						bottom: charExtents.Bottom,
						left: charMargin.Left,
						right: charMargin.Left
					);
					_caretInfo[charIndex + lineNum] = new CaretInfo(
						charIndex: charIndex,
						lineIndex: lineNum,
						hitBox: caretHitBox,
						target: caretBounds
					);

					// create line-ending cursor sink info
					if ( isLastChar ) {
						var lastCaretHitBox = new Bounds(
							top: charExtents.Top,
							bottom: charExtents.Bottom,
							left: charMargin.Center,
							right: frame.xMax
						);
						var lastCaretBounds = new Bounds(
							top: charExtents.Top,
							bottom: charExtents.Bottom,
							left: charMargin.Right,
							right: charMargin.Right
						);
						_caretInfo[charIndex + 1 + lineNum] = new CaretInfo(
							charIndex: charIndex + 1, // + 1 because it references the char on next line
							lineIndex: lineNum,
							hitBox: lastCaretHitBox,
							target: lastCaretBounds
						);
					}
				}

				// create final cursor sink under last line
				if ( isLastLine ) {
					var sinkCaretHitBox = new Bounds(
						top: _lineInfo[_lineInfo.Length - 1].Extents.Bottom,
						bottom: frame.yMin,
						left: frame.xMin,
						right: frame.xMax
					);
					_caretInfo[numChars + numLines] = new CaretInfo(
						charIndex: numChars,
						lineIndex: lineNum,
						hitBox: sinkCaretHitBox,
						target: _caretInfo[_caretInfo.Length - 2].Target
					);
				}
			}
		}

		// helpers
		private int GetCaretIndex ( int charIndex ) {

			// if one past last char, return 2nd last caret index (before catchall)
			if ( charIndex > _lineInfo.Last().CharacterSpan.Max ) {
				return _caretInfo.Length - 2;
			}

			for ( int i = 0; i < _lineInfo.Length; i++ ) {
				if ( _lineInfo[i].CharacterSpan.Contains( charIndex ) ) {
					return charIndex + i;
				}
			}
			return -1;
		}
		private int GetCaretIndex ( Vector2 localPosition ) {

			for ( int i = 0; i < _caretInfo.Length; i++ ) {
				var bounds = _caretInfo[i].HitBox;
				if ( bounds.Contains( localPosition ) ) {
					return i;
				}
			}
			return -1;
		}
		private float ConvertXToLocalSpace ( Rect rect, float x ) {

			var pivot = ( transform as RectTransform ).pivot;
			var pivotDifference = new Vector2( 0.5f, 0.5f ) - pivot;
			var xDif = rect.width * pivotDifference.x;
			return x + xDif;
		}
		private float ConvertYToLocalSpace ( Rect rect, float y ) {

			var pivot = ( transform as RectTransform ).pivot;
			var pivotDifference = new Vector2( 0.5f, 0.5f ) - pivot;
			var yDif = rect.height * pivotDifference.y;
			return y + yDif;
		}

		// selection
		private List<Image> _selections = new List<Image>();
		private void RefreshSelection () {

			// set caret sprits active
			_leadingCaret.gameObject.SetActive( _caretRange.IsValid() );
			_trailingCaret.gameObject.SetActive( _caretRange.IsValid() );

			// clear selection sprites
			_selections.ForEach( selection => Destroy( selection.gameObject ) );
			_selections.Clear();

			if ( !_caretRange.IsValid() ) { return; }

			var range = _caretRange;

			// set caret position
			SetCaretBounds( _leadingCaret, _caretInfo[range.Min] );
			SetCaretBounds( _trailingCaret, _caretInfo[range.Max] );

			var leadingBounds = _caretInfo[range.Min];
			var trailingBounds = _caretInfo[range.Max];
			var startingLineIndex = leadingBounds.LineIndex;
			var endingLineIndex = trailingBounds.LineIndex;
			var startingLine = _lineInfo[startingLineIndex];
			var endingLine = _lineInfo[endingLineIndex];

			var left = leadingBounds.Target.Center;
			var right = trailingBounds.Target.Center;
			var top = startingLine.Extents.Top;
			var bottom = endingLine.Extents.Bottom;

			var hasWidth = right - left > 0;
			var hasHeight = hasWidth ? true : endingLineIndex - startingLineIndex > 1;

			// draw main center block
			if ( hasHeight || hasWidth ) {
				var blockTop = top;
				var blockBottom = bottom;
				if ( !hasWidth ) {
					blockTop = startingLine.Extents.Bottom;
					blockBottom = endingLine.Extents.Top;
					var temp = left;
					left = right;
					right = temp;
				}
				CreateSelectionRect( blockTop, blockBottom, left, right );
			}

			// draw left and right caps
			if ( endingLineIndex - startingLineIndex > 0 ) {
				CreateSelectionRect( // left cap
					top: startingLine.Extents.Bottom,
					bottom: bottom,
					left: startingLine.Extents.Left,
					right: left < right ? left : right
				);
				CreateSelectionRect( // right cap
					top: top,
					bottom: endingLine.Extents.Top,
					left: right > left ? right : left,
					right: endingLine.Extents.Right
				);
			}
		}
		private void CreateSelectionRect ( float top, float bottom, float left, float right ) {

			var go = new GameObject( "Selection" );
			go.transform.SetParent( transform, false );
			go.transform.SetAsFirstSibling();

			var selection = go.AddComponent<Image>();
			selection.rectTransform.anchorMin = ( transform as RectTransform ).pivot;
			selection.rectTransform.anchorMax = ( transform as RectTransform ).pivot;
			selection.color = _highlightColor;
			selection.rectTransform.offsetMax = new Vector2( right, top );
			selection.rectTransform.offsetMin = new Vector2( left, bottom );
			_selections.Add( selection );
		}
		private void SetCaretBounds ( Image caret, Text.CaretInfo location ) {

			// set position
			caret.rectTransform.anchoredPosition = new Vector2( location.Target.Center, location.Target.Top );

			// set height
			var sizeDelta = caret.rectTransform.sizeDelta;
			sizeDelta.y = location.Target.Height;
			caret.rectTransform.sizeDelta = sizeDelta;
		}

		// helpers
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

			// Debug.Log( $"Enter" );
			_isInside.Set( true );
		}
		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			// Debug.Log( $"On Down" );
			_isTrackingCursor.Set( true );
			var localPosition = GetContactPoint( eventData.position );
			var caretIndex = GetCaretIndex( localPosition );
			if ( caretIndex == _caretInfo.Length - 1 ) { caretIndex--; } // don't ever actually select the sink
			_caretAnchorIndex.Set( caretIndex );
			_caretFloatIndex.Set( caretIndex );
		}
		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			// Debug.Log( $"On Drag" );
			var localPosition = GetContactPoint( eventData.position );
			var caretIndex = GetCaretIndex( localPosition );
			if ( caretIndex == _caretInfo.Length - 1 ) { caretIndex--; } // don't ever actually select the sink
			_caretFloatIndex.Set( caretIndex );
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			// Debug.Log( $"On Up" );
			_isTrackingCursor.Set( false );
		}
		void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

			// Debug.Log( $"Exit" );
			_isInside.Set( false );
			_contactPoint.Set( null );
		}
	}

}