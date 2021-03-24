using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Library.Model {

	// *************** Metadata ***************

	public partial class Bucket {

		[Serializable]
		public enum Location {
			Local
		}

	}


	// *************** Data ***************

	[Serializable]
	public partial class Bucket {


		// *********** Public Interface ***********

		public Bucket () {

			_idsByTitle = new Dictionary<string, string>();
			_nodesByID = new Dictionary<string, Node>();
		}


		// ************* Serialization *************

		// serialized data
		[JsonProperty] private List<Node> nodes = new List<Node>();

		[OnSerializing]
		void OnBeforeSerialize ( StreamingContext context ) { }

		[OnDeserialized]
		void OnAfterDeserialize ( StreamingContext context ) {

			foreach ( var node in _nodes ) {

				// register node
				Debug.Log( $"Library.Model.Bucket: Loaded Node {{{node.ID}:{node.Title}}}" );
				_nodesByID.Add( node.ID, node );
				_idsByTitle.Add( node.Title, node.ID );

				// sub to changes
				node.OnTitleChanged += HandleTitleChange;
			}
		}


		// *********** Private Interface ***********

		// runtime data
		private List<Node> _nodes => nodes;
		private Dictionary<string, string> _idsByTitle;
		private Dictionary<string, Node> _nodesByID;

	}

	// *************** Methods  ***************

	public partial class Bucket {

		// *********** Public Interface ***********

		// static info
		public static int NODE_ID_LENGTH = 8;

		// node management
		public Node NewNode ( string title = null ) {

			// create node
			var uid = StringHelpers.UID.Generate( length: NODE_ID_LENGTH, validateUniqueness: id => _nodesByID.KeyIsUnique( id ) );
			var node = new Node( id: uid );
			node.Title = !string.IsNullOrEmpty( title ) ? title : StringHelpers.IncrementedString.Generate( baseString: "Unititled", validateUniqueness: title => !_idsByTitle.ContainsKey( title ) );
			node.OnTitleChanged += HandleTitleChange;

			// register with data structures
			_nodesByID[node.ID] = node;
			_idsByTitle[node.Title] = node.ID;

			return node;
		}
		public void DeleteNode ( string id ) {

			var node = _nodesByID[id];
			_idsByTitle[node.Title] = null;
			_nodesByID[id] = null;
			node.OnTitleChanged -= HandleTitleChange;
		}

		// node queries
		public Node GetNodeForID ( string id ) {

			if ( !_nodesByID.ContainsKey( id ) ) {
				Debug.LogWarning( $"Library.Model.Bucket: Couldn't resolve ID {{{id}}}" );
				return null;
			}
			return _nodesByID[id];
		}
		public string GetIDForTitle ( string title ) {

			if ( !_idsByTitle.ContainsKey( title ) ) {
				NewNode( title: title );
			}
			return _idsByTitle[title];
		}
		public string GetTitleForID ( string id ) {

			return GetNodeForID( id )?.Title;
		}


		// *********** Private Interface ***********

		// event handlers
		private void HandleTitleChange ( Node.TitleChangedEventData eventData ) {

			var id = _idsByTitle[eventData.OldTitle];
			_idsByTitle[eventData.OldTitle] = null;
			_idsByTitle[eventData.NewTitle] = id;

			// todo: fire event
		}

	}


	// **************** Search ****************

	public partial class Bucket {

		public struct SearchResult<T> {
			public string Query;
			public int Score;
			public T Result;
		}

		public List<SearchResult<Node>> SearchTitles ( string titleQuery ) {

			var results = new List<SearchResult<Node>>();

			if ( string.IsNullOrEmpty( titleQuery ) ) {
				return results;
			}

			foreach ( var title in _idsByTitle.Keys ) {
				int score = title.IndexOf( titleQuery, StringComparison.CurrentCultureIgnoreCase );
				if ( score >= 0 ) {
					var id = GetIDForTitle( title );
					var node = GetNodeForID( id );
					results.Add( new SearchResult<Node>() {
						Query = titleQuery,
						Score = score,
						Result = node
					} );
				}
			}
			results.Sort( ( a, b ) => a.Score.CompareTo( b.Score ) );
			return results;
		}

	}
}