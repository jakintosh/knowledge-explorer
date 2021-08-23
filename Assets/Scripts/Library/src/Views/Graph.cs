using Jakintosh.Data;
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

		// data
		private ViewPool<Concept, ViewModel.Concept> _conceptViewPool;
		private Dictionary<int, Concept> _conceptViews;
		private SubscribableDictionary<int, ViewModel.Concept> _conceptModels;

		protected override void OnInitialize () {

			// init subviews
			_workspaceMenu.Init();
			_graphToolbar.Init();

			// init data
			_conceptViews = new Dictionary<int, Concept>();
			_conceptViewPool = new ViewPool<Concept, ViewModel.Concept>(
				prefab: _conceptPrefab,
				container: _conceptContainer,
				setup: ( view, model ) => {
					view.InitWith( model );
					view.gameObject.SetActive( true );
				},
				teardown: view => {
					view.gameObject.SetActive( false );
				}
			);

			// sub to controls
			_graphToolbar.OnNewItem.AddListener( () => {
				Library.App.History.ExecuteAction(
					new Actions.Concept.Create( shouldOpen: true )
				);
			} );
			_graphToolbar.OnDeleteItem.AddListener( () => {

			} );

			// sub to app state
			Library.App.State.ActiveWorkspace.Subscribe( HandleNewWorkspace );
		}
		protected override void OnCleanup () {

			// unsub from app state
			Library.App.State.ActiveWorkspace.Unsubscribe( HandleNewWorkspace );
		}

		// event handlers
		private void HandleNewWorkspace ( ViewModel.Workspace workspace ) {

			_graphToolbar.gameObject.SetActive( workspace != null );
			PopulateGraphViewport( workspace?.GraphViewport );
		}

		// view population
		private void PopulateGraphViewport ( ViewModel.GraphViewport viewportModel ) {

			PopulateConceptViews( viewportModel?.Concepts );
		}
		private void PopulateConceptViews ( SubscribableDictionary<int, ViewModel.Concept> models ) {

			// release all old views
			_conceptViews.ForEach( ( handle, view ) => _conceptViewPool.ReleaseView( view ) );

			// unsub from old models
			_conceptModels?.OnAdded.RemoveListener( HandleConceptViewAdded );
			_conceptModels?.OnUpdated.RemoveListener( HandleConceptViewUpdated );
			_conceptModels?.OnRemoved.RemoveListener( HandleConceptViewRemoved );

			// open all new models
			_conceptModels = models;
			_conceptModels?.GetAll().ForEach( model => OpenView( model ) );

			// sub to future changes
			_conceptModels?.OnAdded.AddListener( HandleConceptViewAdded );
			_conceptModels?.OnUpdated.AddListener( HandleConceptViewUpdated );
			_conceptModels?.OnRemoved.AddListener( HandleConceptViewRemoved );
		}
		private void HandleConceptViewAdded ( int handle ) => OpenView( _conceptModels.Get( handle ) );
		private void HandleConceptViewUpdated ( int handle ) => _conceptViews[handle].InitWith( _conceptModels.Get( handle ) );
		private void HandleConceptViewRemoved ( int handle ) => CloseView( handle );

		private void OpenView ( ViewModel.Concept model ) {

			var view = _conceptViewPool.GetView( model );
			_conceptViews.Add( view.LinkedIdentifier, view );
		}
		private void CloseView ( int handle ) {

			var view = _conceptViews[handle];
			_conceptViews.Remove( handle );
			_conceptViewPool.ReleaseView( view );
		}

	}
}