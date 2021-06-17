using System;
using UnityEngine;
using UnityEngine.EventSystems;

using Bounds = TextEdit.Text.Bounds;

namespace TextEdit {

	public enum ScrollAnchors {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	/*
		Tracks viewport and content bounds for a scroll view.

		All bounds and calculations are within a "container" coordinate space.
	*/
	[Serializable]
	public class ScrollState {

		// events
		public delegate void ContentBoundsChangedEvent ( Bounds contentBounds );
		public event ContentBoundsChangedEvent OnContentBoundsChanged;

		// properties
		public Vector2 Offset => _contentBounds.PositionFromPivot( _pivot ) - _viewportBounds.PositionFromPivot( _pivot );

		// methods
		public void Initialize ( Vector2 pivot, Bounds viewportBounds, Bounds contentBounds, Bounds focusInset ) {

			_pivot = pivot;
			SetViewportBounds( viewportBounds );
			_contentBounds = contentBounds;
			_focusInset = focusInset;

			// align pivots on start
			_contentBounds.MoveBy( -Offset );
		}
		public void SetViewportBounds ( Bounds bounds ) {

			_viewportBounds = bounds;
		}
		public void SetContentSize ( Vector2 size ) {

			// resize content bounds from top left
			_contentBounds.ResizeFromPivot( size, pivot: _pivot );
			ScrollBy( Vector2.zero ); // i dont like this
		}
		public void ScrollBy ( Vector2 delta ) {

			// we use bottom left because it is Pivot(0,0)
			var newBottomLeft = _contentBounds.BottomLeft + delta;

			// this is a clipping situation?
			newBottomLeft = newBottomLeft.Clamp( _minBottomLeft, _maxBottomLeft );
			var movement = newBottomLeft - _contentBounds.BottomLeft;

			if ( movement.magnitude > 0.0001f ) {
				_contentBounds.MoveBy( movement );
				OnContentBoundsChanged?.Invoke( _contentBounds.Duplicate() );
			}
		}
		public void ScrollToContentRect ( Bounds contentRect ) {

			var targetBounds = contentRect.Duplicate().MoveBy( Offset ); // content -> viewport coords
			var delta = FocusBounds.GetDeltaToContain( targetBounds );
			ScrollBy( -delta ); // inverted because we're moving content -> viewport, not viewport -> content
		}

		public void DrawGizmos ( Transform container ) {

			// viewport
			_viewportBounds.DrawGizmos( container: container, color: Color.black );

			// focus inset
			FocusBounds.DrawGizmos( container: container, color: Color.green );

			// content
			_contentBounds.DrawGizmos( container: container, color: Color.blue );

			// anchors
			var color = Gizmos.color;
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere( container.TransformPoint( _contentBounds.PositionFromPivot( _pivot ) ), 0.05f );
			Gizmos.color = Color.black;
			Gizmos.DrawSphere( container.TransformPoint( _viewportBounds.PositionFromPivot( _pivot ) ), 0.05f );
			Gizmos.color = color;
		}

		private Bounds FocusBounds => _viewportBounds.Duplicate().InsetBy( _focusInset );

		private Bounds _viewportBounds; // in container space
		private Bounds _contentBounds; // in container space
		private Bounds _focusInset; // relative to viewport bounds

		private Vector2 _pivot;

		private Vector2 _minBottomLeft => _viewportBounds.BottomLeft - ( _contentBounds.Size - _viewportBounds.Size );
		private Vector2 _maxBottomLeft => _viewportBounds.BottomLeft;
	}

	public class Scroll : MonoBehaviour,
		IScrollHandler {

		// *********** Public Interface ***********

		// events
		public delegate void ScrollOffsetChangedEvent ( Vector2 contentBounds );
		public event ScrollOffsetChangedEvent OnScrollOffsetChanged;

		public Vector2 Offset => _scrollInfo.Offset;
		public Bounds ViewportInset => _viewportInset;


		public void RefreshFrame () {

			_scrollInfo.SetViewportBounds( GetViewportBounds() );
		}
		public void SetContentSize ( Vector2 size ) {

			_scrollInfo.SetContentSize( size );
		}
		public void ScrollToContentRect ( Bounds contentRect ) {

			_scrollInfo.ScrollToContentRect( contentRect );
		}

		// *********** Private Interface ***********

		[Header( "UI Configuration" )]
		[SerializeField] private ScrollAnchors _scrollAnchor;
		[SerializeField] private float _scrollSpeed = 1f;
		[SerializeField] private Bounds _viewportInset;
		[SerializeField] private Bounds _focusInset;

		[Header( "UI Display" )]
		[SerializeField] private RectTransform _content;

		[Header( "UI Debug" )]
		[SerializeField] private bool _debugVisualizer;

		// private data
		private ScrollState _scrollInfo = new ScrollState();

		// mono lifecycle
		private void Awake () {

			// 
			InitializeScrollInfo();

			// event handler
			_scrollInfo.OnContentBoundsChanged += contentBounds => {
				_content.localPosition = contentBounds.PositionFromPivot( _content.pivot );
				OnScrollOffsetChanged?.Invoke( Offset );
			};
		}
		private void OnDrawGizmos () {

			if ( _debugVisualizer ) {
				if ( !Application.isPlaying ) {
					InitializeScrollInfo();
				}
				_scrollInfo.DrawGizmos( container: transform );
			}
		}


		private void InitializeScrollInfo () {

			_scrollInfo.Initialize(
				pivot: GetPivot( _scrollAnchor ),
				viewportBounds: GetViewportBounds(),
				contentBounds: GetContentBounds(),
				focusInset: _focusInset
			);
		}
		private Vector2 GetPivot ( ScrollAnchors scrollAnchor ) {

			return scrollAnchor switch {
				ScrollAnchors.TopLeft => new Vector2( 0.0f, 1.0f ),
				ScrollAnchors.TopRight => new Vector2( 1.0f, 1.0f ),
				ScrollAnchors.BottomLeft => new Vector2( 0.0f, 0.0f ),
				ScrollAnchors.BottomRight => new Vector2( 1.0f, 0.0f ),
				_ => Vector2.zero
			};
		}
		private Bounds GetBounds () {

			return Bounds.FromRectTransform( ( transform as RectTransform ) );
		}
		public Bounds GetViewportBounds () {

			return GetBounds().InsetBy( _viewportInset );
		}
		private Bounds GetContentBounds () {

			var worldCorners = new Vector3[4];
			_content.GetWorldCorners( worldCorners );
			for ( int i = 0; i < 4; i++ ) {
				worldCorners[i] = transform.InverseTransformPoint( worldCorners[i] );
			}

			return new Bounds(
				bottom: worldCorners[0].y,
				top: worldCorners[1].y,
				left: worldCorners[0].x,
				right: worldCorners[2].x
			);
		}

		// ********** IScrollHandler Implementation **********

		void IScrollHandler.OnScroll ( PointerEventData eventData ) {

			var scrollDelta = eventData.scrollDelta;
			scrollDelta.y *= -1; // invert y, so that positive is up and to the right
			_scrollInfo.ScrollBy( _scrollSpeed * scrollDelta );
		}
	}

}