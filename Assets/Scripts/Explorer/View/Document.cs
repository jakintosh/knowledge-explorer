using Framework;
using UnityEngine;

namespace Explorer.View {

	public enum DocumentModes {
		Edit,
		Reorder,
		Read
	}

	public class Document : View {

		[Header( "UI Control" )]
		[SerializeField] private DraggableUIControl _corner;
		[SerializeField] private DocumentModeToolbar _documentToolbar;

		[Header( "UI Configuration" )]
		[SerializeField] private Vector2 _minDocumentSize;
		[SerializeField] private Vector2 _maxDocumentSize;

		[Header( "UI Display" )]
		[SerializeField] private Canvas _canvas;
		[SerializeField] private TextEdit.Text _textEdit;

		// observables
		private Observable<Vector2> _documentSize;

		private Vector3 _dragPosition;

		protected override void OnInitialize () {

			// init subviews
			_documentToolbar.Init();

			// init observables
			_documentSize = new Observable<Vector2>(
				initialValue: ( _canvas.transform as RectTransform ).rect.size,
				onChange: size => {
					( _canvas.transform as RectTransform ).sizeDelta = size;
					_textEdit.RefreshSize();
				}
			);

			// subscribe to controls
			_corner.OnDragBegin.AddListener( () => {
				_dragPosition = GetBottomRightWorldPosition();
			} );
			_corner.OnDragDelta.AddListener( delta => {
				_dragPosition += delta;
				var size = GetSize( GetTopLeftWorldPosition(), _dragPosition ).Clamp( _minDocumentSize, _maxDocumentSize );
				Debug.Log( $"Size:{size}" );
				_documentSize.Set( size );
			} );
		}
		protected override void OnCleanup () {

		}


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

			Debug.Log( $"TL local: {tlLocal}" );
			Debug.Log( $"BR local: {brLocal}" );

			var size = new Vector2( brLocal.x - tlLocal.x, tlLocal.y - brLocal.y );
			return size;
		}
	}

}