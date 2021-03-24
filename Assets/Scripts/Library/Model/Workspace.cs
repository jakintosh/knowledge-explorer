using Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Library.Model {

	[Serializable]
	public class LinkMetadata {
		[SerializeField] public string LinkID;
		[SerializeField] public string SourceID;
		[SerializeField] public string DestinationID;

		[NonSerialized] public LineRenderer Renderer;
	}

	public class Workspace : MonoBehaviour {

		[Serializable]
		public class Metadata {
			[SerializeField] public List<View.Node.Metadata> NodeMetadata = new List<View.Node.Metadata>();
			[SerializeField] public List<LinkMetadata> LinkMetadata = new List<LinkMetadata>();
			[SerializeField] public Bucket Bucket = new Bucket();
		}


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


		// creates and removes node metadata
		// updates info when node data changes
		// creates and removes link metadata

		// *********** Public Interface ***********

		public void OpenNode ( string nodeID, string sourceID = null ) {

			// if node is already open, just jump to it
			if ( _nodeMetadataByID.ContainsKey( nodeID ) ) {
				// Highlight that node, then exit
				return;
			}

			// if node data doesn't exist, abort
			var node = _bucket.GetNodeForID( nodeID );
			if ( node == null ) {
				Debug.LogWarning( $"Workspace: Couldn't open node {nodeID}, doesn't exist" );
				return;
			}


			// if we have a source ID, we need link and position


			var metadata = new View.Node.Metadata() {
				ID = node.ID,
				Position = Vector3.zero,
				WindowState = View.Node.WindowStates.Maximized,
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

		// ********** Private Interface ***********

		private void Awake () {

			// load from disk
			LoadBucket();
			LoadWorkspace();

			// listen for button events
			_newNodeButton.onClick.AddListener( CreateNewNode );
			_saveButton.onClick.AddListener( SaveBucket );
		}
		private void OnDestroy () {

			// save to disk
			SaveWorkspace();
		}

		// node management
		private void InstantiateNode ( View.Node.Metadata nodeMetadata ) {

			// get node data
			var id = nodeMetadata.ID;
			var nodeData = _bucket.GetNodeForID( id );
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
			nodeView.RestoreFromMetadata( nodeMetadata );
			nodeView.OnPositionChanged += HandleNodeMoved;
			_nodeViewsByID[id] = nodeView;
		}
		private void CreateNewNode () {

			var node = _bucket.NewNode();
			var metadata = new View.Node.Metadata() {
				ID = node.ID,
				Position = new Vector3( -1.5f, 3f, 0f ),
				WindowState = View.Node.WindowStates.Maximized,
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
			link.LinkID = StringHelpers.UID.Generate( length: LINK_ID_LENGTH, validateUniqueness: id => _linkMetadataByID.KeyIsUnique( id ) );
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

			// create renderer if necessary
			if ( link.Renderer == null ) {
				link.Renderer = new GameObject().AddComponent<LineRenderer>();
				link.Renderer.startWidth = 0.1f;
			}

			var sourceNodeView = _nodeViewsByID[link.SourceID];
			var destinationNodeView = _nodeViewsByID[link.DestinationID];

			var startPosition = sourceNodeView.LinkOutPosition;
			var endPosition = destinationNodeView.LinkInPosition;

			link.Renderer.SetPositions( new Vector3[] {
				startPosition,
				endPosition
			} );
		}
		private void RedrawLinks ( string nodeID ) {

			_nodeMetadataByID[nodeID].LinkIDs.ForEach( id => DrawLink( id ) );
		}


		// workspace save/load
		private void SaveWorkspace () {

			var metadata = new Workspace.Metadata();
			metadata.NodeMetadata = new List<View.Node.Metadata>( _nodeMetadataByID.Values );
			metadata.LinkMetadata = new List<LinkMetadata>( _linkMetadataByID.Values );
			metadata.Bucket = _bucket;
			Framework.Data.PersistentStore.Save( "/data/workspaces/default.workspace", metadata );
		}
		private void LoadWorkspace () {

			var workspace = Framework.Data.PersistentStore.Load<Workspace.Metadata>( "/data/workspaces/default.workspace" );

			// restore bucket
			_bucket = workspace.Bucket;

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

			// _bucket.Save();
		}
		private void LoadBucket () {

			// _bucket.Load();
		}


		// ************* Private Data *************

		private static int LINK_ID_LENGTH = 8;

		[Header( "UI Components" )]
		[SerializeField] private Button _newNodeButton;
		[SerializeField] private Button _saveButton;

		[Header( "Components" )]
		[SerializeField] private View.Node _nodePrefab;

		private Bucket _bucket;
		private Dictionary<string, View.Node> _nodeViewsByID = new Dictionary<string, View.Node>();
		private Dictionary<string, View.Node.Metadata> _nodeMetadataByID = new Dictionary<string, View.Node.Metadata>();
		private Dictionary<string, LinkMetadata> _linkMetadataByID = new Dictionary<string, LinkMetadata>();
	}
}
