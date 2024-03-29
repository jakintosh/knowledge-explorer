using Jakintosh.Observable;
using Jakintosh.View;
using UnityEngine;
using UnityEngine.UI;

namespace Library.Views {

	public enum DocumentModes {
		Edit,
		Reorder,
		Read
	}

	public class Document : View {

		[Header( "UI Control" )]
		[SerializeField] private DraggableUIControl _corner;
		[SerializeField] private DocumentModeToolbar _documentToolbar;
		[SerializeField] private TextEdit.Scroll _scroll;
		[SerializeField] private TextEdit.Text _textEdit;
		[SerializeField] private BlockEdit _blockEdit;

		[Header( "UI Configuration" )]
		[SerializeField] private Vector2 _minDocumentSize;
		[SerializeField] private Vector2 _maxDocumentSize;

		[Header( "UI Display" )]
		[SerializeField] private Panel _panel;
		[SerializeField] private Canvas _canvas;
		[SerializeField] private Image _background;

		// observables
		private Observable<Vector2> _documentSize;
		private Observable<DocumentModes> _documentMode;

		private Vector3 _dragPosition;

		protected override void OnInitialize () {

			// init subviews
			_panel.Init();
			_documentToolbar.Init();
			_blockEdit.Init();
			_textEdit.Init();

			// init observables
			_documentSize = new Observable<Vector2>(
				initialValue: ( _canvas.transform as RectTransform ).rect.size,
				onChange: size => {
					_panel.SetSizeFromCanvas( size );
					_scroll.SetViewportInset( left: size.x * 0.2f, right: size.x * 0.2f );
					_textEdit.RefreshSize();
				}
			);
			_documentMode = new Observable<DocumentModes>(
				initialValue: _documentToolbar.Mode,
				onChange: mode => {
					_textEdit.SetEditable( mode == DocumentModes.Edit );
					if ( mode == DocumentModes.Reorder ) {

					}
				}
			);

			// subscribe to controls
			_documentToolbar.OnDocumentModeChanged.AddListener( mode => {
				_documentMode.Set( mode );
			} );
			_corner.OnDragBegin.AddListener( () => {
				_dragPosition = GetBottomRightWorldPosition();
			} );
			_corner.OnDragDelta.AddListener( delta => {
				delta.y = 0;
				_dragPosition += delta;
				var size = GetSize( GetTopLeftWorldPosition(), _dragPosition ).Clamp( _minDocumentSize, _maxDocumentSize );
				_documentSize.Set( size );
			} );
		}
		protected override void OnCleanup () { }




		private Vector3 GetTopLeftWorldPosition () {

			var corners = new Vector3[4];
			( _canvas.transform as RectTransform ).GetWorldCorners( corners );
			return corners[1];
		}
		private Vector3 GetBottomRightWorldPosition () {

			var corners = new Vector3[4];
			( _canvas.transform as RectTransform ).GetWorldCorners( corners );
			return corners[3];
		}
		private Vector2 GetSize ( Vector3 topLeftWorld, Vector3 bottomRightWorld ) {

			var tlLocal = _canvas.transform.InverseTransformPoint( topLeftWorld );
			var brLocal = _canvas.transform.InverseTransformPoint( bottomRightWorld );

			var size = new Vector2( brLocal.x - tlLocal.x, tlLocal.y - brLocal.y );
			return size;
		}
	}

}