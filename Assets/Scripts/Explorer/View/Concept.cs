using Framework;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Explorer.View {

	public class Concept : View {

		// ********** Public Interface **********

		public UnityEvent<string> OnClose = new UnityEvent<string>();

		public void SetModel ( Model.Concept model ) {

			_graphUID.Set( model.GraphUID );
			_nodeUID.Set( model.NodeUID );
			_size.Set( GetPhysicalSizeForPresence( model.Size ) );
			_position.Set( model.Position );
		}
		public Model.Concept GetModel () {

			return new Model.Concept(
				nodeUid: _nodeUID.Get(),
				graphUid: _graphUID.Get(),
				position: _position.Get(),
				size: _presenceControl.Size
			);
		}


		// ********** Private Interface **********

		[Header( "UI Controls" )]
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private DraggableControl _draggableControl;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleView;
		[SerializeField] private RectTransform _header;
		[SerializeField] private Transform _background;

		// static data
		private static Float3 COMPACT_SIZE = new Float3( 3f, 0.64f, 0.1f );
		private static Float3 EXPANDED_SIZE = new Float3( 4f, 6f, 0.1f );

		// model data
		private Observable<string> _graphUID;
		private Observable<string> _nodeUID;
		private ListObservable<Model.KnowledgeGraph.Link> _links; // TODO: HashSetObservable
		private Observable<Float3> _size;
		private Observable<Float3> _position;
		private Observable<string> _title;

		// internal data
		private Model.KnowledgeGraph _graph;

		protected override void Init () {

			// init subviews
			Init( _presenceControl );

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
					if ( _graph == null ) { return; }
					_title.Set( _graph.GetTitle( nodeUID ) );
					_links.Set( _graph.GetLinksFromConcept( nodeUID ) );
				}
			);
			_links = new ListObservable<Model.KnowledgeGraph.Link>(
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
						_background.transform.localPosition = ( size * new Float3( 0.5f, -0.5f, 0f ) ).ToVector3();
					}
					if ( _header != null ) {
						_header.sizeDelta = new Vector2( size.x * 100f, _header.sizeDelta.y );
					}
				}
			);
			_position = new Observable<Float3>(
				initialValue: Float3.Zero,
				onChange: position => {
					transform.position = position.ToVector3();
				}
			);
			_title = new Observable<string>(
				initialValue: "{Uninitialized}",
				onChange: title => {
					_titleView.text = title;
				}
			);

			// subscribe to controls
			_presenceControl.OnClosed.AddListener( () => {
				OnClose?.Invoke( _nodeUID.Get() );
			} );
			_presenceControl.OnSizeChanged.AddListener( size => {
				_size.Set( GetPhysicalSizeForPresence( size ) );
			} );
			_draggableControl.OnDragDelta.AddListener( delta => {
				_position.Set( _position.Get() + Float3.From( delta ) );
			} );
		}

		// helpers
		private Float3 GetPhysicalSizeForPresence ( Model.Presence.Sizes size ) =>
			size switch {
				Model.Presence.Sizes.Compact => COMPACT_SIZE,
				Model.Presence.Sizes.Expanded => EXPANDED_SIZE,
				_ => Float3.One
			};
	}

}