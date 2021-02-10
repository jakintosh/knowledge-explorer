using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Model {

	public class NodeManager {

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

			_idsByTitle = new Dictionary<string, string>();
			_nodesByID = new Dictionary<string, Node>();
		}

		// ****************************************

		public void Load () {

			var database = PersistentStore.Load( "/data/buckets/default.bucket" );
			_nodesByID = new Dictionary<string, Node>();
			foreach ( var node in database.Nodes ) {

				// register node
				Debug.Log( $"Loaded Node: {node.ID}" );
				_nodesByID.Add( node.ID, node );
				_idsByTitle.Add( node.Title, node.ID );

				// sub to changes
				node.OnTitleChanged += HandleTitleChange;
			}
		}
		public void Save () {

			var database = new Database();
			database.Nodes = new List<Node>( _nodesByID.Values ).ToArray();
			PersistentStore.Save( "/data/buckets/default.bucket", database );
		}

		// ****************************************

		public Node NewNode ( string title = null ) {

			// create node
			var node = new Node();
			node.Title = string.IsNullOrEmpty( title ) ? GenerateTitle() : title;
			node.ID = GenerateID();
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

		// ****************************************

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
		public List<Node> GetAllNodes () {
			return new List<Node>( _nodesByID.Values );
		}

		// ****************************************

		public static int ID_LENGTH = 8;
		private static char[] ID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
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
		private string GenerateTitle () {

			int index = 0;
			var titleBase = "Untitled";
			var title = titleBase;
			while ( _idsByTitle.ContainsKey( title ) ) {
				title = $"{titleBase}{++index}";
			}
			return title;
		}

		// ****************************************

		private void HandleTitleChange ( string oldTitle, string newTitle ) {

			var id = _idsByTitle[oldTitle];
			_idsByTitle[oldTitle] = null;
			_idsByTitle[newTitle] = id;

			// todo: fire event
		}

		// ****************************************

		private Dictionary<string, string> _idsByTitle;
		private Dictionary<string, Node> _nodesByID;
	}

	public static class PersistentStore {

		private static string FullPath ( string subpath ) => $"{UnityEngine.Application.persistentDataPath}{subpath}";

		public static void Save ( string path, Database database ) {

			var fullpath = FullPath( path );

			var lastSlash = fullpath.LastIndexOf( '/' );
			if ( lastSlash == -1 ) {
				// there are no slashes, invalid
				return;
			}

			// make sure the directory we are saving to exists
			EnsureDirectory( fullpath.Substring( 0, lastSlash ) );

			// write out the actual file
			var json = JsonUtility.ToJson( database );
			File.WriteAllText( fullpath, json );
		}

		public static Database Load ( string path ) {

			var fullpath = FullPath( path );
			if ( !File.Exists( fullpath ) ) {
				return new Database();

			}
			var json = File.ReadAllText( fullpath );
			return JsonUtility.FromJson<Database>( json );
		}

		private static void EnsureDirectory ( string path ) {

			if ( !Directory.Exists( path ) ) {
				var lastSlash = path.LastIndexOf( '/' );
				if ( lastSlash != -1 ) {
					var subPath = path.Substring( 0, lastSlash );
					EnsureDirectory( subPath );
					Debug.Log( $"Creating Directory at: {path}" );
					Directory.CreateDirectory( path );
				} else {
					return;
				}
			}
		}
	}

	[Serializable]
	public class Database {

		[SerializeField] public Node[] Nodes = new Node[0];
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