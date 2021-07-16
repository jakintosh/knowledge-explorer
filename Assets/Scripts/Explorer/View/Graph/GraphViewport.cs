using System.Collections.Generic;
using UnityEngine;

using ConceptModel = Explorer.View.Model.Concept;
using LinkModel = Explorer.View.Model.Link;

namespace Explorer.View {

	/*
		Graph Viewport

		This is a view where the user can create, link, and drag the
		concept views. Its main function is to render those two view
		types (concepts and links), and to communicate those visual
		changes back to the underlying source graph.
	*/
	public class GraphViewport : ReuseableView<Model.GraphViewport> {

		// *********** Public Interface **********

		// concepts
		public void NewConcept ( string graphUid ) {

			var graph = Client.Application.Resources.Graphs.Get( graphUid );
			if ( graph == null ) { return; }

			var newConceptUid = graph.CreateConcept();
			Debug.Log( $"Created new concept with uid-{newConceptUid} in graph uid-{graphUid}" );
			var model = ConceptModel.Default(
				graphUid: graphUid,
				nodeUid: newConceptUid
			);
			OpenConcept( model );
		}
		public void OpenConcept ( ConceptModel model ) {

			// validate model
			if ( model == null || model.GraphUID.IsNullOrEmpty() || model.NodeUID.IsNullOrEmpty() ) {
				Debug.Log( "Cannot open concept: model invalid" );
				return;
			}

			// create and init view
			var conceptView = Instantiate<Concept>(
				original: _conceptViewPrefab,
				parent: this.transform,
				worldPositionStays: false
			);
			conceptView.InitWith( model );

			// view setup
			conceptView.OnClose.AddListener( CloseConcept );
			conceptView.OnOpenLink.AddListener( OpenLink );
			conceptView.OnDragLinkBegin.AddListener( NewDragLink );
			conceptView.OnDragLinkEnd.AddListener( DestroyDragLink );
			conceptView.OnDragLinkReceiving.AddListener( DragLinkReceiving );

			// track
			_conceptViewsByNodeUid.Add( model.NodeUID, conceptView );
		}
		public void CloseConcept ( string conceptUid ) {

			var conceptView = _conceptViewsByNodeUid[conceptUid];
			conceptView.OnClose.RemoveListener( CloseConcept );
			conceptView.OnOpenLink.RemoveListener( OpenLink );
			conceptView.OnDragLinkBegin.RemoveListener( NewDragLink );
			conceptView.OnDragLinkEnd.RemoveListener( DestroyDragLink );
			conceptView.OnDragLinkReceiving.RemoveListener( DragLinkReceiving );
			_conceptViewsByNodeUid.Remove( conceptUid );
			Destroy( conceptView.gameObject );
		}

		// links
		public void OpenLink ( LinkModel linkModel ) {

			// create and instantiate link
			var uid = linkModel.LinkUID;
			var graph = Client.Application.Resources.Graphs.Get( linkModel.GraphUID );
			var link = graph?.GetLink( uid );
			var linkView = Instantiate<Link>( _linkViewPrefab );
			linkView.InitWith( linkModel );

			// view setup
			linkView.SetSource( _conceptViewsByNodeUid[link.FromUID] );
			linkView.SetAnchored( _conceptViewsByNodeUid[link.ToUID] );

			// track
			_linksByUid.Add( uid, linkView );
		}

		// viewport
		public void Clear () {

			// clear concepts
			_conceptViewsByNodeUid.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_conceptViewsByNodeUid.Clear();

			// clear links
			_linksByUid.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_linksByUid.Clear();
		}


		// *********** Private Interface ***********

		[Header( "Prefabs" )]
		[SerializeField] private Concept _conceptViewPrefab = null;
		[SerializeField] private Link _linkViewPrefab = null;

		// private data
		private Dictionary<string, Concept> _conceptViewsByNodeUid = new Dictionary<string, Concept>();
		private Dictionary<string, Link> _linksByUid = new Dictionary<string, Link>();

		// view lifecycle
		public override Model.GraphViewport GetState () {

			return new Model.GraphViewport(
				concepts: _conceptViewsByNodeUid.ConvertToList( ( _, conceptView ) => conceptView.GetState() ),
				links: _linksByUid.ConvertToList( ( _, linkView ) => linkView.GetState() )
			);
		}
		protected override void OnInitialize () { }
		protected override void OnPopulate ( Model.GraphViewport graphViewport ) {

			graphViewport?.Concepts.ForEach( concept => OpenConcept( concept ) );
			graphViewport?.Links.ForEach( link => OpenLink( link ) );
		}
		protected override void OnRecycle () {

			Clear();
		}
		protected override void OnCleanup () { }




		// bunch of temp link stuff this is trash
		private Link _tempLink;
		private void NewDragLink ( string nodeUid ) {

			_tempLink = Instantiate<Link>( _linkViewPrefab );
			_tempLink.InitWith( new LinkModel( null, null ) );
			_tempLink.SetSource( _conceptViewsByNodeUid[nodeUid] );
			_tempLink.SetFree();
		}
		private void DragLinkReceiving ( Concept.DragLinkEventData eventData ) {

			if ( eventData.IsReceiving ) {
				_tempLink.SetDocked( _conceptViewsByNodeUid[eventData.NodeUID] );
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