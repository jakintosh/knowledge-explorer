using Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Explorer.View {

	/*
		Explorer.View.Workspace

		Listens to events

	*/
	public class Workspace : RootView {

		// *********** Public Interface ***********



		// *********** Private Interface ***********

		[Header( "UI Controls" )]
		[SerializeField] private WorkspaceBrowser _workspaceBrowser = null;
		[SerializeField] private WorkspaceToolbar _workspaceToolbar = null;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _uiContainer = null;

		[Header( "Prefabs" )]
		[SerializeField] private Concept _conceptViewPrefab = null;

		// model data
		private Observable<Model.Workspace> _activeWorkspace;

		// private data
		private Model.Context _currentContext = null;
		private Dictionary<string, Concept> _conceptViewsByNodeId = new Dictionary<string, Concept>();

		protected override void Init () {

			// init subviews
			Init( _workspaceBrowser );
			Init( _workspaceToolbar );

			// init observables
			_activeWorkspace = new Observable<Model.Workspace>(
				initialValue: Application.State.Contexts.Current.Workspace,
				onChange: workspace => {

					_uiContainer.SetActive( workspace != null );
					_workspaceBrowser.SetActiveWorkspace( workspace );

					ClearView();
					if ( workspace != null ) {
						InstantiateConcepts( workspace.Concepts );
					}
				}
			);

			// subscribe to controls
			_workspaceToolbar.OnNewItem.AddListener( () => {
				var model = Model.Concept.Default(
					nodeUid: _currentContext.Graph.NewConcept(),
					graphUid: _activeWorkspace.Get().GraphUID
				);
				OpenConcept( model );
			} );
			_workspaceToolbar.OnSave.AddListener( () => {
				_activeWorkspace.Get().SetConcepts(
					_conceptViewsByNodeId.Values.Convert( view => view.GetModel() )
				);
			} );

			// subscribe to application notifications
			SubscribeToContext( Application.State.Contexts.Current );
			Application.State.Contexts.OnCurrentContextChanged += SubscribeToContext;
		}

		private void OpenConcept ( Model.Concept model ) {

			// create and init view
			var view = Instantiate<Concept>(
				original: _conceptViewPrefab,
				parent: this.transform,
				worldPositionStays: false
			);
			Init( view );

			// view setup
			view.SetModel( model );
			view.OnClose.AddListener( nodeUid => CloseConcept( nodeUid ) );

			// track
			_conceptViewsByNodeId.Add( model.NodeUID, view );
		}
		private void CloseConcept ( string nodeUid ) {

			var nodeView = _conceptViewsByNodeId[nodeUid];
			Destroy( nodeView.gameObject );
			_conceptViewsByNodeId.Remove( nodeUid );
		}
		private void InstantiateConcepts ( IEnumerable<Model.Concept> concepts ) {

			foreach ( var concept in concepts ) {
				OpenConcept( concept );
			}
		}
		private void ClearView () {

			_conceptViewsByNodeId.ForEach( ( nodeUid, view ) => Destroy( view.gameObject ) );
			_conceptViewsByNodeId.Clear();
		}


		private void SubscribeToContext ( Model.Context context ) {

			if ( _currentContext != null ) { _currentContext.OnWorkspaceChanged -= _activeWorkspace.Set; }
			_currentContext = context;
			if ( _currentContext != null ) { _currentContext.OnWorkspaceChanged += _activeWorkspace.Set; }
			_activeWorkspace.Set( _currentContext?.Workspace );
		}
	}

}