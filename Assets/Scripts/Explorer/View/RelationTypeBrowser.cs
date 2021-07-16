using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {

	public class RelationTypeBrowser : ReuseableView<Model.Workspace> {

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
		private Jakintosh.Knowledge.Graph _graph;
		private Model.Workspace _workspace;

		// view lifecycle
		public override Model.Workspace GetState () {
			return _workspace;
		}
		protected override void OnInitialize () {

			// init subviews
			_presenceControl.Init();
			_newRelationTypeDialog.Init();
			_relationTypeEditor.InitWith( null );

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newRelationTypeDialog.SetTitle( title: "New Relation Type" );

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
					var relType = _graph?.GetRelationType( relationTypeID );
					_relationTypeEditor.InitWith( relType );
				}
			);
			_cellData = new ListObservable<RelationTypeCellData>(
				initialValue: null,
				onChange: cellData => {
					_relationList.SetData( cellData );
				}
			);

			// sub to controls
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
				var metadata = _graph?.GetMetadataForRelationType( uid ) ?? new Jakintosh.Knowledge.Metadata.RelationType();
				metadata.Display.HexColor = colorString;
				_graph?.SetMetadataForRelationType( uid, metadata );

				_cellData.Set( GetCellData( _graph, _workspace ) );
			} );
		}
		protected override void OnPopulate ( Model.Workspace workspace ) {

			_workspace = workspace;
			_graph = Client.Application.Resources.Graphs.Get( _workspace?.GraphUID );

			_windowOpen.Set( _graph != null );
			_dialogOpen.Set( false );
			_selectedRelationTypeUID.Set( null );
			_cellData.Set( GetCellData( _graph, _workspace ) );

			if ( _graph != null ) {
				_graph.OnRelationTypeEvent.AddListener( HandleRelationTypeEvent );
				_newRelationTypeDialog.SetValidators( validators: _graph.ValidateRelationTypeName );
			}
		}
		protected override void OnRecycle () {

			_newRelationTypeDialog.SetValidators( null );
			_graph?.OnRelationTypeEvent.RemoveListener( HandleRelationTypeEvent );
		}
		protected override void OnCleanup () { }

		// event handlers
		private void HandleRelationTypeEvent ( Jakintosh.Knowledge.Graph.ResourceEventData eventData ) {

			// update cell data
			_cellData.Set( GetCellData( _graph, _workspace ) );
		}

		// helpers
		private List<RelationTypeCellData> GetCellData ( Jakintosh.Knowledge.Graph graph, Model.Workspace workspace ) {

			if ( graph == null ) {
				Debug.Log( $"RelationTypeBrowser: Can't get cell data, missing graph." );
				return new List<RelationTypeCellData>();
			}
			if ( workspace == null ) {
				Debug.Log( $"RelationTypeBrowser: Can't get cell data, missing workspace." );
				return new List<RelationTypeCellData>();
			}

			return graph.AllRelationTypes.ConvertToList( ( uid, relType ) => {

				return new RelationTypeCellData(
					uid: uid,
					name: relType.Name,
					colorString: graph.GetMetadataForRelationType( uid )?.Display.HexColor ?? ""
				);
			} );
		}

	}

}