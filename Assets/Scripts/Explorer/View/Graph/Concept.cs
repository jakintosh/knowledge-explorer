using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

using ConceptModel = Explorer.View.Model.Concept;

namespace Explorer.View {

	public class Concept : ReuseableView<ConceptModel> {

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
		public UnityEvent<Model.Link> OnOpenLink = new UnityEvent<Model.Link>();

		public UnityEvent<Float3> OnPositionChange = new UnityEvent<Float3>();

		// functions
		public override ConceptModel GetState () {

			return new ConceptModel(
				graphUid: _graphUID.Get(),
				nodeUid: _nodeUID.Get(),
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
		[SerializeField] private TMP_InputField _titleInputField;

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
		private HashSetObservable<Jakintosh.Graph.Link> _links;
		private Observable<Float3> _size;
		private Observable<Float3> _position;
		private Observable<Mode> _mode;
		private Observable<bool> _selected;
		private Observable<bool> _receiving;
		private Observable<bool> _hovering;
		private Observable<string> _title;

		// internal data
		private Jakintosh.Knowledge.Graph _graph;

		protected override void OnInitialize () {

			// init subviews
			_presenceControl.Init();
			_editToggle.Init();

			// configure controls
			_draggableReceiver.SetReceivableTypes( typeof( string ) );
			_draggableControl.IgnoreReceivers( _draggableReceiver );

			// init observables
			_graphUID = new Observable<string>(
				initialValue: null,
				onChange: graphUID => {
					_graph = Client.Application.Resources.Graphs.Get( graphUID );
				}
			);
			_nodeUID = new Observable<string>(
				initialValue: null,
				onChange: nodeUID => {
					_draggableControl?.ClearPayloads();
					_draggableControl?.AddPayload( mode: Drag.Mode.Secondary, payload: nodeUID );
					if ( _graph != null ) {
						_title.Set( _graph.GetConceptTitle( nodeUID ) );
						_links.Set( new HashSet<Jakintosh.Graph.Link>( _graph.GetConceptLinks( nodeUID ) ) );
					}
				}
			);
			_links = new HashSetObservable<Jakintosh.Graph.Link>(
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
					_titleInputField.SetTextWithoutNotify( title );
				}
			);
			_mode = new Observable<Mode>(
				initialValue: Mode.View,
				onChange: mode => {
					_titleInputField.interactable = mode == Mode.Edit;
					_headerEditOverlay.gameObject.SetActive( mode == Mode.Edit );
					_presenceControl.gameObject.SetActive( mode == Mode.View );
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
				_editToggle.gameObject.SetActive( size == PresenceControl.Sizes.Expanded );
			} );
			_editToggle.OnToggled.AddListener( isEditing => {
				_mode.Set( isEditing ? Mode.Edit : Mode.View );
			} );

			_titleInputField.onEndEdit.AddListener( newTitle => {
				var conceptUID = _nodeUID.Get();
				if ( _graph != null && conceptUID != null ) {
					_graph.UpdateConceptTitle( conceptUID, newTitle );
				}
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
					var linkUid = _graph.CreateLink( fromConceptUID: sourceUid, toConceptUID: _nodeUID.Get() );
					OnOpenLink?.Invoke( new Model.Link( _graphUID.Get(), linkUid ) );
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
		protected override void OnPopulate ( ConceptModel model ) {

			_graphUID.Set( model.GraphUID );
			_nodeUID.Set( model.NodeUID );
			_presenceControl.Force( size: model.Size );
			_position.Set( model.Position );
		}
		protected override void OnRecycle () { }
		protected override void OnCleanup () { }



		// helpers
		private static Float3 COMPACT_SIZE = new Float3( 3f, 0.64f, 0.1f );
		private static Float3 EXPANDED_SIZE = new Float3( 4f, 4.64f, 0.1f );
		private Float3 GetPhysicalSizeForPresence ( PresenceControl.Sizes size ) =>
			size switch {
				PresenceControl.Sizes.Compact => COMPACT_SIZE,
				PresenceControl.Sizes.Expanded => EXPANDED_SIZE,
				_ => Float3.One
			};
	}

}