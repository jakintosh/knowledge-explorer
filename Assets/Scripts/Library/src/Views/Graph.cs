using Jakintosh.Observable;
using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;

namespace Library.Views {

	public class Graph : View {

		[Header( "UI Control" )]
		[SerializeField] private WorkspaceMenu _workspaceMenu;
		[SerializeField] private GraphToolbar _graphToolbar;

		[Header( "UI Display" )]
		[SerializeField] private Transform _conceptContainer;

		[Header( "UI Assets" )]
		[SerializeField] private Concept _conceptPrefab;


		// private data
		private int _lastActiveConceptIndex;
		private List<Concept> _conceptViews;

		private Observable<ViewModel.Workspace> _workspace;

		protected override void OnInitialize () {

			// init data
			_conceptViews = new List<Concept>();

			// init subviews
			_workspaceMenu.Init();
			_graphToolbar.Init();

			// init observables
			_workspace = new Observable<ViewModel.Workspace>(
				initialValue: null,
				onChange: workspace => {
					_graphToolbar.gameObject.SetActive( workspace != null );
					PopulateGraphViewport( workspace?.GraphViewport );
				}
			);

			// sub to controls
			_graphToolbar.OnNewItem.AddListener( () => {

				Library.App.History.ExecuteAction(
					new Actions.Concept.Create(
						shouldOpen: true
					)
				);
			} );
			_graphToolbar.OnDeleteItem.AddListener( () => {

			} );
			_graphToolbar.OnSave.AddListener( () => {
				SaveViewportState();
			} );

			// sub to model
			Library.App.State.ActiveWorkspace.Subscribe( _workspace.Set );
		}
		protected override void OnCleanup () {

			// unsub from model
			Library.App.State.ActiveWorkspace.Unsubscribe( _workspace.Set );
		}

		// event handlers

		// private functions
		private void SaveViewportState () {

			var conceptViews = _conceptViews
				.Filter( view => view.gameObject.activeSelf );
			conceptViews.ForEach( view => view.Save() );

			var conceptModels = conceptViews
				.Convert( view => view.GetState() );

			var linkModels = (List<ViewModel.Link>)null;

			var viewportModel = new ViewModel.GraphViewport( conceptModels, linkModels );

			_workspace.Get().GraphViewport = viewportModel;
		}

		// get next concept view
		private Concept GetConceptView () {

			// if there isn't another one left in queue, create one
			if ( ++_lastActiveConceptIndex > _conceptViews.Count - 1 ) {
				var view = Instantiate<Concept>( _conceptPrefab, _conceptContainer, false );
				_conceptViews.Add( view );
			}
			_conceptViews[_lastActiveConceptIndex].gameObject.SetActive( true );
			return _conceptViews[_lastActiveConceptIndex];
		}

		// view population
		private void PopulateGraphViewport ( ViewModel.GraphViewport viewportModel ) {

			PopulateConceptViews( viewportModel?.Concepts );
		}
		private void PopulateConceptViews ( SubscribableDictionary<ViewHandle, ViewModel.Concept> models ) {

			var viewModels = models?.GetAll();
			_lastActiveConceptIndex = ( viewModels?.Count ?? 0 ) - 1;

			while ( _conceptViews.Count - 1 < _lastActiveConceptIndex ) {
				var view = Instantiate<Concept>( _conceptPrefab, _conceptContainer, false );
				_conceptViews.Add( view );
			}

			for ( int i = 0; i < _conceptViews.Count; i++ ) {
				var view = _conceptViews[i];
				var hasModel = i <= _lastActiveConceptIndex;
				if ( hasModel ) { view.InitWith( viewModels[i] ); }
				view.gameObject.SetActive( hasModel );
			}
		}

	}
}