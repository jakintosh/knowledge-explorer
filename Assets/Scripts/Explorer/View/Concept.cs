using Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using Link = Explorer.Model.KnowledgeGraph.Link;
using ConceptModel = Explorer.Model.View.Concept;

namespace Explorer.View {

	public class Concept : View<ConceptModel> {

		// ********** Public Interface **********

		// data types
		public enum Mode {
			View,
			Edit
		}

		public struct DragLinkEventData {
			public string NodeUID;
			public bool IsReceiving;
			public DragLinkEventData ( string uid, bool receiving ) {
				NodeUID = uid;
				IsReceiving = receiving;
			}
		}

		// events
		public UnityEvent<string> OnClose = new UnityEvent<string>();
		public UnityEvent<string> OnDragLinkBegin = new UnityEvent<string>();
		public UnityEvent<string> OnDragLinkEnd = new UnityEvent<string>();
		public UnityEvent<DragLinkEventData> OnDragLinkReceiving = new UnityEvent<DragLinkEventData>();
		public UnityEvent<string> OnOpenRelationship = new UnityEvent<string>();

		public UnityEvent<Float3> OnPositionChange = new UnityEvent<Float3>();

		// functions
		public void SetModel ( ConceptModel model ) {

			// set relevant view model data
			_graphUID.Set( model.GraphUID );
			_nodeUID.Set( model.NodeUID );
			_presenceControl.Force( size: model.Size );
			_position.Set( model.Position );
		}
		public override ConceptModel GetInitData () {

			return new ConceptModel(
				nodeUid: _nodeUID.Get(),
				graphUid: _graphUID.Get(),
				position: _position.Get(),
				size: _presenceControl.Size
			);
		}

		public Float3 GetPosition () => _position.Get();

		// ********** Private Interface **********

		[Header( "UI Controls" )]
		[SerializeField] private Draggable3DControl _draggableControl;
		[SerializeField] private DraggableReceiver _draggableReceiver;
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private ToggleControl _editToggle;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleView;
		[SerializeField] private RectTransform _header;
		[SerializeField] private Image _headerEditOverlay;
		[SerializeField] private RectTransform _outlines;
		[SerializeField] private Image _selectedOutline;
		[SerializeField] private Image _dropZoneOutline;
		[SerializeField] private Transform _background;

		// model data
		private Observable<string> _graphUID;
		private Observable<string> _nodeUID;
		private HashSetObservable<Link> _links;
		private Observable<Float3> _size;
		private Observable<Float3> _position;
		private Observable<Mode> _mode;
		private Observable<bool> _selected;
		private Observable<bool> _receiving;
		private Observable<bool> _hovering;
		private Observable<string> _title;

		// internal data
		private Model.KnowledgeGraph _graph;

		protected override void InitFrom ( ConceptModel data ) {

			Init();
			SetModel( data );
		}
		protected override void Init () {

			// init subviews
			InitView( _presenceControl );
			InitView( _editToggle );

			// configure controls
			_draggableReceiver.SetReceivableTypes( typeof( string ) );
			_draggableControl.IgnoreReceivers( _draggableReceiver );

			// init observables
			_graphUID = new Observable<string>(
				initialValue: null,
				onChange: graphUID => {
					_graph = Application.Resources.Graphs.Get( graphUID );
				}
			);
			_nodeUID = new Observable<string>(
				initialValue: null,
				onChange: nodeUID => {
					_draggableControl?.ClearPayloads();
					_draggableControl?.AddPayload( mode: Drag.Mode.Secondary, payload: nodeUID );
					if ( _graph != null ) {
						_title.Set( _graph.GetTitle( nodeUID ) );
						_links.Set( new HashSet<Link>( _graph.GetLinksFromConcept( nodeUID ) ) );
					}
				}
			);
			_links = new HashSetObservable<Link>(
				initialValue: null,
				onChange: links => {
					if ( links == null ) { return; }
				}
			);
			_size = new Observable<Float3>(
				initialValue: GetPhysicalSizeForPresence( _presenceControl.Size ),
				onChange: size => {
					if ( _background != null ) {
						_background.transform.localScale = size.ToVector3();
					}
					if ( _outlines != null ) {
						_outlines.sizeDelta = new Vector2( size.x * 100f, size.y * 100f );
					}
					if ( _header != null ) {
						_header.sizeDelta = new Vector2( size.x * 100f, _header.sizeDelta.y );
						_header.localPosition = new Vector3( x: _header.localPosition.x, y: size.y / 2, z: _header.localPosition.z );
					}
				}
			);
			_position = new Observable<Float3>(
				initialValue: Float3.Zero,
				onChange: position => {
					transform.position = position.ToVector3();
					OnPositionChange?.Invoke( position );
				}
			);
			_title = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: title => {
					_titleView.text = title;
				}
			);
			_mode = new Observable<Mode>(
				initialValue: Mode.View,
				onChange: mode => {
					_headerEditOverlay.gameObject.SetActive( mode == Mode.Edit );
					_presenceControl.SetEnabled(
						close: mode == Mode.View,
						size: mode == Mode.View,
						context: mode == Mode.View
					);
				}
			);
			_selected = new Observable<bool>(
				initialValue: false,
				onChange: selected => {
					_selectedOutline.gameObject.SetActive( selected );
				}
			);
			_receiving = new Observable<bool>(
				initialValue: false,
				onChange: receiving => {
					_dropZoneOutline.gameObject.SetActive( receiving );
				}
			);
			_hovering = new Observable<bool>(
				initialValue: false,
				onChange: hovering => {
					var c = _dropZoneOutline.color;
					c.a = hovering ? 1f : 0.2f;
					_dropZoneOutline.color = c;
				}
			);

