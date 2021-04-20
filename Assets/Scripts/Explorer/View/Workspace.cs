using Framework;
using System.Collections.Generic;
using UnityEngine;

using WorkspaceModel = Explorer.Model.Workspace;
using ConceptModel = Explorer.Model.View.Concept;

namespace Explorer.View {


	public class Workspace : RootView {

		// *********** Public Interface ***********



		// *********** Private Interface ***********

		[Header( "UI Controls" )]
		[SerializeField] private WorkspaceBrowser _workspaceBrowser = null;
		[SerializeField] private RelationshipTypeBrowser _relTypeBrowser = null;
		[SerializeField] private WorkspaceToolbar _workspaceToolbar = null;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _uiContainer = null;

		[Header( "Prefabs" )]
		[SerializeField] private Concept _conceptViewPrefab = null;
		[SerializeField] private Relationship _relationshipViewPrefab = null;

		// model data
		private Observable<WorkspaceModel> _activeWorkspace;

		// private data
		private Model.Context _currentContext = null;
		private Dictionary<string, Concept> _conceptViewsByNodeId = new Dictionary<string, Concept>();
		private Dictionary<string, Relationship> _relationshipsById = new Dictionary<string, Relationship>();

		protected override void Init () {

			// init subviews
			InitView( _workspaceBrowser );
			InitView( _relTypeBrowser );
			InitView( _workspaceToolbar );

			// init observables
			_activeWorkspace = new Observable<WorkspaceModel>(
				initialValue: Application.State.Contexts.Current.Workspace,
				onChange: workspace => {

					_uiContainer.SetActive( workspace != null );
					_workspaceBrowser.SetActiveWorkspace( workspace );

					ClearView();

					workspace?.Concepts?.ForEach( concept => OpenConcept( concept ) );
					workspace?.Relationships?.ForEach( relUID => OpenRelationship( relUID ) );
				}
			);

			// subscribe to controls
			_workspaceToolbar.OnNewItem.AddListener( () => {
				var model = ConceptModel.Default(
					nodeUid: _currentContext.Graph.NewConcept(),
					graphUid: _activeWorkspace.Get().GraphUID
				);
				OpenConcept( model );
			} );
			_workspaceToolbar.OnSave.AddListener( () => {
				_activeWorkspace.Get()?.SetConcepts( _conceptViewsByNodeId.Values.Convert( view => view.GetInitData() ) );
				_activeWorkspace.Get()?.SetOpenRelationships( _relationshipsById.Keys );
			} );

			// subscribe to application notifications
			SubscribeToContext( Application.State.Contexts.Current );
			Application.State.Contexts.OnCurrentContextChanged += SubscribeToContext;
		}

		private void OpenRelationship ( string relationshipUid ) {

			// create and instantiate relationship
			var (source, dest, type) = _currentContext.Graph.GetRelationshipInfo( relationshipUid );
			var relationshipView = Instantiate<Relationship>( _relationshipViewPrefab );
			InitView( relationshipView );

			// view setup
			relationshipView.SetSource( _conceptViewsByNodeId[source] );
			relationshipView.SetAnchored( _conceptViewsByNodeId[dest] );

			// track
			_relationshipsById.Add( relationshipUid, relationshipView );
		}

		private void OpenConcept ( ConceptModel model ) {

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
			view.OnOpenRelationship.AddListener( OpenRelationship );

			// track
			_conceptViewsByNodeId.Add( model.NodeUID, view );
		}
		private void CloseConcept ( string nodeUid ) {

			var nodeView = _conceptViewsByNodeId[nodeUid];
			nodeView.OnClose.RemoveListener( CloseConcept );
			nodeView.OnDragLinkBegin.RemoveListener( NewDragLink );
			nodeView.OnDragLinkEnd.RemoveListener( DestroyDragLink );
			nodeView.OnDragLinkReceiving.RemoveListener( DragLinkReceiving );
			nodeView.OnOpenRelationship.RemoveListener( OpenRelationship );
			_conceptViewsByNodeId.Remove( nodeUid );
			Destroy( nodeView.gameObject );
		}
		private void ClearView () {

			_conceptViewsByNodeId.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_conceptViewsByNodeId.Clear();

			_relationshipsById.ForEach( ( _, view ) => Destroy( view.gameObject ) );
			_relationshipsById.Clear();
		}

		private Relationship _tempLink;
		private void NewDragLink ( string nodeUid ) {

			_tempLink = Instantiate<Relationship>( _relationshipViewPrefab );
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

		private void SubscribeToContext ( Model.Context context ) {

			if ( _currentContext != null ) { _currentContext.OnWorkspaceChanged -= _activeWorkspace.Set; }
			_currentContext = context;
			if ( _currentContext != null ) { _currentContext.OnWorkspaceChanged += _activeWorkspace.Set; }
			_activeWorkspace.Set( _currentContext?.Workspace );
		}
	}

}