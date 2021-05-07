using System.Collections.Generic;
using UnityEngine;

using WorkspaceModel = Explorer.Model.View.Workspace;
using ConceptModel = Explorer.Model.View.Concept;

namespace Explorer.View {

	public class GraphViewport : View {

		// *********** Public Interface ***********

		public void SetContext ( WorkspaceModel workspace, Knowledge.Graph graph ) {

			_graph = graph;
			_workspace = workspace;


			_uiContainer.SetActive( _workspace != null );

			ClearViewport();

			if ( _workspace == null ) { return; }

			_workspace.Concepts?.ForEach( concept => OpenConcept( concept ) );
			_workspace.Links?.ForEach( linkUid => OpenLink( linkUid ) );
		}

		// *********** Private Interface ***********

		[Header( "UI Controls" )]
		[SerializeField] private GraphToolbar _graphToolbar = null;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _uiContainer = null;

		[Header( "Prefabs" )]
		[SerializeField] private Concept _conceptViewPrefab = null;
		[SerializeField] private Link _linkViewPrefab = null;


		// private data
		private WorkspaceModel _workspace;
		private Knowledge.Graph _graph;
		private Dictionary<string, Concept> _conceptViewsByNodeId = new Dictionary<string, Concept>();
		private Dictionary<string, Link> _linksById = new Dictionary<string, Link>();

		protected override void Init () {

			// init subviews
			InitView( _graphToolbar );

			// subscribe to controls
			_graphToolbar.OnNewItem.AddListener( () => {
				var model = ConceptModel.Default(
					graphUid: _graph?.UID,
					nodeUid: _graph?.NewConcept()
				);
				OpenConcept( model );
			} );
			_graphToolbar.OnSave.AddListener( () => {
				_workspace?.SetConcepts( _conceptViewsByNodeId.Values.Convert( view => view.GetInitData() ) );
				_workspace?.SetOpenLinks( _linksById.Keys );
			} );
		}

		private void OpenLink ( string linkUid ) {

			// create and instantiate link
			var link = _graph?.GetLink( linkUid );
			var linkView = Instantiate<Link>( _linkViewPrefab );
			InitView( linkView );

			// view setup
			linkView.SetSource( _conceptViewsByNodeId[link.FromUID] );
			linkView.SetAnchored( _conceptViewsByNodeId[link.ToUID] );

			// track
			_linksById.Add( linkUid, linkView );
		}

		private void OpenConcept ( ConceptModel model ) {

			// validate model
			if ( model == null || model.GraphUID.IsNullOrEmpty() || model.NodeUID.IsNullOrEmpty() ) {
				Debug.Log( "Cannot open concept: model invalid" );
				return;
			}

			// create and init view
			var view = Instantiate<Concept>(
				original: _conceptViewPrefab,
				parent: this.transform,
				worldPositionStays: false
			);
			InitView( view, model );

			// view setup
			view.OnClose.AddListener( CloseConcept );
			view.OnDragLinkBegin.AddListener( NewDragLink );
			view.OnDragLinkEnd.AddListener( DestroyDragLink );
			view.OnDragLinkReceiving.AddListener( DragLinkReceiving );
			view.OnOpenLink.AddListener( OpenLink );

			// track
			_conceptViewsByNodeId.Add( model.NodeUID, view );
		}
		private void CloseConcept ( string nodeUid ) {

			var nodeView = _conceptViewsByNodeId[nodeUid];
			nodeView.OnClose.RemoveListener( CloseConcept );
			nodeView.OnDragLinkBegin.RemoveListener( NewDragLink );
			nodeView.OnDragLinkEnd.RemoveListener( DestroyDragLink );
			nodeView.OnDragLinkReceiving.RemoveListener( DragLinkReceiving );
			nodeView.OnOpenLink.RemoveListener( OpenLink );
			_conceptViewsByNodeId.Remove( nodeUid );
			Destroy( nodeView.gameObject );
		}
		private void ClearViewport () {

			// clear concepts
			_conceptViewsByNodeId.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_conceptViewsByNodeId.Clear();

			// clear links
			_linksById.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_linksById.Clear();
		}

		private Link _tempLink;
		private void NewDragLink ( string nodeUid ) {

			_tempLink = Instantiate<Link>( _linkViewPrefab );
			InitView( _tempLink );
			_tempLink.SetSource( _conceptViewsByNodeId[nodeUid] );
			_tempLink.SetFree();
		}
		private void DragLinkReceiving ( Concept.DragLinkEventData eventData ) {

			if ( eventData.IsReceiving ) {
				_tempLink.SetDocked( _conceptViewsByNodeId[eventData.NodeUID] );
			} else {
				_tempLink.SetFree();
			}
		}
		private void DestroyDragLink ( string nodeUid ) {

			Destroy( _tempLink.gameObject );
			_tempLink = null;
		}
	}

}