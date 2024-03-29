using Jakintosh.Observable;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TextEdit {

	public enum Directions {
		Up,
		Down,
		Left,
		Right
	}

	public enum EnterBehaviors {
		NewLine,
		Submit
	}

	[Serializable]
	public struct Span : IEquatable<Span> {

		public int Min => min;
		public int Max => max;
		public int Length => max - min;

		public bool Equals ( Span other ) => min.Equals( other.min ) && max.Equals( other.max );
		public bool IsValid () => min >= 0 && max >= 0;
		public bool Contains ( int i ) => i >= min && i <= max;

		public static Span Invalid => new Span( -1, -1 );

		public Span ( int a ) : this( a, a ) { }
		public Span ( int a, int b ) {

			min = a < b ? a : b;
			max = a > b ? a : b;
		}

		// backing data
		[SerializeField] private int min;
		[SerializeField] private int max;
	}

	[Serializable]
	public struct Bounds : IEquatable<Bounds> {

		public float Top => top;
		public float Bottom => bottom;
		public float Middle => bottom + ( Height / 2f );
		public float Height => top - bottom;

		public float Left => left;
		public float Right => right;
		public float Center => left + ( Width / 2f );
		public float Width => right - left;

		public Vector2 TopLeft => new Vector2( left, top );
		public Vector2 TopCenter => new Vector2( Center, top );
		public Vector2 TopRight => new Vector2( right, top );
		public Vector2 MiddleLeft => new Vector2( left, Middle );
		public Vector2 MiddleCenter => new Vector2( Center, Middle );
		public Vector2 MiddleRight => new Vector2( right, Middle );
		public Vector2 BottomLeft => new Vector2( left, bottom );
		public Vector2 BottomCenter => new Vector2( Center, bottom );
		public Vector2 BottomRight => new Vector2( right, bottom );

		public Vector2 PositionAtPivot ( Vector2 pivot ) {

			var pivotOffset = new Vector2(
				x: pivot.x * Width,
				y: pivot.y * Height
			);
			return BottomLeft + pivotOffset;
		}

		public Vector2 Size => new Vector2( Width, Height );

		// initializers
		public Bounds ( float top = 0f, float bottom = 0f, float left = 0f, float right = 0f ) {

			this.top = top;
			this.bottom = bottom;
			this.left = left;
			this.right = right;
		}
		public Bounds ( Vector2 position, Vector2 size )
			: this(
				top: position.y + size.y,
				bottom: position.y,
				left: position.x,
				right: position.x + size.x
			) { }
		public static Bounds FromRect ( Rect rect ) =>
			new Bounds(
				top: rect.yMax,
				bottom: rect.yMin,
				left: rect.xMin,
				right: rect.xMax
			);
		public static Bounds FromRectTransform ( RectTransform rt ) =>
			FromRect( rt.rect );
		public static Bounds Zero => new Bounds( 0, 0, 0, 0 );

		// operators
		public static Bounds operator - ( Bounds bounds ) {
			return new Bounds(
				top: -bounds.top,
				bottom: -bounds.bottom,
				left: -bounds.left,
				right: -bounds.right
			);
		}

		public bool Equals ( Bounds other )
			=> TopLeft.Equals( other.TopLeft ) && BottomRight.Equals( other.BottomRight );
		public bool Contains ( Vector2 point )
			=> point.x >= Left && point.x <= Right && point.y <= Top && point.y >= Bottom;
		public bool Contains ( Bounds bounds )
			=> bounds.Top <= Top && bounds.Bottom >= Bottom && bounds.Left >= Left && bounds.Right <= Right;
		public override string ToString ()
			=> $"Bounds( T: {top}, B: {bottom}, L: {left}, R: {right} )";

		public Vector2 GetDeltaToContain ( Bounds other ) {

			return new Vector2(
				x: ( other.Left - Left ).WithCeiling( 0f ) + ( other.Right - Right ).WithFloor( 0f ),
				y: ( other.Bottom - Bottom ).WithCeiling( 0f ) + ( other.Top - Top ).WithFloor( 0f )
			);
		}
		public Bounds GetMarginTo ( Bounds to ) {

			return new Bounds(
				top: to.top - top,
				bottom: bottom - to.bottom,
				left: left - to.left,
				right: to.right - right
			);
		}
		public Bounds GetRelativeMarginTo ( Vector2 otherSize, Vector2? pivot = null ) {

			var p = pivot.HasValue ? pivot.Value : new Vector2( 0.5f, 0.5f );
			var sizeDifference = otherSize - Size;
			return new Bounds(
				top: sizeDifference.y * ( 1 - p.y ),
				bottom: sizeDifference.y * p.y,
				left: sizeDifference.x * p.x,
				right: sizeDifference.x * ( 1 - p.x )
			);
		}

		public void ResizeFromPivot ( Vector2 newSize, Vector2 pivot ) {

			ExpandBy( GetRelativeMarginTo( newSize, pivot ) );
		}
		public Vector2 GetOffsetTo ( Bounds other, Vector2? pivot = null ) {
			var p = pivot.HasValue ? pivot.Value : new Vector2( 0.5f, 0.5f );
			return other.PositionAtPivot( p ) - PositionAtPivot( p );
		}
		public Vector2 GetOffsetFrom ( Bounds other, Vector2? pivot = null ) =>
			other.GetOffsetTo( this, pivot );

		public Bounds Duplicate ()
			=> new Bounds( top, bottom, left, right );
		public Bounds ExpandBy ( Bounds margin )
			=> InsetBy( -margin );
		public Bounds InsetBy ( Bounds padding ) {

			top -= padding.top;
			bottom += padding.bottom;
			left += padding.left;
			right -= padding.right;
			return this;
		}
		public Bounds MoveBy ( Vector2 offset ) {

			top += offset.y;
			bottom += offset.y;
			right += offset.x;
			left += offset.x;
			return this;
		}
		public Vector3 Clamp ( Vector3 vector )
			=> new Vector3(
				x: vector.x.ClampedBetween( Left, Right ),
				y: vector.y.ClampedBetween( Bottom, Top ),
				z: 0
			);

		public void DrawGizmos ( Transform container, Color color ) {

			var bounds = this;

			var topLeft = container.TransformPoint( bounds.TopLeft );
			var topRight = container.TransformPoint( bounds.TopRight );
			var bottomLeft = container.TransformPoint( bounds.BottomLeft );
			var bottomRight = container.TransformPoint( bounds.BottomRight );

			var oldColor = Gizmos.color;
			Gizmos.color = color;
			Gizmos.DrawLine( topLeft, topRight );
			Gizmos.DrawLine( topLeft, bottomLeft );
			Gizmos.DrawLine( topRight, bottomRight );
			Gizmos.DrawLine( bottomLeft, bottomRight );
			Gizmos.color = oldColor;
		}

		// backing data
		[SerializeField] private float top;
		[SerializeField] private float bottom;
		[SerializeField] private float left;
		[SerializeField] private float right;
	}

	// types
	public partial class Text {

		private static int NULL_WIDTH_SPACE_ASCII_CODE = 8203;

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

			public LineInfo After ( int numChars, int numCarets ) {

				var lineMargin = new Bounds(
					top: Extents.Bottom - ( Extents.Top - Margin.Top ),
					bottom: Extents.Bottom - ( Extents.Top - Margin.Top ) - Margin.Height,
					left: Margin.Left,
					right: Margin.Right
				);
				var lineExtents = new Bounds(
					top: Extents.Bottom,
					bottom: Extents.Bottom - Extents.Height,
					left: Extents.Left,
					right: Extents.Right
				);
				var lineCharacterSpan = new Span(
					a: CharacterSpan.Max + 1,
					b: CharacterSpan.Max + numChars
				);
				var lineCaretSpan = new Span(
					a: CaretSpan.Max + 1,
					b: CaretSpan.Max + numCarets
				);

				return new LineInfo(
					margin: lineMargin,
					extents: lineExtents,
					chars: lineCharacterSpan,
					carets: lineCaretSpan
				);
			}
		}

		[Serializable]
		public struct WordInfo {

			public int LineIndex { get; private set; }
			public Span CharacterSpan { get; private set; }
			public Span CaretSpan { get; private set; }

			public int CaretIndexBefore => CaretSpan.Min;
			public int CaretIndexAfter => CaretSpan.Max;

			public WordInfo ( int lineIndex, Span chars, Span carets ) {

				LineIndex = lineIndex;
				CharacterSpan = chars;
				CaretSpan = carets;
			}


			public void DrawGizmos_Carets ( CaretInfo[] carets, Transform container, Color color, Vector3? offset = null ) {

				var offsetAmt = offset.HasValue ? offset.Value : Vector3.zero;
				carets[CaretIndexBefore].Target.Duplicate().InsetBy( new Bounds( 0, 0, -0.25f, -0.25f ) ).MoveBy( offsetAmt ).DrawGizmos( container, color );
				carets[CaretIndexAfter].Target.Duplicate().InsetBy( new Bounds( 0, 0, -0.25f, -0.25f ) ).MoveBy( offsetAmt ).DrawGizmos( container, color );
			}
			public void DrawGizmos_Extents ( CharacterInfo[] characters, Transform container, Color color, Vector3? offset = null ) {

				var off = offset.HasValue ? (Vector2)offset.Value : Vector2.zero;
				var topLeft = container.TransformPoint( characters[CharacterSpan.Min].Extents.TopLeft + off );
				var topRight = container.TransformPoint( characters[CharacterSpan.Max].Extents.TopRight + off );
				var bottomLeft = container.TransformPoint( characters[CharacterSpan.Min].Extents.BottomLeft + off );
				var bottomRight = container.TransformPoint( characters[CharacterSpan.Max].Extents.BottomRight + off );

				var oldColor = Gizmos.color;
				Gizmos.color = color;
				Gizmos.DrawLine( topLeft, topRight );
				Gizmos.DrawLine( topLeft, bottomLeft );
				Gizmos.DrawLine( topRight, bottomRight );
				Gizmos.DrawLine( bottomLeft, bottomRight );
				Gizmos.color = oldColor;
			}
			public void DrawGizmos_Margins ( CharacterInfo[] characters, Transform container, Color color, Vector3? offset = null ) {

				var off = offset.HasValue ? (Vector2)offset.Value : Vector2.zero;
				var topLeft = container.TransformPoint( characters[CharacterSpan.Min].Margin.TopLeft + off );
				var topRight = container.TransformPoint( characters[CharacterSpan.Max].Margin.TopRight + off );
				var bottomLeft = container.TransformPoint( characters[CharacterSpan.Min].Margin.BottomLeft + off );
				var bottomRight = container.TransformPoint( characters[CharacterSpan.Max].Margin.BottomRight + off );

				var oldColor = Gizmos.color;
				Gizmos.color = color;
				Gizmos.DrawLine( topLeft, topRight );
				Gizmos.DrawLine( topLeft, bottomLeft );
				Gizmos.DrawLine( topRight, bottomRight );
				Gizmos.DrawLine( bottomLeft, bottomRight );
				Gizmos.color = oldColor;
			}
		}

		[Serializable]
		public struct CharacterInfo {

			public char Character { get; private set; }
			public int CharacterInt { get; private set; }
			public Bounds Extents { get; private set; }
			public Bounds Margin { get; private set; }

			public CharacterInfo ( char character, Bounds margin, Bounds extents ) {

				Character = character;
				CharacterInt = Convert.ToInt32( character );
				Margin = margin;
				Extents = extents;
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

			public CaretInfo After ( float right, float target ) {

				var caretHitBox = new Bounds(
					top: HitBox.Top,
					bottom: HitBox.Bottom,
					left: HitBox.Right,
					right: right
				);
				return InContainer(
					container: caretHitBox,
					charIndex: CharIndex + 1,
					lineIndex: LineIndex,
					target: target
				);
			}
			public static CaretInfo InContainer ( Bounds container, int charIndex, int lineIndex, float left, float right, float target ) {

				var caretHitBox = new Bounds(
					top: container.Top,
					bottom: container.Bottom,
					left: left,
					right: right
				);
				return CaretInfo.InContainer(
					container: caretHitBox,
					charIndex: charIndex,
					lineIndex: lineIndex,
					target: target
				);
			}
			public static CaretInfo InContainer ( Bounds container, int charIndex, int lineIndex, float target ) {

				var caretTarget = new Bounds(
					top: container.Top,
					bottom: container.Bottom,
					left: target,
					right: target
				);
				return new CaretInfo(
					charIndex: charIndex,
					lineIndex: lineIndex,
					hitBox: container,
					target: caretTarget
				);
			}

		}

		[Serializable]
		public class SelectionInfo : IEquatable<SelectionInfo> {

			public Span CaretSpan { get; private set; }

			public Span CharacterSpan => new Span( LeadingCaret.CharIndex, TrailingCaret.CharIndex );
			public Span LineSpan => new Span( LeadingCaret.LineIndex, TrailingCaret.LineIndex );

			public int AnchorCaretIndex => _anchorIndex;
			public int FloatCaretIndex => _floatIndex;

			public CaretInfo AnchorCaret => _caretInfo[_anchorIndex];
			public CaretInfo FloatCaret => _caretInfo[_floatIndex];
			public CaretInfo LeadingCaret => _caretInfo[CaretSpan.Min];
			public CaretInfo TrailingCaret => _caretInfo[CaretSpan.Max];

			public bool Equals ( SelectionInfo other ) => CharacterSpan.Equals( other.CharacterSpan ) && CaretSpan.Equals( other.CaretSpan );


			public SelectionInfo ( Span caretSpan, CaretInfo[] caretInfo ) {

				CaretSpan = caretSpan;
				_caretInfo = caretInfo;
			}

			public void SetAnchorCaretIndex ( int index ) {

				if ( _anchorIndex == index && _floatIndex == index ) { return; }
				_anchorIndex = index;
				_floatIndex = index;
				SetCaretRange( _anchorIndex, _floatIndex );
			}
			public void SetFloatCaretIndex ( int index ) {

				if ( _floatIndex == index ) { return; }
				_floatIndex = index;
				SetCaretRange( _anchorIndex, _floatIndex );
			}
			public void SetCaretInfo ( CaretInfo[] caretInfo ) {

				_caretInfo = caretInfo;
			}

			private int _anchorIndex = -1;
			private int _floatIndex = -1;
			private CaretInfo[] _caretInfo;

			private void SetCaretRange ( int a, int b ) {

				var newRange = new Span( a, b );
				if ( CaretSpan.Equals( newRange ) ) { return; }
				CaretSpan = newRange;
			}
		}
	}

	// component
	[RequireComponent( typeof( CanvasGroup ) )]
	// [RequireComponent( typeof( Scroll ) )]
	public partial class Text : MonoBehaviour,
		IPointerEnterHandler,
		IPointerDownHandler,
		IDragHandler,
		IPointerUpHandler,
		IPointerExitHandler,
		ISelectHandler,
		IDeselectHandler,
		ICancelHandler {

		public UnityEvent OnSubmit = new UnityEvent();
		public UnityEvent<string> OnTextChanged = new UnityEvent<string>();

		public string GetText () {

			return _text;
		}
		public void SetText ( string text ) {

			if ( text == _text ) { return; }
			if ( text == null ) { text = ""; }

			_text = text;

			_editing.Set( false );

			RenderTextMesh( _textMesh );
		}

		public void Init ()
			=> Initialize();

		public void SetEditable ( bool isEditable ) {

			_editable.Set( isEditable );

			// if ( _isEditable = isEditable ) { return; }

			// _isEditable = isEditable;

			// if ( _isEditable ) {
			// 	SetEditing( _isSelected );
			// } else {
			// 	SetEditing( false );
			// }
		}

		public void RefreshSize () {

			_scroll.RefreshFrame();
			RenderTextMesh( _textMesh );
		}

		[Header( "UI Config" )]
		[SerializeField] private string _text;
		[SerializeField] private bool _isEditable;
		[SerializeField] private EnterBehaviors _enterBehavior;

		[Header( "UI Control" )]
		[SerializeField] Scroll _scroll;

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
		[SerializeField] private bool _visualizeWordMargins;
		[SerializeField] private bool _visualizeWordExtents;
		[SerializeField] private bool _visualizeWordCarets;
		[SerializeField] private bool _visualizeCharacterMargins;
		[SerializeField] private bool _visualizeCharacterExtents;
		[SerializeField] private bool _visualizeCaretBounds;
		[SerializeField] private bool _showContactPoint;

		// ui elements
		private CanvasGroup _canvasGroup;
		private Image _primarySelection;
		private Image _leftCapSelection;
		private Image _rightCapSelection;

		// text information
		private LineInfo[] _lineInfo;
		private WordInfo[] _wordInfo;
		private CharacterInfo[] _charInfo;
		private CaretInfo[] _caretInfo;
		private SelectionInfo _selection;
		private Bounds _textBounds;

		// view model
		private Observable<bool> _selected;
		private Observable<bool> _editable;
		private Observable<bool> _editing;

		// state data
		private bool _isInitialized;
		private bool _isAwake;
		private bool _isInside;
		private bool _isDown;
		private Vector2 _intendedCaretPosition = Vector2.zero;

		// frame flags
		private bool _refreshSelection = false;
		private bool _renderTextMesh = false;
		private List<(int charIndex, string text)> _pendingTextTransformations = new List<(int charIndex, string text)>();
		private List<Func<SelectionInfo, string, (SelectionInfo, string)>> _textTransformations = new List<Func<SelectionInfo, string, (SelectionInfo, string)>>();

		// mono lifecycle
		private void Awake () {

			Initialize();
		}
		private void Update () {

			// read user input
			ReadInput();
			// }
			// private void LateUpdate () {

			// TODO: This whole "pending transformations" doesn't actually work and needs to
			//       be redesigned, esp once everything needs to be "agent event" driven for
			//       undo/redo and change tracking.

			// (SelectionInfo selection, string text) state = (_selection, _textMesh.text);
			// if ( _textTransformations.Count > 0 ) {

			// 	_textTransformations.ForEach( transformation => {
			// 		state = transformation( state.selection, state.text );
			// 	} );

			// 	_selection = state.selection;
			// 	_textMesh.text = state.text;

			// 	_renderTextMesh = true;
			// 	_refreshSelection = true;
			// 	textChanged = true;

			// 	_textTransformations.Clear();
			// }

			// if ( !_isEditing ) {
			// 	return;
			// }

			// process pending text transformations
			// bool textChanged = false;

			if ( _pendingTextTransformations.Count > 0 ) {
				if ( _pendingTextTransformations.Count > 1 ) {
					Debug.Log( $"TextEdit.Text: More than one text transformation being processed, and that will break things." );
				}
				foreach ( var textTransformation in _pendingTextTransformations ) {

					// update text
					_text = textTransformation.text;
					// if ( textChanged ) {
					OnTextChanged?.Invoke( GetText() );
					// }

					// re-render mesh
					RenderTextMesh( _textMesh ); // TODO: remove once this is better
					/*_renderTextMesh = true; // TODO: put this back in once fixed */

					// update selection
					var caretIndex = GetCaretIndexFromCharIndex( textTransformation.charIndex );
					_selection.SetAnchorCaretIndex( caretIndex );
					SetCaretIntentToFloatCaret( x: true, y: true );

					_refreshSelection = true;
				}
				_pendingTextTransformations.Clear();
			}

			if ( _renderTextMesh ) {
				RenderTextMesh( _textMesh );
				_renderTextMesh = false;
			}

			if ( _refreshSelection ) {
				RefreshSelection( _selection );
				_refreshSelection = false;
			}

			// if ( textChanged ) {
			// 	OnTextChanged?.Invoke( GetText() );
			// }
		}
		private void OnEnable () {

			_renderTextMesh = true;
		}
		private void OnDrawGizmos () {

			var scrollOffset = _scroll?.Offset ?? Vector2.zero;
			var needsLayout = (
				_visualizeLineMargins ||
				_visualizeLineExtents ||
				_visualizeWordMargins ||
				_visualizeWordExtents ||
				_visualizeWordCarets ||
				_visualizeCharacterMargins ||
				_visualizeCharacterExtents ||
				_visualizeCaretBounds
			);

			if ( !Application.isPlaying && needsLayout ) {

				_scroll.RefreshFrame();
				RenderTextMesh( _textMesh );
			}

			if ( _visualizeLineMargins ) {
				_lineInfo.ForEach( lineInfo => lineInfo.Margin.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: Color.blue ) );
			}
			if ( _visualizeLineExtents ) {
				_lineInfo.ForEach( lineInfo => lineInfo.Extents.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: Color.blue ) );
			}
			if ( _visualizeWordMargins ) {
				_wordInfo.ForEach( wordInfo => wordInfo.DrawGizmos_Margins( characters: _charInfo, container: transform, color: Color.blue, offset: scrollOffset ) );
			}
			if ( _visualizeWordExtents ) {
				_wordInfo.ForEach( wordInfo => wordInfo.DrawGizmos_Extents( characters: _charInfo, container: transform, color: Color.blue, offset: scrollOffset ) );
			}
			if ( _visualizeWordCarets ) {
				_wordInfo.ForEach( wordInfo => wordInfo.DrawGizmos_Carets( carets: _caretInfo, container: transform, color: Color.red, offset: scrollOffset ) );
			}
			if ( _visualizeCharacterMargins ) {
				_charInfo.ForEach( charInfo => charInfo.Margin.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: Color.blue ) );
			}
			if ( _visualizeCharacterExtents ) {
				_charInfo.ForEach( charInfo => charInfo.Extents.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: Color.blue ) );
			}
			if ( _visualizeCaretBounds ) {
				_caretInfo.ForEach( caretInfo => {
					caretInfo.Target.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: new Color( 0f, 0.5f, 0.5f, 0.5f ) );
					caretInfo.HitBox.MoveBy( scrollOffset ).DrawGizmos( container: transform, color: Color.black );
				} );
			}
			if ( _showContactPoint ) {
				Gizmos.color = Color.red;
				Gizmos.DrawSphere( transform.TransformPoint( _intendedCaretPosition ), 1f / 128f );
				Gizmos.color = Color.white;
			}
		}

		// state
		// private bool _isSelected;
		// private bool _isEditing;
		// private void SetSelected ( bool isSelected ) {

		// 	if ( _isSelected == isSelected ) { return; }

		// 	_isSelected = isSelected;

		// 	if ( _isSelected ) {
		// 		SetEditing( true );
		// 	} else {
		// 		SetEditing( false );
		// 	}
		// }
		// private void SetEditing ( bool isEditing ) {

		// 	// AND request with permission
		// 	isEditing = isEditing && _isEditable;

		// 	if ( _isEditing == isEditing ) { return; }

		// 	_isEditing = isEditing;

		// 	if ( _isEditing ) {
		// 	} else {
		// 		_selection = new SelectionInfo( Span.Invalid, _caretInfo );
		// 		RefreshSelection( _selection );
		// 	}
		// }

		// user manipulation functions
		private void Insert ( char c )
			=> Insert( c.ToString() );
		private void Insert ( string text ) {

			// guards
			if ( !_selection.CaretSpan.IsValid() ) { return; }
			if ( text.IsNullOrEmpty() ) { return; }

			// create transformation
			var charIndex = _selection.CharacterSpan.Min;
			var range = _selection.CharacterSpan.Length;
			var newText = _text;
			if ( range > 0 ) { newText = newText.Remove( startIndex: charIndex, count: range ); }
			newText = newText.Insert( startIndex: charIndex, text );

			// add text transformation
			_pendingTextTransformations.Add( (charIndex + text.Length, newText) );
		}
		// private (SelectionInfo selection, string text) Insert ( SelectionInfo selection, string text, string insertion ) {

		// 	var state = (selection, text);

		// 	// guards
		// 	if ( !selection.CaretSpan.IsValid() ) { return state; }
		// 	if ( insertion.IsNullOrEmpty() ) { return state; }

		// 	// create transformation
		// 	var charIndex = selection.CharacterSpan.Min;
		// 	var range = selection.CharacterSpan.Length;
		// 	if ( range > 0 ) { text = text.Remove( startIndex: charIndex, count: range ); }
		// 	text = text.Insert( startIndex: charIndex, insertion );

		// 	return ( new SelectionInfo())
		// }
		private void Delete () {

			// guards
			if ( !_selection.CaretSpan.IsValid() ) { return; }

			// create transformation
			var transformation = _selection.CharacterSpan.Length > 0 ?
				ClearSelectedSubstring() :
				Backspace();

			// add text transformation
			_pendingTextTransformations.Add( transformation );
		}
		private (int charIndex, string text) ClearSelectedSubstring () {

			if ( !_selection.CaretSpan.IsValid() ) { return (-1, _text); }
			if ( _text.IsNullOrEmpty() ) { return (-1, _text); }

			var newText = _text.Remove(
				startIndex: _selection.CharacterSpan.Min,
				count: _selection.CharacterSpan.Length
			);

			return (_selection.CharacterSpan.Min, newText);
		}
		private (int charIndex, string text) Backspace () {

			if ( _selection.CaretSpan.Min <= 0 ) { return (0, _text); }

			var isWordMovement = Input.GetKey( KeyCode.LeftAlt ) || Input.GetKey( KeyCode.RightAlt );
			var isLineMovement = Input.GetKey( KeyCode.LeftCommand ) || Input.GetKey( KeyCode.RightCommand );

			// determine caret index for backspace
			var caretIndex = _selection.CaretSpan.Min - 1;
			if ( isWordMovement ) {
				caretIndex = GetPreviousWordStartCaretIndex( _selection.CharacterSpan.Min );
			} else if ( isLineMovement ) {
				caretIndex = _lineInfo[_selection.LineSpan.Min].CaretSpan.Min;
			}

			var startIndex = _caretInfo[caretIndex].CharIndex;
			var count = _selection.CharacterSpan.Min - startIndex;
			var newText = _text.Remove( startIndex, count );

			return (startIndex, newText);
		}
		private void MoveCaret ( Directions direction ) {

			// ensure a caret range to manipulate
			if ( !_selection.CaretSpan.IsValid() ) { return; }

			// get some helper vars
			var isExpandingSelection = Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift );
			var isWordMovement = Input.GetKey( KeyCode.LeftAlt ) || Input.GetKey( KeyCode.RightAlt );
			var isLineMovement = Input.GetKey( KeyCode.LeftCommand ) || Input.GetKey( KeyCode.RightCommand );
			var isHorizontalMove = direction == Directions.Left || direction == Directions.Right;
			var isVerticalMove = direction == Directions.Up || direction == Directions.Down;

			var caretIndex = _selection.FloatCaretIndex;
			var floatCaret = _selection.FloatCaret;
			var currentLine = _lineInfo[floatCaret.LineIndex];
			var forceUpdateXIntent = false;

			if ( isHorizontalMove ) {

				if ( isLineMovement ) {

					caretIndex = direction switch {
						Directions.Left => currentLine.CaretSpan.Min,
						Directions.Right => currentLine.CaretSpan.Max,
						_ => caretIndex
					};

				} else if ( isWordMovement ) {

					caretIndex = direction switch {
						Directions.Left => GetPreviousWordStartCaretIndex( floatCaret.CharIndex ),
						Directions.Right => GetNextWordEndCaretIndex( floatCaret.CharIndex ),
						_ => caretIndex
					};

				} else {

					caretIndex += direction switch {
						Directions.Left => -1,
						Directions.Right => 1,
						_ => 0
					};
				}

			} else if ( isVerticalMove ) {

				if ( isLineMovement ) {

					caretIndex = direction switch {
						Directions.Up => 0,
						Directions.Down => _caretInfo.LastIndex(),
						_ => 0
					};

				} else {

					var lineNum = floatCaret.LineIndex + direction switch {
						Directions.Up => -1,
						Directions.Down => 1,
						_ => 0
					};
					if ( lineNum < 0 ) {
						caretIndex = 0;
						forceUpdateXIntent = true;
					} else if ( lineNum < _lineInfo.Length ) {
						var lineY = _lineInfo[lineNum].Extents.Middle;
						caretIndex = GetCaretIndexFromTextPosition( new Vector2( _intendedCaretPosition.x, lineY ) );
					} else {
						caretIndex = _caretInfo.LastIndex();
						forceUpdateXIntent = true;
					}
				}
			}
			caretIndex = Mathf.Clamp( caretIndex, 0, _caretInfo.LastIndex() );

			if ( isExpandingSelection ) {
				_selection.SetFloatCaretIndex( caretIndex );
			} else {
				_selection.SetAnchorCaretIndex( caretIndex );
			}

			// only update x-intent if moved horizontally, or went outside vertical range
			SetCaretIntentToFloatCaret( x: isHorizontalMove || forceUpdateXIntent, y: true );
			_refreshSelection = true;
		}
		private void SelectAll () {

			_selection.SetAnchorCaretIndex( 0 );
			_selection.SetFloatCaretIndex( _caretInfo.LastIndex() );
			_refreshSelection = true;
			SetCaretIntentToFloatCaret( x: true, y: true );
		}
		private void Copy () {

			if ( !_selection.CaretSpan.IsValid() ) { return; }
			GUIUtility.systemCopyBuffer = GetSelectionString();
		}
		private void Cut () {

			if ( !_selection.CaretSpan.IsValid() ) { return; }
			Copy();
			Delete();
		}
		private void Paste () {

			if ( !_selection.CaretSpan.IsValid() ) { return; }
			Insert( GUIUtility.systemCopyBuffer );
		}

		// processing
		private void Initialize () {

			if ( _isInitialized ) { return; }
			_isInitialized = true;

			// init data
			_lineInfo = new LineInfo[0];
			_wordInfo = new WordInfo[0];
			_charInfo = new CharacterInfo[0];
			_caretInfo = new CaretInfo[0];
			_selection = new SelectionInfo(
				caretSpan: Span.Invalid,
				caretInfo: _caretInfo
			);

			// get components
			_canvasGroup = GetComponent<CanvasGroup>();

			// init scroll
			_scroll.Init();

			// set up carets
			InitCaret( _leadingCaret );
			InitCaret( _trailingCaret );

			// set up selections
			_primarySelection = CreateSelectionRect( "Primary Selection" );
			_leftCapSelection = CreateSelectionRect( "Left-cap Selection" );
			_rightCapSelection = CreateSelectionRect( "Right-cap Selection" );


			// init observables
			_editing = new Observable<bool>(
				initialValue: false,
				onChange: editing => {
					if ( !editing ) {
						_selection = new SelectionInfo( Span.Invalid, _caretInfo );
						RefreshSelection( _selection );
					}
				}
			);
			_selected = new Observable<bool>(
				initialValue: false,
				onChange: selected => {
					_editing.Set( selected );
				}
			);
			_editable = new Observable<bool>(
				initialValue: _isEditable,
				onChange: editable => {
					_canvasGroup.blocksRaycasts = editable;
					_canvasGroup.interactable = editable;
					_editing.Set( _selected.Get() && editable );
				}
			);


			// refresh everything
			RenderTextMesh( _textMesh );
			RefreshSelection( _selection );

			// listen to events
			_scroll.OnOffsetChanged.AddListener( _ => {
				_refreshSelection = true;
			} );
			_scroll.OnViewportBoundsChanged.AddListener( _ => {
				_refreshSelection = true;
				_renderTextMesh = true;
			} );
			_scroll.OnContentBoundsChanged.AddListener( _ => {
				_refreshSelection = true;
				// _renderTextMesh = true;
			} );
		}
		private void ReadInput () {

			if ( !_editing.Get() ) { return; }

			// commands
			bool executedCommand = false;
			if ( Input.GetKey( KeyCode.LeftCommand ) || Input.GetKey( KeyCode.RightCommand ) ) {
				if ( Input.GetKeyDown( KeyCode.A ) ) { SelectAll(); executedCommand = true; }
				if ( Input.GetKeyDown( KeyCode.C ) ) { Copy(); executedCommand = true; }
				if ( Input.GetKeyDown( KeyCode.X ) ) { Cut(); executedCommand = true; }
				if ( Input.GetKeyDown( KeyCode.V ) ) { Paste(); executedCommand = true; }
			}

			if ( !executedCommand && !Input.inputString.IsNullOrEmpty() ) { Insert( FilterValidCharacterInput( Input.inputString ) ); }
			if ( Input.GetKeyDown( KeyCode.Delete ) || Input.GetKeyDown( KeyCode.Backspace ) ) { Delete(); }
			if ( Input.GetKeyDown( KeyCode.UpArrow ) ) { MoveCaret( Directions.Up ); }
			if ( Input.GetKeyDown( KeyCode.DownArrow ) ) { MoveCaret( Directions.Down ); }
			if ( Input.GetKeyDown( KeyCode.LeftArrow ) ) { MoveCaret( Directions.Left ); }
			if ( Input.GetKeyDown( KeyCode.RightArrow ) ) { MoveCaret( Directions.Right ); }
			if ( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) ) {
				switch ( _enterBehavior ) {
					case EnterBehaviors.NewLine:
						Insert( Environment.NewLine );
						break;
					case EnterBehaviors.Submit:
						_selected.Set( false );
						OnSubmit?.Invoke();
						break;
				};
			}
		}
		private void RenderTextMesh ( TextMeshProUGUI textMesh ) {

			// it would be nice to make this optional, but TMP
			// shits the bed anyway if disabled
			if ( !gameObject.activeInHierarchy ) {
				return;
			}

			//
			// Step 1 ) Re-render TextMesh and extract all metadata

			textMesh.text = _text.IsNullOrEmpty() ? GetNullWidthString() : _text;
			textMesh.ForceMeshUpdate();
			LayoutRebuilder.ForceRebuildLayoutImmediate( textMesh.rectTransform );

			var textInfo_tmp = textMesh.textInfo;
			var lines_tmp = textInfo_tmp.lineInfo;
			var chars_tmp = textInfo_tmp.characterInfo;
			var numLines_tmp = textInfo_tmp.lineCount;
			var numChars_tmp = textInfo_tmp.characterCount;

			// create info arrays
			var chars = new List<CharacterInfo>( capacity: numChars_tmp ); // this count we can be sure of ahead of time
			var lines = new List<LineInfo>( capacity: numLines_tmp + 1 ); // this is a best guess, but not definitive
			var words = new List<WordInfo>( capacity: textInfo_tmp.wordCount ); // use textInfo.wordCount as a guess; our calculation of words is different
			var carets = new List<CaretInfo>( capacity: numChars_tmp + numLines_tmp + 1 ); // this is a best guess, and most likely a maximum


			//
			// Step 2 ) Process all geometry data into buffers

			var rt = ( transform as RectTransform );
			var bounds = Bounds.FromRect( rt.rect );
			var textMeshRt = textMesh.rectTransform;
			var textMeshBounds = Bounds.FromRect( textMeshRt.rect );
			var pivotOffset = GetPivotOffsetBetween( from: textMeshRt, to: rt );
			var viewportInset = _scroll.ViewportInset;
			var lineMarginHeight = numLines_tmp switch {
				0 => 0, // technically impossible
				_ => lines_tmp[0].ascender - lines_tmp[0].descender
			};
			var lineExtentsHeight = numLines_tmp switch {
				0 => 0, // technically impossible
						// _ => lines_tmp[0].lineExtents.max.y - lines_tmp[0].lineExtents.min.y
				1 => lineMarginHeight,
				_ => lines_tmp[0].ascender - lines_tmp[1].ascender
			};

			// TODO: how do we tell if the frame can't support a single character width?

			// read each line
			for ( int lineIndex = 0; lineIndex < numLines_tmp; lineIndex++ ) {

				var line = lines_tmp[lineIndex];

				// line dimensions
				var lineTop = line.ascender - pivotOffset.y - viewportInset.Top;
				var lineBottom = line.descender - pivotOffset.y - viewportInset.Top;
				var lineExtentsTop = lineIndex == 0 ? bounds.Top - viewportInset.Top : lineTop;
				var lineExtentsBottom = lineTop - lineExtentsHeight;

				var lineMargin = new Bounds(
					top: lineTop,
					bottom: lineBottom,
					left: bounds.Left + viewportInset.Left,
					right: bounds.Right - viewportInset.Right
				);
				var lineExtents = new Bounds(
					top: lineExtentsTop,
					bottom: lineExtentsBottom,
					left: bounds.Left,
					right: bounds.Right
				);
				var lineCharacterSpan = new Span(
					a: line.firstCharacterIndex,
					b: line.lastCharacterIndex
				);
				var lastCharIsNewLine = IsNewLineCharacter( chars_tmp[line.lastCharacterIndex].character );
				var caretSpanStart = lineIndex == 0 ? 0 : lines.Last().CaretSpan.Max + 1;
				var caretSpanEnd = caretSpanStart + lineCharacterSpan.Length + ( lastCharIsNewLine ? 0 : 1 );
				var lineCaretSpan = new Span(
					a: caretSpanStart,
					b: caretSpanEnd
				);
				var lineInfo = new LineInfo(
					margin: lineMargin,
					extents: lineExtents,
					chars: lineCharacterSpan,
					carets: lineCaretSpan
				);
				lines.Add( lineInfo );

				// read each character
				int wordCharStart = -1;
				int wordCaretStart = -1;
				for ( int charIndex = line.firstCharacterIndex; charIndex <= line.lastCharacterIndex; charIndex++ ) {

					// helper info
					var char_tmp = chars_tmp[charIndex];
					var isFirstChar = charIndex == line.firstCharacterIndex;
					var isLastChar = charIndex == line.lastCharacterIndex;
					var isEmptyCharacter = IsNewLineCharacter( char_tmp.character ) || IsNullWidthSpace( char_tmp.character );

					// determine word status
					var isWordActive = wordCharStart > -1;
					var isWordCharacter = !IsNonWordCharacter( char_tmp.character );
					if ( isWordCharacter && !isWordActive ) {
						wordCharStart = charIndex;
						wordCaretStart = carets.Count;
					}
					if ( isWordActive && ( !isWordCharacter || isLastChar ) ) { // the word is over
						words.Add( new WordInfo(
							lineIndex: lineIndex,
							chars: new Span( wordCharStart, isWordCharacter ? charIndex : charIndex - 1 ), // if space, go back one
							carets: new Span( wordCaretStart, isWordCharacter ? carets.Count + 1 : carets.Count ) // if not space, go forward one
						) );
						wordCharStart = -1;
					}


					// generate character position info
					// var horizontalOffset = ( ( viewportInset.Left - viewportInset.Right ) / 2f ) - pivotOffset.x;
					var leftWeight = 1f - textMeshRt.anchorMin.x;
					var rightWeight = textMeshRt.anchorMax.x;
					var leftOffset = viewportInset.Left * leftWeight;
					var rightOffset = viewportInset.Right * rightWeight;
					var insetOffset = ( leftOffset - rightOffset ) / ( leftWeight + rightWeight );
					var horizontalOffset = insetOffset - pivotOffset.x;
					var charLeft = char_tmp.origin + horizontalOffset;
					var charRight = isLastChar && char_tmp.character == ' ' ?
						char_tmp.origin + 3.544f + horizontalOffset : // manually move over for spaces
						char_tmp.xAdvance + horizontalOffset; // TODO: remove this once spaces are fixed
					var charMargin = new Bounds(
						top: lineTop,
						bottom: lineBottom,
						left: charLeft,
						right: charRight //  right: charInfo_TMP.xAdvance - frameOffset.x  // TODO: replace once spaces are fixed
					);
					var charExtents = new Bounds(
						top: lineExtents.Top,
						bottom: lineExtents.Bottom,
						left: charLeft,
						right: charRight //  right: charInfo_TMP.xAdvance - frameOffset.x  // TODO: replace once spaces are fixed
					);
					var charInfo = new CharacterInfo(
						character: char_tmp.character,
						margin: charMargin,
						extents: charExtents
					);
					chars.Add( charInfo );


					// generate caret info
					var caret = CaretInfo.InContainer(
						container: lineExtents,
						charIndex: charIndex,
						lineIndex: lineIndex,
						left: isFirstChar ? bounds.Left : chars[charIndex - 1].Margin.Center,
						right: isLastChar && isEmptyCharacter ? bounds.Right : charMargin.Center,
						target: charMargin.Left
					);
					carets.Add( caret );

					// add extra line-end caret when relevant
					if ( isLastChar && !isEmptyCharacter ) {
						var endCaret = caret.After(
							right: bounds.Right,
							target: charMargin.Right
						);
						carets.Add( endCaret );
					}
				}
			}

			// special case for last char is newline
			if ( IsNewLineCharacter( chars.Last().Character ) ) {

				// create new line after last
				var lineInfo = lines.Last().After(
					numChars: 1,
					numCarets: 1
				);
				lines.Add( lineInfo );

				// create caret for line
				var lineCaretInfo = CaretInfo.InContainer(
					container: lineInfo.Extents,
					charIndex: numChars_tmp, // points to just after the last character
					lineIndex: lines.Count - 1, // last line
					target: lineInfo.Margin.Left
				);
				carets.Add( lineCaretInfo );
			}

			// save out the words
			_lineInfo = lines.ToArray();
			_wordInfo = words.ToArray();
			_charInfo = chars.ToArray();
			_caretInfo = carets.ToArray();

			// send updated caret info to selection
			_selection?.SetCaretInfo( _caretInfo );


			// 
			// Step 3 ) Update scroll content size

			var firstLine = _lineInfo.First();
			var lastLine = _lineInfo.Last();
			_textBounds = new Bounds(
				top: firstLine.Extents.Top,
				bottom: lastLine.Extents.Bottom,
				left: firstLine.Margin.Left,
				right: _lineInfo.Length > 1 ?
					firstLine.Margin.Right :
					_charInfo.Last().Margin.Right
			);
			_scroll.SetPreferredContentSize( _textBounds.Size );
		}
		private void RefreshSelection ( SelectionInfo selection ) {

			// enable/disable game objects
			var rangeValid = selection.CaretSpan.IsValid();
			_leadingCaret.gameObject.SetActive( rangeValid );
			_trailingCaret.gameObject.SetActive( rangeValid );
			if ( !rangeValid ) {
				_primarySelection?.gameObject.SetActive( false );
				_leftCapSelection?.gameObject.SetActive( false );
				_rightCapSelection?.gameObject.SetActive( false );
				return;
			}

			// render carets
			SetCaretBounds( _leadingCaret.rectTransform, selection.LeadingCaret.Target );
			SetCaretBounds( _trailingCaret.rectTransform, selection.TrailingCaret.Target );

			// render selection rects
			var leadingCaret = selection.LeadingCaret;
			var trailingCaret = selection.TrailingCaret;
			var startingLineIndex = leadingCaret.LineIndex;
			var endingLineIndex = trailingCaret.LineIndex;
			var startingLine = _lineInfo[startingLineIndex];
			var endingLine = _lineInfo[endingLineIndex];

			var left = leadingCaret.Target.Center;
			var right = trailingCaret.Target.Center;
			var top = startingLine.Extents.Top;
			var bottom = endingLine.Extents.Bottom;

			var hasWidth = right - left > 0;
			var hasHeight = hasWidth ? true : endingLineIndex - startingLineIndex > 1;
			var showPrimarySelection = hasWidth || hasHeight;
			var showCapSelections = endingLineIndex - startingLineIndex > 0;

			_primarySelection.gameObject.SetActive( showPrimarySelection );
			_leftCapSelection.gameObject.SetActive( showCapSelections );
			_rightCapSelection.gameObject.SetActive( showCapSelections );

			if ( showPrimarySelection ) {
				var blockTop = top;
				var blockBottom = bottom;
				if ( !hasWidth ) {
					blockTop = startingLine.Extents.Bottom;
					blockBottom = endingLine.Extents.Top;
					var temp = left;
					left = right;
					right = temp;
				}
				SetContentRect(
					rt: _primarySelection.rectTransform,
					top: blockTop,
					bottom: blockBottom,
					left: left,
					right: right
				);
			}
			if ( showCapSelections ) {
				SetContentRect(
					rt: _leftCapSelection.rectTransform,
					top: startingLine.Extents.Bottom,
					bottom: bottom,
					left: startingLine.Extents.Left,
					right: left < right ? left : right
				);
				SetContentRect(
					rt: _rightCapSelection.rectTransform,
					top: top,
					bottom: endingLine.Extents.Top,
					left: right > left ? right : left,
					right: endingLine.Extents.Right
				);
			}
		}


		// getters and setters
		private int GetCaretIndexFromCharIndex ( int charIndex ) {

			// if past last char, return last caret index
			if ( _lineInfo.Last().CharacterSpan.IsValid() ) {
				if ( charIndex > _lineInfo.Last().CharacterSpan.Max ) {
					return _caretInfo.LastIndex();
				}
			}

			for ( int i = 0; i < _lineInfo.Length; i++ ) {
				var line = _lineInfo[i];
				if ( line.CharacterSpan.Contains( charIndex ) ) {
					return ( charIndex - line.CharacterSpan.Min ) + line.CaretSpan.Min;
				}
			}
			return -1;
		}
		private int GetCaretIndexFromLocalPosition ( Vector2 localPosition )
			=> GetCaretIndexFromTextPosition( localPosition - _scroll.Offset );
		private int GetCaretIndexFromTextPosition ( Vector2 textPosition ) {

			var pos = ClampTextPositionToTextBounds( textPosition );
			for ( int i = 0; i < _lineInfo.Length; i++ ) {
				var line = _lineInfo[i];
				if ( line.Extents.Contains( pos ) ) {
					for ( int j = line.CaretSpan.Min; j <= line.CaretSpan.Max; j++ ) {
						if ( _caretInfo[j].HitBox.Contains( pos ) ) {
							return j;
						}
					}
				}
			}
			return -1;
		}
		private int GetWordIndexFromCharIndex ( int charIndex ) {

			if ( _wordInfo.Length == 0 ) { return int.MinValue; }

			for ( int i = 0; i < _wordInfo.Length; i++ ) {
				if ( charIndex <= _wordInfo[i].CharacterSpan.Max ) {
					return i;
				}
			}
			return int.MaxValue;
		}
		private int GetNextWordEndCaretIndex ( int charIndex ) {

			// get current word, if invalid we're at the end
			var wordIndex = GetWordIndexFromCharIndex( charIndex );
			if ( wordIndex == int.MaxValue ) { wordIndex = _wordInfo.Length - 1; }

			// check invalidated for current word
			var isInvalidated = charIndex >= _caretInfo[_wordInfo[wordIndex].CaretIndexAfter].CharIndex;
			if ( isInvalidated ) { wordIndex++; }

			// if we're after end, return last
			if ( wordIndex >= _wordInfo.Length ) {
				return _caretInfo.LastIndex(); // ignore sink
			} else {
				wordIndex = Mathf.Clamp( wordIndex, 0, _wordInfo.Length - 1 );
				return _wordInfo[wordIndex].CaretIndexAfter;
			}
		}
		private int GetPreviousWordStartCaretIndex ( int charIndex ) {

			// get current word, if invalid we're at the end
			var wordIndex = GetWordIndexFromCharIndex( charIndex );
			if ( wordIndex == int.MaxValue ) { wordIndex = _wordInfo.Length - 1; }

			// check invalidated for current word
			var isInvalidated = charIndex <= _caretInfo[_wordInfo[wordIndex].CaretIndexBefore].CharIndex;
			if ( isInvalidated ) { wordIndex--; }

			// if we're before beginning, return 0
			if ( wordIndex < 0 ) {
				return 0;
			} else {
				wordIndex = Mathf.Clamp( wordIndex, 0, _wordInfo.Length - 1 );
				return _wordInfo[wordIndex].CaretIndexBefore;
			}
		}
		private string GetSelectionString () {

			return _selection.CaretSpan.IsValid() ?
				_text.Substring( startIndex: _selection.CharacterSpan.Min, length: _selection.CharacterSpan.Length ) :
				"";
		}

		private Vector2 GetPivotOffsetBetween ( RectTransform from, RectTransform to ) {

			var pivotDifference = ( to.pivot - from.pivot );
			return new Vector2(
				x: to.rect.width * pivotDifference.x,
				y: to.rect.height * pivotDifference.y
			);
		}
		private Vector2 GetLocal2DPositionFromScreenPoint ( Vector2 screenPosition ) {

			var ray = Camera.main.ScreenPointToRay( screenPosition );
			var plane = new Plane( inNormal: -transform.forward, inPoint: transform.position );
			plane.Raycast( ray, out float distance );
			var contactPoint = ray.origin + ( ray.direction * distance );
			return transform.InverseTransformPoint( contactPoint );
		}
		private Vector2 ClampLocalPositionToFrame ( Vector2 localPosition ) {

			var frame = ( transform as RectTransform ).rect;
			var x = Mathf.Clamp( localPosition.x, frame.xMin, frame.xMax );
			var y = Mathf.Clamp( localPosition.y, frame.yMin, frame.yMax );
			return new Vector2( x, y );
		}
		private Vector2 ClampTextPositionToTextBounds ( Vector2 textPosition )
			=> _textBounds.Clamp( textPosition );

		private void SetCaretBounds ( RectTransform rt, Bounds bounds ) {

			rt.anchoredPosition = bounds.TopCenter + _scroll.Offset;

			var sizeDelta = rt.sizeDelta;
			sizeDelta.y = bounds.Height;
			rt.sizeDelta = sizeDelta;
		}
		private void SetContentRect ( RectTransform rt, float top, float bottom, float left, float right ) {

			rt.offsetMax = new Vector2( right, top ) + _scroll.Offset;
			rt.offsetMin = new Vector2( left, bottom ) + _scroll.Offset;
		}
		private void SetCaretIndexFromLocalPosition ( Vector2 localPosition, bool setAnchor ) {

			var clampedPosition = ClampLocalPositionToFrame( localPosition );
			var caretIndex = GetCaretIndexFromLocalPosition( clampedPosition );
			if ( setAnchor ) {
				_selection.SetAnchorCaretIndex( caretIndex );
			} else {
				_selection.SetFloatCaretIndex( caretIndex );
			}
			SetCaretIntentToFloatCaret( x: true, y: true );
			RefreshSelection( _selection );
		}
		private void SetCaretIntentToFloatCaret ( bool x, bool y ) {

			if ( !_selection.CaretSpan.IsValid() ) {
				_intendedCaretPosition = Vector2.zero;
				return;
			}

			var position = _selection.FloatCaret.Target.MiddleCenter;
			if ( x ) { _intendedCaretPosition.x = position.x; }
			if ( y ) { _intendedCaretPosition.y = position.y; }
			if ( y ) { _scroll.ScrollToContentRect( _selection.FloatCaret.Target ); }
		}


		// text processing
		private string GetNullWidthString ()
			=> Convert.ToChar( NULL_WIDTH_SPACE_ASCII_CODE ).ToString();
		private bool IsNullWidthSpace ( char character )
			=> Convert.ToChar( NULL_WIDTH_SPACE_ASCII_CODE ).Equals( character );
		private bool IsNewLineCharacter ( char c ) {
			return Regex.IsMatch( c.ToString(), @"(\r\n|\r|\n)" );
		}
		private bool IsNonWordCharacter ( char c ) {
			return char.IsWhiteSpace( c ) || char.IsPunctuation( c );
		}
		private string FilterValidCharacterInput ( string input ) {
			return Regex.Replace( input, @"\p{C}+", string.Empty );
		}


		// create stuff
		private void InitCaret ( Image caret ) {

			caret.rectTransform.anchorMin = ( transform as RectTransform ).pivot;
			caret.rectTransform.anchorMax = ( transform as RectTransform ).pivot;
			caret.gameObject.SetActive( false );
		}
		private Image CreateSelectionRect ( string name ) {

			var go = new GameObject( name );
			go.transform.SetParent( transform, false );
			go.transform.SetAsFirstSibling();

			var selection = go.AddComponent<Image>();
			selection.rectTransform.anchorMin = ( transform as RectTransform ).pivot;
			selection.rectTransform.anchorMax = ( transform as RectTransform ).pivot;
			selection.color = _highlightColor;
			return selection;
		}


		// ********** IPointer Event Handlers **********

		void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

			_isInside = true;
		}
		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			_isDown = true;

			if ( !_editable.Get() ) { return; }

			// set selected
			EventSystem.current.SetSelectedGameObject( gameObject, eventData );

			var localPosition = GetLocal2DPositionFromScreenPoint( eventData.position );
			SetCaretIndexFromLocalPosition( localPosition, setAnchor: !( Input.GetKey( KeyCode.LeftShift ) || Input.GetKey( KeyCode.RightShift ) ) );
		}
		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			if ( !_editable.Get() ) { return; }

			var localPosition = GetLocal2DPositionFromScreenPoint( eventData.position );
			SetCaretIndexFromLocalPosition( localPosition, setAnchor: false );
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			_isDown = false;
		}
		void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

			_isInside = false;
		}
		void ISelectHandler.OnSelect ( BaseEventData eventData ) {

			_selected.Set( true );
		}
		void IDeselectHandler.OnDeselect ( BaseEventData eventData ) {

			_selected.Set( false );
		}
		void ICancelHandler.OnCancel ( BaseEventData eventData ) {

			_selected.Set( false );
		}
	}

}