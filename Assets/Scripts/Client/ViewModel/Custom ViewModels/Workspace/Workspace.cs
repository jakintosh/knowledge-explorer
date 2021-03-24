using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

using ResourceMetadata = Framework.Data.Metadata.Resource;

namespace Client.ViewModel {

	[Serializable]
	public class Node {

		// ********** OUTPUTS **********

		// ********** INPUTS ***********

		// *****************************

		// components
		[SerializeField] public Frame Frame;
		[SerializeField] public Presence Presence;


		// readonly properties
		public string ID;

		// constructor
		public Node ( string id ) {

			this.id = id;
		}

		// serialized data
		[SerializeField] private string id;
	}

	[Serializable]
	public class Link {

		// ********** OUTPUTS **********

		// ********** INPUTS ***********

		// *****************************

		// readonly properties
		public string ID => id;
		public string SourceID => sourceID;
		public string DestinationID => destinationID;

		// constructor
		public Link ( string id, string sourceID, string destinationID ) {

			this.id = id;
			this.sourceID = sourceID;
			this.destinationID = destinationID;
		}

		// serialized data
		[SerializeField] private string id;
		[SerializeField] private string sourceID;
		[SerializeField] private string destinationID;
	}

	[Serializable]
	public class Workspace : IEquatable<Workspace> {

		// ********** OUTPUTS **********

		// ********** INPUTS ***********

		public void CreateNewNode ( string title = null ) {

		}
		public void OpenNode ( string id, string sourceID = null ) {

			if ( nodes.Exists( node => node.ID == id ) ) {
				// show this somehow
				return;
			}

			// track new node
			var openedNode = new Node(
				id: id
			);
			nodes.Add( openedNode );


			if ( !string.IsNullOrEmpty( sourceID ) ) {
				CreateLink( fromID: sourceID, toID: id );
			}
		}
		public void CloseNode ( string id ) {

			// remove node
			nodes.RemoveAll( node => node.ID == id );

			// remove links
			DestroyLinks( withID: id );
		}

		// *****************************


		// *********** Public Interface ***********

		// properties
		[JsonIgnore] public string Name => name;
		[JsonIgnore] public string ID => id;
		[JsonIgnore] public string GraphUID => graphId;

		// constructor
		public Workspace () {

			nodes = new List<Node>();
			links = new List<Link>();
		}

		// set data
		public void SetMetadata ( ResourceMetadata metadata ) {

			name = metadata.Name;
			id = metadata.ID;
		}
		public void SetGraph ( Server.Graph graph ) {

			_graph = graph;
			graphId = _graph.UID;
		}


		// ************ Serialization ************

		// serializable data
		[JsonProperty] private string id;
		[JsonProperty] private string graphId;
		[JsonProperty] private string name;
		[JsonProperty] private List<Node> nodes;
		[JsonProperty] private List<Link> links;


		// ********** Equality Stuff **********

		public bool Equals ( Workspace other ) {
			return other?.ID?.Equals( ID ) ?? false;
		}
		public override bool Equals ( object obj ) {
			if ( obj == null ) { return false; }
			var workspaceObj = obj as Workspace;
			if ( workspaceObj == null ) { return false; }
			return Equals( workspaceObj );
		}
		public override int GetHashCode () {
			return id.GetHashCode();
		}

		// ********** Private Interface **********

		private Server.Graph _graph;

		private void CreateLink ( string fromID, string toID ) {

			var linkID = StringHelpers.UID.Generate( 8, id => !links.Exists( link => link.ID == id ) );
			var newLink = new Link(
				id: linkID,
				sourceID: fromID,
				destinationID: toID
			);
			links.Add( newLink );
		}
		private void DestroyLinks ( string withID ) {

			links.RemoveAll( link => ( link.SourceID == withID || link.DestinationID == withID ) );
		}

	}

}