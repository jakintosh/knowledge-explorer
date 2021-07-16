using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using WorkspaceVM = Explorer.View.Model.Workspace;
using KnowledgeGraph = Jakintosh.Knowledge.Graph;

namespace Explorer.View {

	public class Graph : View {

		[Header( "UI Control" )]
		[SerializeField] private WorkspaceMenu _workspaceMenu;
		[SerializeField] private GraphToolbar _graphToolbar;

		[Header( "UI Display" )]
		[SerializeField] private Transform _conceptContainer;

		[Header( "UI Assets" )]
		[SerializeField] private App.View.Concept _conceptPrefab;


		// private data
		private int _lastActiveConceptIndex;
		private List<App.View.Concept> _conceptViews;

		private Observable<WorkspaceVM> _workspace;
		private Observable<KnowledgeGraph> _graph;



		protected override void OnInitialize () {

			// init data
			_conceptViews = new List<App.View.Concept>();


			// init subviews
			_workspaceMenu.Init();
			_graphToolbar.Init();


			// init observables
			_graph = new Observable<KnowledgeGraph>(
				initialValue: null,
				onChange: graph => {
					_graphToolbar.gameObject.SetActive( graph != null );
				}
			);
			_workspace = new Observable<WorkspaceVM>(
				initialValue: null,
				onChange: workspace => {
					_graph.Set( Client.Resources.Graphs.Get( workspace?.GraphUID ) );
					PopulateGraphViewport( workspace?.GraphViewport );
				}
			);


			// sub to controls
			_graphToolbar.OnNewItem.AddListener( () => {
				var view = GetConceptView();
				var model = Model.Concept.Default(
					graphUid: _graph.Get().UID,
					nodeUid: _graph.Get().CreateConcept()
				);
				view.InitWith( model );
			} );
			_graphToolbar.OnDeleteItem.AddListener( () => {

			} );
			_graphToolbar.OnSave.AddListener( () => {
				SaveViewportState();
			} );

			Client.Contexts.Current.OnContextStateModified.AddListener( HandleNewState );
		}
		protected override void OnCleanup () {

			Client.Contexts.Current.OnContextStateModified.RemoveListener( HandleNewState );
		}

		// event handlers
		private void HandleNewState ( Client.ExplorerContextState state )
			=> _workspace.Set( state.Workspace );

		// private functions
		private void SaveViewportState () {

			var conceptViews = _conceptViews
				.Filter( view => view.gameObject.activeSelf );
			conceptViews.ForEach( view => view.Save() );

			var conceptModels = conceptViews
				.Convert( view => view.GetState() );

			var linkModels = (List<Model.Link>)null;

			var viewportModel = new Model.GraphViewport( conceptModels, linkModels );

			_workspace.Get().GraphViewport = viewportModel;
		}

		// get next concept view
		private App.View.Concept GetConceptView () {

			// if there isn't another one left in queue, create one
			if ( ++_lastActiveConceptIndex > _conceptViews.Count - 1 ) {
				var view = Instantiate<App.View.Concept>( _conceptPrefab, _conceptContainer, false );
				_conceptViews.Add( view );
			}
			_conceptViews[_lastActiveConceptIndex].gameObject.SetActive( true );
			return _conceptViews[_lastActiveConceptIndex];
		}

		// view population
		private void PopulateGraphViewport ( Model.GraphViewport viewportModel ) {

			PopulateConceptViews( viewportModel?.Concepts );
		}
		private void PopulateConceptViews ( List<Explorer.View.Model.Concept> models ) {

			_lastActiveConceptIndex = ( models?.Count ?? 0 ) - 1;

			while ( _conceptViews.Count - 1 < _lastActiveConceptIndex ) {
				var view = Instantiate<App.View.Concept>( _conceptPrefab, _conceptContainer, false );
				_conceptViews.Add( view );
			}

			for ( int i = 0; i < _conceptViews.Count; i++ ) {
				var view = _conceptViews[i];
				var hasModel = i <= _lastActiveConceptIndex;
				if ( hasModel ) { view.InitWith( models[i] ); }
				view.gameObject.SetActive( hasModel );
			}
		}

	}
}