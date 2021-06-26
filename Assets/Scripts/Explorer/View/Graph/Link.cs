using Jakintosh.Observable;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	/*
		Relationship is a view that connects two nodes and has a label.
		Relationship can also be actively dragged.

		Components:
		- Start Point
		- End Point
		- Type
		- Label Visibility
		- End caps

		States:
		- Free / Docked / Anchored
		- Show Label (bool)
	*/
	public class Link : ReuseableView<Model.Link> {

		/*
			really these concept views and points just want a scene anchor,
			not necessarily a concept view. but the anchor ids need to persist
			across sessions. 
		*/

		// *********** Public Interface ***********

		public enum State {
			Free,
			Docked,
			Anchored
		}

		public void SetSource ( Concept source ) {

			_sourceConceptView.Set( source );
		}
		public void SetFree () {

			_state.Set( State.Free );
			_destinationConceptView.Set( null );
		}
		public void SetDocked ( Concept destination ) {

			_state.Set( State.Docked );
			_destinationConceptView.Set( destination );
		}
		public void SetAnchored ( Concept destination ) {

			_state.Set( State.Anchored );
			_destinationConceptView.Set( destination );
		}


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private Button _typeLabelButton;

		[Header( "UI Display" )]
		[SerializeField] private Transform _buttonCanvas;
		[SerializeField] private Image _backgroundImage;
		[SerializeField] private TextMeshProUGUI _titleLabel;
		[SerializeField] private LineRenderer _lineRenderer;

		[Header( "UI Assets" )]
		[SerializeField] private Color _sourceColor;
		[SerializeField] private Color _destColor;
		[SerializeField] private ContextMenu _contextMenuPrefab;

		private Model.Link _linkModel;

		private Observable<State> _state;
		private Observable<Concept> _sourceConceptView;
		private Observable<Concept> _destinationConceptView;
		private Observable<Float3> _startPoint;
		private Observable<Float3> _endPoint;
		private Observable<bool> _selected;

		// view lifecycle
		public override Model.Link GetState () {

			return new Model.Link(
				graphUid: _linkModel.GraphUID,
				linkUid: _linkModel.LinkUID
			);
		}
		protected override void OnInitialize () {

			_lineRenderer.textureMode = LineTextureMode.Tile;

			_state = new Observable<State>(
				initialValue: State.Free,
				onChange: state => {
					_lineRenderer.colorGradient = GetGradientForState( state );
					_typeLabelButton.gameObject.SetActive( state == State.Anchored );
				}
			);

			_sourceConceptView = new Observable<Concept>(
				initialValue: null,
				onChange: sourceConceptView => {
					_sourceConceptView?.Previous()?.OnPositionChange.RemoveListener( HandleSourceViewPositionChange );
					_startPoint?.Set( sourceConceptView?.GetPosition() ?? Float3.Zero );
					sourceConceptView?.OnPositionChange.AddListener( HandleSourceViewPositionChange );
				}
			);
			_destinationConceptView = new Observable<Concept>(
				initialValue: null,
				onChange: destinationConceptView => {
					_sourceConceptView?.Previous()?.OnPositionChange.RemoveListener( HandleDestinationViewPositionChange );
					_endPoint?.Set( destinationConceptView?.GetPosition() ?? Float3.Zero );
					destinationConceptView?.OnPositionChange.AddListener( HandleDestinationViewPositionChange );
				}
			);

			_startPoint = new Observable<Float3>(
				initialValue: Float3.Zero,
				onChange: startPoint => {
					var positions = GetPositions( startPoint.ToVector3(), _endPoint?.Get().ToVector3() ?? Vector3.zero );
					_lineRenderer.SetPositions( positions );
					_lineRenderer.positionCount = positions.Length;
					_buttonCanvas.position = positions[0] + ( ( positions[positions.Length - 1] - positions[0] ) / 2f );
				}
			);
			_endPoint = new Observable<Float3>(
				initialValue: Float3.Zero,
				onChange: endPoint => {
					var positions = GetPositions( _startPoint?.Get().ToVector3() ?? Vector3.zero, endPoint.ToVector3() );
					_lineRenderer.SetPositions( positions );
					_lineRenderer.positionCount = positions.Length;
					_buttonCanvas.position = positions[0] + ( ( positions[positions.Length - 1] - positions[0] ) / 2f );
				}
			);
			_selected = new Observable<bool>(
				initialValue: false,
				onChange: selected => {

				}
			);

			_typeLabelButton.onClick.AddListener( () => {

				var contextMenu = Instantiate<ContextMenu>(
						original: _contextMenuPrefab,
						parent: transform,
						worldPositionStays: false );

				contextMenu.transform.position = _typeLabelButton.transform.position + _typeLabelButton.transform.forward * -0.1f;

				var graph = Client.Application.Resources.Graphs.Get( _linkModel.GraphUID );

				contextMenu.InitWith(
					new Model.ContextMenu(
						graph
							.AllRelationTypes
							.ConvertToList( ( _, relType ) =>
								new Model.ContextAction(
									name: relType.Name,
									action: () => {
										graph.UpdateLinkType( _linkModel.LinkUID, relType.UID );
										Destroy( contextMenu.gameObject );
										this.InitWith( _linkModel );
									}
								)
							)
							.ToArray()
				) );
			} );
		}
		protected override void OnPopulate ( Model.Link linkModel ) {

			_linkModel = linkModel;
			// what state am i in
			// how do i resolve connected views from last session
			var graph = Client.Application.Resources.Graphs.Get( linkModel.GraphUID );
			if ( graph == null ) { return; }
			var link = graph.GetLink( linkModel.LinkUID );
			if ( link == null ) { return; }

			Debug.Log( $"View.Link.OnPopulate: link = {link.ToString()}" );

			var relType = graph.GetRelationType( link.TypeUID );
			var metadata = graph.GetMetadataForRelationType( link.TypeUID );

			if ( relType == null ) {

				_titleLabel.text = "Undefined";
				_backgroundImage.color = Color.magenta;

			} else {

				Debug.Log( $"View.Link.OnPopulate: relType = {relType.ToString()}" );
				Debug.Log( $"View.Link.OnPopulate: relTypeMetadata = {metadata.ToString()}" );

				// set name
				_titleLabel.text = graph.GetRelationType( link.TypeUID ).Name;

				// set color
				var colorString = graph.GetMetadataForRelationType( link.TypeUID ).Display.HexColor;
				if ( ColorUtility.TryParseHtmlString( colorString, out var color ) ) {
					_backgroundImage.color = color;
				}
			}


		}
		protected override void OnRecycle () { }

		private void Update () {

			if ( _destinationConceptView.Get() == null ) {
				var t = _sourceConceptView.Get().transform;
				var ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				var plane = new Plane( inNormal: -t.forward, inPoint: t.position );
				plane.Raycast( ray, out float distance );
				_endPoint.Set( Float3.From( ray.origin + ( ray.direction * distance ) ) );
			}
		}

		// event handlers
		private void HandleSourceViewPositionChange ( Float3 position ) => _startPoint.Set( position );
		private void HandleDestinationViewPositionChange ( Float3 position ) => _endPoint.Set( position );

		// helpers
		private Gradient GetGradientForState ( State state ) {

			var alphaKeys = state switch {
				_ => new GradientAlphaKey[] {
					new GradientAlphaKey( 1.0f, 0.0f )
				}
			};
			var colorKeys = state switch {
				State.Free => new GradientColorKey[] {
						new GradientColorKey( _sourceColor, 0.0f )
				},
				State.Docked => new GradientColorKey[] {
						new GradientColorKey( _sourceColor, 0.0f ),
						new GradientColorKey( _sourceColor, 0.4f ),
						new GradientColorKey( _destColor, 0.6f ),
						new GradientColorKey( _destColor, 1.0f )
				},
				State.Anchored => new GradientColorKey[] {
						new GradientColorKey( new Color(0.2f,0.2f,0.2f), 0.0f ),
						new GradientColorKey( new Color(0.2f,0.2f,0.2f), 1.0f )
				},
				_ => new GradientColorKey[] {
						new GradientColorKey( Color.magenta, 0.0f )
				}
			};

			var gradient = new Gradient();
			gradient.SetKeys( colorKeys, alphaKeys );
			return gradient;
		}
		private Vector3[] GetPositions ( Vector3 start, Vector3 end ) {

			Vector3[] positions = new Vector3[50];
			for ( int i = 0; i < 50; i++ ) {
				positions[i] = ( ( ( ( end - start ) / 49 ) * i ) + start );
			}
			return positions;
		}

	}

}