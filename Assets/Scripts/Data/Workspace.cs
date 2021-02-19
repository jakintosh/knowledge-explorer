using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Model {

	[Serializable]
	public class WorkspaceMetadata {
		[SerializeField] public List<NodeMetadata> NodeMetadata = new List<NodeMetadata>();
		[SerializeField] public List<LinkMetadata> LinkMetadata = new List<LinkMetadata>();
	}

	[Serializable]
	public struct NodeMetadata {
		[SerializeField] public string ID;
		[SerializeField] public Vector3 Position;
		[SerializeField] public List<string> LinkIDs; // this is a list because unity cant serialize sets! wow!
	}

	[Serializable]
	public class LinkMetadata {
		[SerializeField] public string LinkID;
		[SerializeField] public string SourceID;
		[SerializeField] public string DestinationID;

		[NonSerialized] public LineRenderer Renderer;
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


		// *********** Public Interface ***********

		public void OpenNode ( string nodeID, string sourceID = null ) {

			// if node is already open, just jump to it
			if ( _nodeMetadataByID.ContainsKey( nodeID ) ) {
				// Highlight that node, then exit
				return;
			}

			// if node data doesn't exist, abort
			var node = Bucket.Instance.GetNodeForID( nodeID );
			if ( node == null ) {
				Debug.LogWarning( $"Workspace: Couldn't open node {nodeID}, doesn't exist" );
				return;
			}


			// if we have a source ID, we need link and position


			var metadata = new NodeMetadata() {
				ID = node.ID,
				Position = Vector3.zero,
				LinkIDs = new List<string>()
			};
			InstantiateNode( metadata );


			// add link
			if ( sourceID != null ) {
				var link = CreateLinkMetadata( sourceID, destinationID: nodeID );
				_nodeMetadataByID[sourceID].LinkIDs.Add( link.LinkID );
				_nodeMetadataByID[nodeID].LinkIDs.Add( link.LinkID );
				DrawLink( link );
			}
		}
		public void CloseNode ( string nodeID ) {

			if ( _nodeMetadataByID.TryGetValue( nodeID, out var nodeMetadata ) ) {

				// remove links
				var linkIDs = nodeMetadata.LinkIDs.ToArray();
				foreach ( var linkID in linkIDs ) {
					RemoveLinkMetadata( linkID );
				}

				// remove metadata
				var nodeView = _nodeViewsByID[nodeID];
				_nodeViewsByID.Remove( nodeID );
				_nodeMetadataByID.Remove( nodeID );

				// destroy game object
				Destroy( nodeView.gameObject );
			}
		}

		public void OpenSearch () {

			HandleSearchClicked();
		}

		// ********** Private Interface ***********

		private void Awake () {

			_searchAnimation = new Timer( 0.16f );

			_nodeViewsByID = new Dictionary<string, View.Node>();
			_nodeMetadataByID = new Dictionary<string, NodeMetadata>();
			_linkMetadataByID = new Dictionary<string, LinkMetadata>();

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

			// get node data
			var id = nodeMetadata.ID;
			var nodeData = Bucket.Instance.GetNodeForID( id );
			if ( nodeData == null ) {
				// no node in the database with this ID, most likely didn't save the database
				Debug.LogWarning( $"Workspace: Node {id} found in workspace, but not in database. The database probably wasn't saved last session." );
				return;
			}

			// register metadata
			_nodeMetadataByID[id] = nodeMetadata;

			// create and set up view
			var nodeView = Instantiate<View.Node>( _nodePrefab );
			nodeView.Data = nodeData;
			nodeView.transform.position = nodeMetadata.Position;
			nodeView.OnPositionChanged += HandleNodeMoved;
			_nodeViewsByID[id] = nodeView;
		}
		private void CreateNewNode () {

			var node = Bucket.Instance.NewNode();
			var metadata = new NodeMetadata() {
				ID = node.ID,
				Position = new Vector3( -1.5f, 3f, 0f ),
				LinkIDs = new List<string>()
			};
			InstantiateNode( metadata );
		}
		private void HandleNodeMoved ( string nodeID, Vector3 position ) {

			if ( !_nodeMetadataByID.ContainsKey( nodeID ) ) {
				Debug.LogWarning( $"Workspace: Tried to update metadata for node {nodeID}, but doesn't exist" );
			}

			var metadata = _nodeMetadataByID[nodeID];
			metadata.Position = position;
			_nodeMetadataByID[nodeID] = metadata;

			RedrawLinks( nodeID );
		}

		// link management
		private LinkMetadata CreateLinkMetadata ( string sourceID, string destinationID ) {

			var link = new LinkMetadata();
			link.LinkID = GenerateLinkID();
			link.SourceID = sourceID;
			link.DestinationID = destinationID;
			link.Renderer = null;

			_linkMetadataByID[link.LinkID] = link;

			return link;
		}
		private void RemoveLinkMetadata ( string linkID ) {

			if ( _linkMetadataByID.TryGetValue( linkID, out var link ) ) {
				Destroy( link.Renderer );
				_nodeMetadataByID[link.SourceID].LinkIDs.Remove( linkID );
				_nodeMetadataByID[link.DestinationID].LinkIDs.Remove( linkID );
			}
		}
		private void DrawLink ( string linkID ) {

			// get link
			if ( !_linkMetadataByID.TryGetValue( linkID, out var link ) ) {
				// couldn't draw link, doesn't exist
				return;
			}

			DrawLink( link );
		}
		private void DrawLink ( LinkMetadata link ) {

			var sourceNodeView = _nodeViewsByID[link.SourceID];
			var destinationNodeView = _nodeViewsByID[link.DestinationID];

			var startPosition = sourceNodeView.LinkOutPosition;
			var endPosition = destinationNodeView.LinkInPosition;

			if ( link.Renderer == null ) {
				link.Renderer = new GameObject().AddComponent<LineRenderer>();
				link.Renderer.startWidth = 0.1f;
			}
			link.Renderer.SetPositions( new Vector3[] {
				startPosition,
				endPosition
			} );
		}
		private void RedrawLinks ( string nodeID ) {

			_nodeMetadataByID[nodeID].LinkIDs.ForEach( id => DrawLink( id ) );
		}

		public static int LINK_ID_LENGTH = 8;
		private static char[] LINK_ID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
		private string GenerateLinkID () {

			var random = new System.Random( (int)( Time.time * 1000 ) );
			var sb = new StringBuilder( capacity: LINK_ID_LENGTH );
			string id;
			do {
				sb.Clear();
				for ( int i = 0; i < LINK_ID_LENGTH; i++ ) {
					var c = LINK_ID_CHARS[random.Next( LINK_ID_CHARS.Length )];
					sb.Append( c );
				}
				id = sb.ToString();
			} while ( _linkMetadataByID.ContainsKey( id ) );
			return id;
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
			metadata.LinkMetadata = new List<LinkMetadata>( _linkMetadataByID.Values );
			PersistentStore.Save( "/data/workspaces/default.workspace", metadata );
		}
		private void LoadWorkspace () {

			var workspace = PersistentStore.Load<WorkspaceMetadata>( "/data/workspaces/default.workspace" );

			// restore nodes
			foreach ( var nodeMetadata in workspace.NodeMetadata ) {
				InstantiateNode( nodeMetadata );
			}

			// restore links
			foreach ( var linkMetadata in workspace.LinkMetadata ) {
				_linkMetadataByID.Add( linkMetadata.LinkID, linkMetadata );
				DrawLink( linkMetadata );
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

		private Dictionary<string, View.Node> _nodeViewsByID;
		private Dictionary<string, NodeMetadata> _nodeMetadataByID;
		private Dictionary<string, LinkMetadata> _linkMetadataByID;

		private bool _searchOpen;
		private Timer _searchAnimation;
	}
}
