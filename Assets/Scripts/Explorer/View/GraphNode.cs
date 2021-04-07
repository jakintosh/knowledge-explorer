using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Graph = Explorer.Model.KnowledgeGraph;

namespace Explorer.View {

	public class GraphNode : MonoBehaviour {

		private Graph _graph;
		private string _nodeUID;

		public void SetGraphNode ( Graph graph, string nodeUID ) {

			_graph = graph;
			_nodeUID = nodeUID;
		}
	}

}