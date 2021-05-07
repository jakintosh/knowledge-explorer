using Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {

	public class RelationTypeBrowser : View {

		// *********** Public Interface ***********

		public void SetContext ( Model.View.Workspace workspace, Knowledge.Graph graph ) {

			_workspace = workspace;

			_graph?.OnRelationTypeEvent.RemoveListener( HandleRelationTypeEvent );
			_graph = graph;
			_graph?.OnRelationTypeEvent.AddListener( HandleRelationTypeEvent );

			_windowOpen.Set( _graph != null );
			_cellData.Set( GetCellData( _graph, _workspace ) );

			if ( _graph != null ) {
				_newRelationTypeDialog.SetValidators( validators: _graph.ValidateRelationTypeName );
			} else {
				_newRelationTypeDialog.SetValidators( null );
			}
		}


		// *********** Private Interface ***********

		[Header( "UI Subviews" )]
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private ValidatedTextEntryDialog _newRelationTypeDialog;
		[SerializeField] private RelationTypeEditor _relationTypeEditor;

		[Header( "UI Control" )]
		[SerializeField] private Button _newRelationTypeButton;
		[SerializeField] private RelationTypeList _relationList;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _contentContainer;
		[SerializeField] private GameObject _fade;


		// view model data
		private Observable<bool> _windowOpen;
		private Observable<bool> _dialogOpen;
		private Observable<string> _selectedRelationTypeUID;
		private ListObservable<RelationTypeCellData> _cellData;

		// internal data
		private Knowledge.Graph _graph;
		private Model.View.Workspace _workspace;

		protected override void Init () {

			// init subviews
			InitView( _presenceControl );
			InitView( _newRelationTypeDialog );
			InitView( _relationTypeEditor );

			// init observables
			_windowOpen = new Observable<bool>(
				initialValue: false,
				onChange: open => {
					gameObject.SetActive( open );
				}
			);
			_dialogOpen = new Observable<bool>(
				initialValue: false,
				onChange: open => {
					_fade.SetActive( open );
					_newRelationTypeDialog.gameObject.SetActive( open );
					_newRelationTypeButton.interactable = !open;
					_presenceControl.SetInteractive( close: false, size: !open, context: false );
				}
			);
			_selectedRelationTypeUID = new Observable<string>(
				initialValue: null,
				onChange: relationTypeID => {
					if ( relationTypeID == null ) { return; }
					var relType = _graph.GetRelationType( relationTypeID );
					_relationTypeEditor.SetRelationType( relType );
				}
			);
			_cellData = new ListObservable<RelationTypeCellData>(
				initialValue: null,
				onChange: cellData => {
					Debug.Log( "getting new list data" );
					_relationList.SetData( cellData );
				}
			);


			// connect controls
			_presenceControl.OnSizeChanged.AddListener( presenceSize => {
				_contentContainer.gameObject.SetActive( presenceSize == PresenceControl.Sizes.Expanded );
			} );

			_newRelationTypeButton.onClick.AddListener( () => {
				_dialogOpen.Set( true );
			} );

			_newRelationTypeDialog.OnClose.AddListener( () => {
				_dialogOpen.Set( false );
			} );
			_newRelationTypeDialog.OnConfirm.AddListener( name => {
				_graph?.NewRelationType( name );
			} );

			_relationList.OnCellClicked.AddListener( cellData => {
				_selectedRelationTypeUID.Set( cellData.UID );
			} );

			_relationTypeEditor.OnNameChanged.AddListener( ( uid, name ) => {
				_graph?.UpdateRelationTypeName( uid, name );
			} );
			_relationTypeEditor.OnColorStringChanged.AddListener( ( uid, colorString ) => {
				Debug.Log( $"got new color {colorString} for {uid}" );
				_workspace?.SetRelationTypeColor( uid, colorString );
				_cellData.Set( GetCellData( _graph, _workspace ) );
			} );

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newRelationTypeDialog.SetTitle( title: "New Relation Type" );
		}

		private List<RelationTypeCellData> GetCellData ( Knowledge.Graph graph, Model.View.Workspace workspace ) {

			if ( graph == null || workspace == null ) {
				Debug.Log( $"RelationTypeBrowser: Can't get cell data, mission (graph || workspace)" );
				return new List<RelationTypeCellData>();
			}

			return graph.AllRelationTypes.ConvertToList( ( uid, relType ) =>
				new RelationTypeCellData(
					uid: uid,
					name: relType.Name,
					colorString: workspace.GetRelationTypeColor( uid )
				)
			);
		}

		// event handlers
		private void HandleRelationTypeEvent ( Knowledge.Graph.ResourceEventData eventData ) {

			// update cell data
			var cellData = GetCellData( _graph, _workspace );
			_cellData.Set( cellData );
		}
	}

}