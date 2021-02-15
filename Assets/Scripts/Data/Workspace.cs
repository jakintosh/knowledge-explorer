using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Model {

	[Serializable]
	public class WorkspaceMetadata {
		[SerializeField] public List<NodeMetadata> NodeMetadata = new List<NodeMetadata>();
	}

	[Serializable]
	public struct NodeMetadata {
		[SerializeField] public string ID;
		[SerializeField] public Vector3 Position;
	}

	public class Workspace : MonoBehaviour {

		// ********** singleton management **********

		private static Workspace _instance;
		public static Workspace Instance {
			get {
				if ( _instance == null ) {
					_instance = FindObjectOfType<Workspace>();
					if ( _instance == null ) {
						Debug.LogError( "Workspace: No Workspace.Instance found" );
					}
				}
				return _instance;
			}
		}

		// ****************************************


		// *********** Public Interface ***********

		public void OpenNode ( string id ) {

			if ( _nodeMetadataByID.ContainsKey( id ) ) {

				// Highlight that node

			} else {

				var node = Bucket.Instance.GetNodeForID( id );
				if ( node == null ) {
					Debug.LogWarning( $"Workspace: Couldn't open node {id}, doesn't exist" );
					return;
				}

				var metadata = new NodeMetadata() {
					ID = node.ID,
					Position = Vector3.zero
				};
				InstantiateNode( metadata );
			}
		}
		public void CloseNode ( string id ) {

			if ( _nodeMetadataByID.ContainsKey( id ) ) {

				var node = _nodesByID[id];
				_nodesByID.Remove( id );
				_nodeMetadataByID.Remove( id );
				Destroy( node.gameObject );
			}
		}
		public void OpenSearch () {

			HandleSearchClicked();
		}

		// ********** Private Interface ***********

		private void Awake () {

			_searchAnimation = new Timer( 0.16f );

			_nodesByID = new Dictionary<string, View.Node>();
			_nodeMetadataByID = new Dictionary<string, NodeMetadata>();

			_searchField.onValueChanged.AddListener( HandleSearchChanged );
			_newNodeButton.onClick.AddListener( CreateNewNode );
			_saveButton.onClick.AddListener( SaveBucket );

			LoadBucket();
			LoadWorkspace();
		}
		private void Update () {

			AnimateSearch();
		}
		private void OnDestroy () {

			SaveWorkspace();
		}

		// node management
		private void InstantiateNode ( NodeMetadata nodeMetadata ) {

			var id = nodeMetadata.ID;
			var nodeData = Bucket.Instance.GetNodeForID( id );
			if ( nodeData == null ) {
				// no node in the database with this ID, most likely didn't save the database
				Debug.LogWarning( $"Workspace: Node {id} found in workspace, but not in database. The database probably wasn't saved last session." );
				return;
			}
			var nodeView = Instantiate<View.Node>( _nodePrefab );
			nodeView.Data = nodeData;
			nodeView.transform.position = nodeMetadata.Position;
			nodeView.OnPositionChanged += HandleNodeMoved;
			_nodesByID[id] = nodeView;
			_nodeMetadataByID[id] = nodeMetadata;
		}
		private void CreateNewNode () {

			var node = Bucket.Instance.NewNode();
			var metadata = new NodeMetadata() {
				ID = node.ID,
				Position = new Vector3( -1.5f, 3f, 0f )
			};
			InstantiateNode( metadata );
		}
		private void HandleNodeMoved ( string id, Vector3 position ) {

			if ( !_nodeMetadataByID.ContainsKey( id ) ) {
				Debug.LogWarning( $"Workspace: Tried to update metadata for node {id}, but doesn't exist" );
			}

			var metadata = _nodeMetadataByID[id];
			metadata.Position = position;
			_nodeMetadataByID[id] = metadata;
		}

		// search stuff
		private void HandleSearchClicked () {

			_searchOpen = _searchOpen.Toggled();
			_searchAnimation.Start();
		}
		private void HandleSearchChanged ( string searchString ) {

			// kill children
			foreach ( Transform child in _searchResults ) {
				Destroy( child.gameObject );
			}

			// set search results active
			_searchResults.gameObject.SetActive( !string.IsNullOrEmpty( searchString ) );

			// populate results
			var results = Bucket.Instance.SearchTitles( searchString );
			foreach ( var result in results ) {
				var cell = Instantiate<View.SearchResultCell>( _searchResultCellPrefab, _searchResults );
				cell.SetNode( result.Result );
			}
		}
		private void AnimateSearch () {

			if ( _searchAnimation.IsRunning ) {

				var startWidth = _searchOpen ? 100f : 600f;
				var endWidth = _searchOpen ? 600f : 100f;

				var rt = _searchField.transform as RectTransform;
				var size = rt.sizeDelta;
				size.x = startWidth.Lerp( to: endWidth, _searchAnimation.Percentage );
				rt.sizeDelta = size;

				var startAlpha = _searchOpen ? 0f : 1f;
				var endAlpha = _searchOpen ? 1f : 0f;
				var currentAlpha = startAlpha.Lerp( to: endAlpha, _searchAnimation.Percentage );
				_searchText.alpha = currentAlpha;
				_searchResultsGroup.alpha = currentAlpha;

				if ( _searchAnimation.IsComplete ) {
					_searchAnimation.Stop();
					if ( _searchOpen ) {
						// do stuff on finish open
					} else {
						// do stuff on finish close
						_searchField.text = "";
					}
				}
			}
		}

		// workspace save/load
		private void SaveWorkspace () {

			var metadata = new WorkspaceMetadata();
			metadata.NodeMetadata = new List<NodeMetadata>( _nodeMetadataByID.Values );
			PersistentStore.Save( "/data/workspaces/default.workspace", metadata );
		}
		private void LoadWorkspace () {

			var metadata = PersistentStore.Load<WorkspaceMetadata>( "/data/workspaces/default.workspace" );
			foreach ( var nodeMetadata in metadata.NodeMetadata ) {
				InstantiateNode( nodeMetadata );
			}
		}

		// bucket save/load
		private void SaveBucket () {

			Bucket.Instance.Save();
		}
		private void LoadBucket () {

			Bucket.Instance.Load();
		}


		// ************* Private Data *************

		[Header( "UI Components" )]
		[SerializeField] private TMP_InputField _searchField;
		[SerializeField] private CanvasGroup _searchText;
		[SerializeField] private RectTransform _searchResults;
		[SerializeField] private CanvasGroup _searchResultsGroup;
		[SerializeField] private Button _newNodeButton;
		[SerializeField] private Button _saveButton;

		[Header( "Components" )]
		[SerializeField] private View.Node _nodePrefab;
		[SerializeField] private View.SearchResultCell _searchResultCellPrefab;

		private Dictionary<string, View.Node> _nodesByID;
		private Dictionary<string, NodeMetadata> _nodeMetadataByID;

		private bool _searchOpen;
		private Timer _searchAnimation;
	}
}
