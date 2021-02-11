using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

				var node = NodeManager.Instance.GetNodeForID( id );
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


		// ********** Private Interface ***********

		private void Awake () {

			_nodesByID = new Dictionary<string, View.Node>();
			_nodeMetadataByID = new Dictionary<string, NodeMetadata>();

			_newNodeButton.onClick.AddListener( CreateNewNode );
			_saveButton.onClick.AddListener( SaveBucket );

			LoadBucket();
			LoadWorkspace();
		}
		private void OnDestroy () {

			SaveWorkspace();
		}

		// node management
		private void InstantiateNode ( NodeMetadata nodeMetadata ) {

			var id = nodeMetadata.ID;
			var nodeData = NodeManager.Instance.GetNodeForID( id );
			var nodeView = Instantiate<View.Node>( _nodePrefab );
			nodeView.Data = nodeData;
			nodeView.transform.position = nodeMetadata.Position;
			nodeView.OnPositionChanged += HandleNodeMoved;
			_nodesByID[id] = nodeView;
			_nodeMetadataByID[id] = nodeMetadata;
		}
		private void CreateNewNode () {

			var node = NodeManager.Instance.NewNode();
			var metadata = new NodeMetadata() {
				ID = node.ID,
				Position = Vector3.zero
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

		private void SaveBucket () {

			NodeManager.Instance.Save();
		}
		private void LoadBucket () {

			NodeManager.Instance.Load();
		}


		// ************* Private Data *************

		[Header( "UI Components" )]
		[SerializeField] private Button _newNodeButton;
		[SerializeField] private Button _saveButton;

		[Header( "Components" )]
		[SerializeField] private View.Node _nodePrefab;

		private Dictionary<string, View.Node> _nodesByID;
		private Dictionary<string, NodeMetadata> _nodeMetadataByID;
	}
}
