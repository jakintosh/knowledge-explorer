using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {

	public class RelationshipTypeBrowser : View {

		// *********** Public Interface ***********

		// public void SetActiveWorkspace ( Model.Workspace workspace ) => _activeGraph.Set( workspace );


		// *********** Private Interface ***********

		[Header( "UI Subviews" )]
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private ValidatedTextEntryDialog _newRelationshipTypeDialog;

		[Header( "UI Control" )]
		[SerializeField] private Button _newRelationshipTypeButton;
		[SerializeField] private RelationshipTypeList _relationshipListLayout;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _relationshipTypeListContainer;
		[SerializeField] private GameObject _fade;


		// model data
		private Observable<bool> _dialogOpen;
		private Observable<Model.KnowledgeGraph> _activeGraph;
		private Observable<Model.Context> _currentContext;
		private ListObservable<string> _allRelationshipTypeUIDs;


		protected override void Init () {

			// init subviews
			InitView( _presenceControl );
			InitView( _newRelationshipTypeDialog );

			// init observables
			_dialogOpen = new Observable<bool>(
				initialValue: false,
				onChange: open => {
					_fade.SetActive( open );
					_newRelationshipTypeDialog.gameObject.SetActive( open );
					_newRelationshipTypeButton.interactable = !open;
					_presenceControl.SetInteractive( close: false, size: !open, context: false );
				}
			);
			_currentContext = new Observable<Model.Context>(
				initialValue: Application.State.Contexts.Current,
				onChange: context => {
					var prev = _currentContext?.Previous();
					if ( prev != null ) { prev.OnGraphChanged -= HandleNewGraph; }
					if ( context != null ) { context.OnGraphChanged += HandleNewGraph; }
				}
			);
			_activeGraph = new Observable<Model.KnowledgeGraph>(
				initialValue: null,
				onChange: graph => {
					gameObject.SetActive( graph != null );
					if ( graph == null ) { return; }
					var cellData = graph.AllRelationshipTypes.Convert( kvp => new RelationshipTypeCellData( kvp.Key, kvp.Value ) );
					_relationshipListLayout.SetData( cellData );
					_newRelationshipTypeDialog.SetValidators( validators: candidate => !graph.AllRelationshipTypeNames.Contains( candidate ) );
				}
			);
			_allRelationshipTypeUIDs = new ListObservable<string>(
				initialValue: null,
				onChange: allRelationshipUIDs => {

					// _relationshipListLayout.SetData()
				}
			);

			// connect controls
			_newRelationshipTypeButton.onClick.AddListener( () => {
				_dialogOpen.Set( true );
			} );
			_newRelationshipTypeDialog.OnClose.AddListener( () => {
				_dialogOpen.Set( false );
			} );
			_newRelationshipTypeDialog.OnConfirm.AddListener( name => {
				_activeGraph.Get().NewRelationshipType( name );
			} );
			_presenceControl.OnSizeChanged.AddListener( presenceSize => {
				var isExpanded = presenceSize == Model.Presence.Sizes.Expanded;
				_relationshipTypeListContainer.gameObject.SetActive( isExpanded );
				_newRelationshipTypeButton.gameObject.SetActive( isExpanded );
			} );

			// listen to application events
			Application.State.Contexts.OnCurrentContextChanged += context => {
				_currentContext.Set( context );
			};

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newRelationshipTypeDialog.SetTitle( title: "New Relationship Type" );
		}

		private void HandleNewGraph ( Model.KnowledgeGraph graph ) => _activeGraph?.Set( graph );

	}

}