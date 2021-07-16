using Jakintosh.Observable;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TextEdit {

	public enum ScrollAnchors {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public enum ContentFit {
		None,
		Width,
		Height
	}

	public class Scroll : MonoBehaviour,
		IScrollHandler {

		// *********** Public Interface ***********

		// events
		public UnityEvent<Vector2> OnOffsetChanged = new UnityEvent<Vector2>();
		public UnityEvent<Bounds> OnViewportBoundsChanged = new UnityEvent<Bounds>();
		public UnityEvent<Bounds> OnContentBoundsChanged = new UnityEvent<Bounds>();

		// properties
		public Vector2 Offset => _offset?.Get() ?? Vector2.zero;
		public Bounds ViewportInset => _viewportInset;

		// methods
		public void Init () {

			if ( _isInitialized ) { return; }
			_isInitialized = true;

			// init vars
			_pivot = GetPivot( _scrollAnchor );

			// init observables
			_offsetBounds = new Observable<Bounds>(
				initialValue: Bounds.Zero,
				onChange: bounds => {

					// clamp offset for new bounds
					_offset?.Set( bounds.Clamp( _offset.Get() ) );
				}
			);
			_containerBounds = new Observable<Bounds>(
				initialValue: GenerateContainerBounds(),
				onChange: bounds => {

					// update viewport bounds
					_viewportBounds?.Set( bounds.InsetBy( _viewportInset ) );
				}
			);
			_viewportBounds = new Observable<Bounds>(
				initialValue: GenerateContainerBounds().InsetBy( _viewportInset ),
				onChange: bounds => {

					if ( _contentSize != null ) {

						// update valid scroll range
						var newOffsetBounds = GenerateOffsetBounds(
							viewportSize: bounds.Size,
							contentSize: _contentSize.Get()
						);
						_offsetBounds.Set( newOffsetBounds );

						// refresh content size
						var newContentSize = GetValidContentSize(
							contentFit: _contentFitToViewport,
							viewportSize: bounds.Size,
							contentSize: _contentSize.Get()
						);
						_contentSize.Set( newContentSize );
					}

					// update content bounds
					_contentBounds?.Set( GenerateContentBounds( bounds, _contentSize.Get(), _offset.Get() ) );

					// fire event
					OnViewportBoundsChanged?.Invoke( bounds );
				}
			);
			_contentSize = new Observable<Vector2>(
				initialValue: _content.rect.size,
				onChange: size => {

					// update valid scroll range
					var newOffsetBounds = GenerateOffsetBounds(
						viewportSize: _viewportBounds.Get().Size,
						contentSize: size
					);
					_offsetBounds.Set( newOffsetBounds );

					// update content bounds
					_contentBounds?.Set( GenerateContentBounds( _viewportBounds.Get(), size, _offset.Get() ) );
				},
				onSet: size => {
					return GetValidContentSize(
						contentFit: _contentFitToViewport,
						viewportSize: _viewportBounds.Get().Size,
						contentSize: size
					);
				}
			);
			_offset = new Observable<Vector2>(
				initialValue: Vector2.zero,
				onChange: offset => {

					// update content bounds
					_contentBounds?.Set( GenerateContentBounds( _viewportBounds.Get(), _contentSize.Get(), offset ) );

					// fire event
					OnOffsetChanged?.Invoke( offset );
				},
				onSet: offset => {
					return _offsetBounds.Get().Clamp( offset );
				}
			);
			_contentBounds = new Observable<Bounds>(
				initialValue: GenerateContentBounds( _viewportBounds.Get(), _contentSize.Get(), _offset.Get() ),
				onChange: bounds => {

					// layout the content RectTransform
					LayoutContent( content: _content, bounds: bounds, fit: _contentFitToViewport );

					// fire event
					OnContentBoundsChanged?.Invoke( bounds );
				}
			);
		}
		public void RefreshFrame ()
			=> _containerBounds?.Set( GenerateContainerBounds() );
		public void RefreshContentSize ()
			=> _contentSize?.Set( _content.rect.size );
		public void ResetScrollOffset ()
			=> _offset?.Set( Vector2.zero );
		public void ScrollToContentRect ( Bounds contentRect ) {

			var viewport = _viewportBounds.Get();
			var target = contentRect.Duplicate().MoveBy( Offset ); // content -> viewport coords
			var delta = viewport.GetDeltaToContain( target );
			_offset.Set( _offset.Get() - delta );
		}
		public void SetPreferredContentSize ( Vector2 size )
			=> _contentSize?.Set( size );
		public void SetViewportInset ( float? top = null, float? bottom = null, float? left = null, float? right = null ) {

			var t = top.HasValue ? top.Value : _viewportInset.Top;
			var b = bottom.HasValue ? bottom.Value : _viewportInset.Bottom;
			var l = left.HasValue ? left.Value : _viewportInset.Left;
			var r = right.HasValue ? right.Value : _viewportInset.Right;
			_viewportInset = new Bounds( t, b, l, r );

			_viewportBounds.Set( _containerBounds.Get().InsetBy( _viewportInset ) );
		}

		// *********** Private Interface ***********

		[Header( "UI Configuration" )]
		[SerializeField] private ScrollAnchors _scrollAnchor;
		[SerializeField] private float _scrollSpeed = 1f;
		[SerializeField] private Bounds _viewportInset;
		[SerializeField] private ContentFit _contentFitToViewport;

		[Header( "UI Display" )]
		[SerializeField] private RectTransform _content;

		[Header( "UI Debug" )]
		[SerializeField] private bool _debugVisualizer;

		// view model
		private bool _isInitialized;
		private bool _containerNeedsLayout;
		private Vector2 _pivot;
		private Observable<Vector2> _offset;
		private Observable<Bounds> _offsetBounds;
		private Observable<Vector2> _contentSize;
		private Observable<Bounds> _containerBounds;
		private Observable<Bounds> _viewportBounds; // in container space
		private Observable<Bounds> _contentBounds; // in container space

		// mono lifecycle
		private void Awake () {

			if ( !_isInitialized ) {
				Init();
			}
		}
		private void OnEnable () {

			_containerNeedsLayout = true;
		}
		private void LateUpdate () {

			if ( _containerNeedsLayout || transform.hasChanged ) {
				_containerBounds.Set( GenerateContainerBounds() );
				_containerNeedsLayout = false;
				transform.hasChanged = false;
			}
		}
		private void OnDrawGizmos () {

			if ( _debugVisualizer ) {

				Bounds viewport, content, offset;

				if ( !Application.isPlaying ) {
					_pivot = GetPivot( _scrollAnchor );
					viewport = GenerateContainerBounds().InsetBy( _viewportInset );
					var contentSize = GetValidContentSize( _contentFitToViewport, viewport.Size, _content.rect.size );
					content = GenerateContentBounds( viewport, contentSize, Vector2.zero );
					offset = GenerateOffsetBounds( viewport.Size, content.Size );

					// position the content
					LayoutContent( content: _content, bounds: content, fit: _contentFitToViewport );

				} else {
					viewport = _viewportBounds.Get();
					content = _contentBounds.Get();
					offset = _offsetBounds.Get();
				}

				var color = Gizmos.color;

				viewport.DrawGizmos( container: transform, color: Color.black );
				Gizmos.color = Color.black;
				Gizmos.DrawSphere( transform.TransformPoint( viewport.PositionAtPivot( _pivot ) ), 1f * transform.lossyScale.x );

				content.DrawGizmos( container: transform, color: Color.blue );
				Gizmos.color = Color.blue;
				Gizmos.DrawSphere( transform.TransformPoint( content.PositionAtPivot( _pivot ) ), 1f * transform.lossyScale.x );

				offset.Duplicate().MoveBy( viewport.PositionAtPivot( _pivot ) ).DrawGizmos( container: transform, color: Color.red );

				Gizmos.color = color;
			}
		}

		// helpers
		private Vector2 GetPivot ( ScrollAnchors scrollAnchor )
			=> scrollAnchor switch {
				ScrollAnchors.TopLeft => new Vector2( 0.0f, 1.0f ),
				ScrollAnchors.TopRight => new Vector2( 1.0f, 1.0f ),
				ScrollAnchors.BottomLeft => new Vector2( 0.0f, 0.0f ),
				ScrollAnchors.BottomRight => new Vector2( 1.0f, 0.0f ),
				_ => Vector2.zero
			};
		private Vector2 GetScrollDirections ( ScrollAnchors scrollAnchor )
			=> scrollAnchor switch {
				ScrollAnchors.TopLeft => new Vector2( x: 1, y: -1 ),
				ScrollAnchors.TopRight => new Vector2( x: -1, y: -1 ),
				ScrollAnchors.BottomLeft => new Vector2( x: 1, y: 1 ),
				ScrollAnchors.BottomRight => new Vector2( x: -1, y: 1 ),
				_ => new Vector2( x: 0, y: 0 )
			};
		private Vector2 GetValidContentSize ( ContentFit contentFit, Vector2 viewportSize, Vector2 contentSize ) {

			var newContentSize = new Vector2(
				x: contentFit switch {
					ContentFit.Width => viewportSize.x,
					_ => contentSize.x
				},
				y: contentFit switch {
					ContentFit.Height => viewportSize.y,
					_ => contentSize.y
				}
			);
			return newContentSize;
		}
		private Bounds GenerateContainerBounds ()
			=> Bounds.FromRectTransform( ( (RectTransform)transform ) );
		private Bounds GenerateContentBounds ( Bounds viewport, Vector2 size, Vector2 offset ) {

			// create bounds
			var contentBounds = new Bounds(
				position: Vector2.zero,
				size: size
			);

			// align pivot to viewport
			var pivotOffset = contentBounds.PositionAtPivot( _pivot ) - viewport.PositionAtPivot( _pivot );
			contentBounds.MoveBy( -pivotOffset );

			// move into actual offset
			contentBounds.MoveBy( offset );

			return contentBounds;
		}
		private Bounds GenerateOffsetBounds ( Vector2 viewportSize, Vector2 contentSize ) {

			// get size delta, but negative means no scroll room, so clamp those
			var sizeDelta = new Vector2(
				x: ( contentSize.x - viewportSize.x ).WithFloor( 0f ),
				y: ( contentSize.y - viewportSize.y ).WithFloor( 0f )
			);

			// get valid scrolling directions
			var scrollDirection = GetScrollDirections( _scrollAnchor );

			// create arrays of corner values
			var xVals = new[] { 0f, sizeDelta.x * -scrollDirection.x }; // direction is neg because its the inverse
			var yVals = new[] { 0f, sizeDelta.y * -scrollDirection.y }; // space for the scrolling bounds

			return new Bounds(
				top: Mathf.Max( yVals ),
				bottom: Mathf.Min( yVals ),
				left: Mathf.Min( xVals ),
				right: Mathf.Max( xVals )
			);
		}

		private void LayoutContent ( RectTransform content, Bounds bounds, ContentFit fit ) {

			// set anchors
			switch ( fit ) {
				case ContentFit.Width:
					content.anchorMin = new Vector2( x: 0f, y: content.anchorMin.y );
					content.anchorMax = new Vector2( x: 1f, y: content.anchorMax.y );
					break;
				case ContentFit.Height:
					content.anchorMin = new Vector2( x: content.anchorMin.x, y: 0f );
					content.anchorMax = new Vector2( x: content.anchorMax.x, y: 1f );
					break;
			}

			// update size
			content.SetSizeWithCurrentAnchors( RectTransform.Axis.Horizontal, bounds.Width );
			content.SetSizeWithCurrentAnchors( RectTransform.Axis.Vertical, bounds.Height );

			// update position
			content.localPosition = bounds.PositionAtPivot( content.pivot );
		}

		// ********** IScrollHandler Implementation **********

		void IScrollHandler.OnScroll ( PointerEventData eventData ) {

			var offsetDelta = _scrollSpeed * -eventData.scrollDelta; // scroll delta goes the opposite way i need
			_offset.Set( _offset.Get() + offsetDelta );
		}
	}

}