using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Model {

	public class NodeManager {

		/*
k
			Load a Node
			Create a Node
			Resolve a link
			Have a link update

			Link should use ID: always point to specific object
			Link should recieve updates as live events

		*/

		// ********** singleton management **********

		private static NodeManager _instance;
		public static NodeManager Instance {
			get {
				if ( _instance == null ) {
					_instance = new NodeManager();
				}
				return _instance;
			}
		}
		private NodeManager () {

			// _linksByID = new Dictionary<string, Link>();
			_idsByTitle = new Dictionary<string, string>();
			_nodesByID = new Dictionary<string, Node>();
		}

		// ****************************************

		public void Load () {

			var database = PersistentStore.Load( "/data/default.bucket" );
			_nodesByID = new Dictionary<string, Node>();
			foreach ( var node in database.Nodes ) {

				// register node
				_nodesByID.Add( node.ID, node );
				_idsByTitle.Add( node.Title, node.ID );

				// register link
				// var link = new Link();
				// link.Title = node.Title;
				// link.ID = node.ID;
				// _linksByID[node.ID] = link;

				// sub to changes
				node.OnTitleChanged += HandleTitleChange;
			}
		}
		public void Save () {

			var database = new Database();
			database.Nodes = new List<Node>( _nodesByID.Values ).ToArray();
			PersistentStore.Save( "/data/default.bucket", database );
		}

		public Node NewNode ( string title = null ) {

			// create node
			var node = new Node();
			node.Title = title;
			node.ID = GenerateID();
			node.OnTitleChanged += HandleTitleChange;

			// create link
			// var link = new Link();
			// link.ID = node.ID;
			// link.Title = title;

			// register with data structures
			_nodesByID[node.ID] = node;
			_idsByTitle[node.Title] = node.ID;
			// _linksByID[node.ID] = link;

			return node;
		}
		public void DeleteNode ( string id ) {

			var node = _nodesByID[id];
			node.OnTitleChanged -= HandleTitleChange;
			_idsByTitle[node.Title] = null;
			_nodesByID[id] = null;
		}

		// public Link GetLinkForTitle ( string title ) {

		// 	var id = GetIDForTitle( title );
		// 	if ( !_linksByID.ContainsKey( id ) ) {
		// 		var link = new Link();
		// 		link.ID = id;
		// 		link.Title = title;
		// 		_linksByID[id] = link;
		// 	}
		// 	return _linksByID[id];
		// }
		public string GetIDForTitle ( string title ) {

			if ( !_idsByTitle.ContainsKey( title ) ) {
				NewNode( title: title );
			}
			return _idsByTitle[title];
		}
		public string GetTitleForID ( string id ) {

			if ( _nodesByID.ContainsKey( id ) ) {
				return _nodesByID[id].Title;
			} else {
				return null;
			}
		}

		public static int ID_LENGTH = 8;
		static char[] ID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
		private string GenerateID () {

			var random = new System.Random( (int)( Time.time * 1000 ) );
			var sb = new StringBuilder( capacity: ID_LENGTH );
			string id;
			do {
				sb.Clear();
				for ( int i = 0; i < ID_LENGTH; i++ ) {
					var c = ID_CHARS[random.Next( ID_CHARS.Length )];
					sb.Append( c );
				}
				id = sb.ToString();
			} while ( _nodesByID.ContainsKey( id ) );
			return id;
		}

		private void HandleTitleChange ( string oldTitle, string newTitle ) {

			var id = _idsByTitle[oldTitle];
			_idsByTitle[oldTitle] = null;
			_idsByTitle[newTitle] = id;

			// todo: fire event
		}

		// private Dictionary<string, Link> _linksByID;
		private Dictionary<string, string> _idsByTitle;
		private Dictionary<string, Node> _nodesByID;
	}

	public static class PersistentStore {

		public static void Save ( string path, Database database ) {

			var json = JsonUtility.ToJson( database );
			File.WriteAllText( Application.persistentDataPath + path, json );
		}

		public static Database Load ( string path ) {

			if ( !File.Exists( path ) ) { return null; }
			var json = File.ReadAllText( Application.persistentDataPath + path );
			return JsonUtility.FromJson<Database>( json );
		}
	}

	[Serializable]
	public class Database {

		[SerializeField] public Node[] Nodes;
	}

	[Serializable]
	public class Node {

		// events
		public delegate void TitleChangedEvent ( string oldTitle, string newTitle );
		public event TitleChangedEvent OnTitleChanged;

		// properties
		public string ID {
			get => _id;
			set => _id = value;
		}
		public string Title {
			get => _title;
			set {
				if ( _title != value ) {
					var oldTitle = _title;
					_title = value;
					OnTitleChanged?.Invoke( oldTitle: oldTitle, newTitle: _title );
				}
			}
		}
		public string Body {
			get => _body;
			set => _body = value;
		}

		// serialized backing data
		[SerializeField] private string _id;
		[SerializeField] private string _title;
		[SerializeField] private string _body;
	}

}