			// subscribe to controls
			_presenceControl.OnClosed.AddListener( () => {
				OnClose?.Invoke( _nodeUID.Get() );
			} );
			_presenceControl.OnSizeChanged.AddListener( size => {
				_size.Set( GetPhysicalSizeForPresence( size ) );
				_editToggle.gameObject.SetActive( size == Model.Presence.Sizes.Expanded );
			} );
			_editToggle.OnToggled.AddListener( isEditing => {
				_mode.Set( isEditing ? Mode.Edit : Mode.View );
			} );

			_draggableReceiver.OnHover.AddListener( isHovering => {
				_hovering.Set( isHovering );
				OnDragLinkReceiving?.Invoke( new DragLinkEventData( _nodeUID.Get(), isHovering ) );
			} );
			_draggableReceiver.OnReceiving.AddListener( isReceiving => {
				_receiving.Set( isReceiving );
			} );
			_draggableReceiver.OnReceivedPayload.AddListener( payload => {
				if ( payload is string ) {
					var sourceUid = payload as string;
					Debug.Log( $"{_title.Get()}[{_nodeUID.Get()}] recieved uid [{sourceUid}]" );
					var relationshipUID = _graph.LinkConcepts( from: sourceUid, to: _nodeUID.Get() );
					OnOpenRelationship?.Invoke( relationshipUID );
				}
			} );

			_draggableControl.OnDragBegin.AddListener( eventData => {
				switch ( eventData.Mode ) {

					case Drag.Mode.Primary:
						break;

					case Drag.Mode.Secondary:
						_selected.Set( true );
						OnDragLinkBegin?.Invoke( _nodeUID.Get() );
						break;
				}
			} );
			_draggableControl.OnDragDelta.AddListener( eventData => {
				switch ( eventData.Mode ) {

					case Drag.Mode.Primary:
						_position.Set( _position.Get() + Float3.From( eventData.Delta ) );
						break;

					case Drag.Mode.Secondary:
						break;
				}
			} );
			_draggableControl.OnDragEnd.AddListener( eventData => {
				switch ( eventData.Mode ) {

					case Drag.Mode.Primary:
						break;

					case Drag.Mode.Secondary:
						_selected.Set( false );
						OnDragLinkEnd?.Invoke( _nodeUID.Get() );
						break;
				}
			} );
		}

		// helpers
		private static Float3 COMPACT_SIZE = new Float3( 3f, 0.64f, 0.1f );
		private static Float3 EXPANDED_SIZE = new Float3( 4f, 4.64f, 0.1f );
		private Float3 GetPhysicalSizeForPresence ( Model.Presence.Sizes size ) =>
			size switch {
				Model.Presence.Sizes.Compact => COMPACT_SIZE,
				Model.Presence.Sizes.Expanded => EXPANDED_SIZE,
				_ => Float3.One
			};
	}

}