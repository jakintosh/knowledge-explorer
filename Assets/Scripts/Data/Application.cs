using UnityEngine;
using UnityEngine.UI;

namespace Model {

	public class Application : MonoBehaviour {

		[Header( "UI Components" )]
		[SerializeField] private Button _newNodeButton;
		[SerializeField] private Button _saveButton;

		[Header( "Components" )]
		[SerializeField] private View.Node _nodePrefab;

		private void Awake () {

			NodeManager.Instance.Load();
			var nodes = NodeManager.Instance.GetAllNodes();
			foreach ( var node in nodes ) {
				var nodeElement = Instantiate<View.Node>( _nodePrefab );
				nodeElement.NodeModel = node;
			}

			_newNodeButton.onClick.AddListener( CreateNewNode );
			_saveButton.onClick.AddListener( Save );
		}

		private void CreateNewNode () {

			var node = NodeManager.Instance.NewNode();
			var nodeElement = Instantiate<View.Node>( _nodePrefab );
			nodeElement.NodeModel = node;
		}
		private void Save () {

			NodeManager.Instance.Save();
		}
	}
}
