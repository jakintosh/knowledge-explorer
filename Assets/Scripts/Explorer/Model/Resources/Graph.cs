using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Explorer.Model {

	public partial class Graph {

		public enum NodeDataTypes {
			Invalid = -1,
			Node = 0,
			Integer = 1,
			String = 2
		}
		public class Query {

			// data
			private Graph _graph;
			private HashSet<string> _nodes;

			protected Query ( Graph graph ) {

				_graph = graph;
				_nodes = new HashSet<string>();
			}

			public static Query WithGraph ( Graph graph ) {

				return new Query( graph );
			}
			public Query FromNode ( string uid ) {

				_nodes.Clear();
				_nodes.Add( uid );
				return this;
			}
			public Query FromRelationshipType ( string relTypeUID, bool inverse = false ) {

				var relUIDs = _graph.GetRelationshipUIDsOfType( relTypeUID );
				var rels = _graph.GetRelationships( relUIDs );
				var nodeUIDs = rels.Convert( rel => inverse ? rel.FromUID : rel.ToUID );

				_nodes.Clear();
				_nodes.UnionWith( nodeUIDs );

				return this;
			}
			public Query Duplicate () {

				var query = new Query( _graph );
				query._nodes = new HashSet<string>( _nodes );
				return query;
			}
			public Query FilterNeighbors ( string relType, bool inverse = false ) {

				var relUIDs = new List<string>();
				_graph.GetNodes( _nodes ).ForEach( node => {
					relUIDs.AddRange( inverse ? node.InverseRelationshipUIDs : node.RelationshipUIDs );
				} );

				var rels = _graph.GetRelationships( relUIDs )
					.Filter( r => r.TypeUID == relType )
					.Convert( r => r.ToUID );

				_nodes.Clear();
				_nodes.UnionWith( rels );

				return this;
			}
			public int ResultCount () {

				return _graph.GetNodes( _nodes ).Count;
			}
			public List<T> ResultsOfType<T> () {

				var result = _graph
					.GetNodes( _nodes )
					.Filter( node => ( node as Node<T> ) != null )
					.Convert( node => ( node as Node<T> ).Value );

				return new List<T>( result );
			}
		}
		public class Node : IdentifiableResource {

			[JsonProperty] public virtual NodeDataTypes Type => NodeDataTypes.Node;
			[JsonProperty] public List<string> RelationshipUIDs { get; private set; }
			[JsonProperty] public List<string> InverseRelationshipUIDs { get; private set; }

			public Node ( string uid ) : base( uid ) {

				RelationshipUIDs = new List<string>();
				InverseRelationshipUIDs = new List<string>();
			}
		}
		public class Node<T> : Node {

			[JsonProperty] public override NodeDataTypes Type => _dataType;
			[JsonProperty] public T Value { get; private set; }

			public Node ( string uid, T value, NodeDataTypes dataType ) : base( uid ) {

				_dataType = dataType;
				Value = value;
			}
			public void updateValue ( T value ) {

				Value = value;
			}

			private NodeDataTypes _dataType;
		}
		public class Relationship : IdentifiableResource {

			[JsonProperty] public string TypeUID { get; set; }
			[JsonProperty] public string FromUID { get; private set; }
			[JsonProperty] public string ToUID { get; private set; }

			public Relationship ( string uid, string typeUID, string fromUID, string toUID ) : base( uid ) {

				TypeUID = typeUID;
				FromUID = fromUID;
				ToUID = toUID;
			}
		}
		public class RelationshipType : IdentifiableResource {

			[JsonProperty] public string Name { get; private set; }
			[JsonProperty] public NodeDataTypes DataType { get; private set; }

			public RelationshipType ( string uid, string name, NodeDataTypes dataType ) : base( uid ) {

				DataType = dataType;
				Name = name;
			}
		}

		// json stuff
		public class NodeConverter : JsonConverter {

			public override bool CanConvert ( Type objectType ) => typeof( Node ).IsAssignableFrom( objectType );

			public override bool CanRead => true;
			public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer ) {

				var jObject = JObject.Load( reader );
				var nodeType = (NodeDataTypes)jObject["Type"].Value<int>();
				var node = nodeType switch {
					NodeDataTypes.String => new Node<string>( "", "", NodeDataTypes.String ),
					NodeDataTypes.Integer => new Node<int>( "", 0, NodeDataTypes.Integer ),
					_ => new Node( "" )
				};
				serializer.Populate( jObject.CreateReader(), node );

				return node;
			}

			public override bool CanWrite => false;
			public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer ) => throw new NotImplementedException();
		}
	}


	[Serializable]
	public partial class Graph {


		// ********** Public Interface **********

		public Graph () {

			nodes = new Dictionary<string, Node>();
			relationships = new Dictionary<string, Relationship>();
			relationshipTypes = new Dictionary<string, RelationshipType>();

			_relationshipUIDsByType = new Dictionary<string, HashSet<string>>();
		}

		public string CreateNode () {

			var uid = GetUID();
			var node = new Node( uid: uid );
			nodes[uid] = node;
			return uid;
		}
		public string CreateNode<T> ( T value ) {

			var type = GetType( value );
			if ( type == NodeDataTypes.Invalid ) {
				UnityEngine.Debug.Log( $"Trying to create node of unsupported type {typeof( T ).ToString()}." );
				return null;
			}
			var uid = GetUID();
			var node = new Node<T>(
				uid: uid,
				value: value,
				dataType: type
			);
			nodes[uid] = node;
			return uid;
		}
		public void DeleteNode ( string uid ) {

			var node = nodes[uid];
			node.RelationshipUIDs.ForEach( relID => {
				var rel = relationships[relID];
				var otherNode = nodes[rel.ToUID];
				otherNode.InverseRelationshipUIDs.Remove( relID );
				relationships.Remove( relID );
			} );
			node.InverseRelationshipUIDs.ForEach( relID => {
				var rel = relationships[relID];
				var otherNode = nodes[rel.FromUID];
				otherNode.RelationshipUIDs.Remove( relID );
				relationships.Remove( relID );
			} );
			nodes.Remove( uid );
		}

		public string CreateRelationship ( string fromUID, string typeUID, string toUID ) {

			var uid = GetUID();
			var rel = new Relationship(
				uid: uid,
				typeUID: typeUID,
				fromUID: fromUID,
				toUID: toUID
			);
			relationships[uid] = rel;

			var from = nodes[fromUID];
			from.RelationshipUIDs.Add( uid );

			var to = nodes[toUID];
			to.InverseRelationshipUIDs.Add( uid );

			return uid;
		}
		public void DeleteRelationship ( string uid ) {

			var rel = relationships[uid];

			var fromNode = nodes[rel.FromUID];
			fromNode.RelationshipUIDs.Remove( uid );

			var toNode = nodes[rel.ToUID];
			toNode.InverseRelationshipUIDs.Remove( uid );

			relationships.Remove( uid );
		}

		public string CreateRelationshipType ( string name, NodeDataTypes dataType ) {

			var uid = GetUID();
			var relType = new RelationshipType(
				uid: uid,
				name: name,
				dataType: dataType
			);
			relationshipTypes[uid] = relType;
			return uid;
		}
		public void DeleteRelationshipType ( string uid ) {

			// TODO: what should happen here
		}

		public string GetUID () {

			return StringHelpers.UID.Generate(
				length: 10,
				validateUniqueness: uid =>
					nodes.KeyIsUnique( uid ) &&
					relationships.KeyIsUnique( uid ) &&
					relationshipTypes.KeyIsUnique( uid ) );
		}
		public NodeDataTypes GetType<T> ( T value = default( T ) ) {

			return typeof( T ) switch {
				Type t when t == typeof( string ) => NodeDataTypes.String,
				Type t when t == typeof( int ) => NodeDataTypes.Integer,
				_ => NodeDataTypes.Invalid
			};
		}

		public Node GetNode ( string uid ) {

			return nodes[uid];
		}
		public List<Node> GetNodes ( IEnumerable<string> uids ) {

			var nodes = new List<Node>();
			foreach ( var uid in uids ) { nodes.Add( GetNode( uid ) ); }
			return nodes;
		}

		public Relationship GetRelationship ( string uid ) {

			return relationships[uid];
		}
		public List<Relationship> GetRelationships ( IEnumerable<string> uids ) {

			var rels = new List<Relationship>();
			foreach ( var uid in uids ) { rels.Add( GetRelationship( uid ) ); }
			return rels;
		}
		public HashSet<string> GetRelationshipUIDsOfType ( string uid ) {

			return _relationshipUIDsByType[uid];
		}

		public RelationshipType GetRelationshipType ( string uid ) {

			return relationshipTypes[uid];
		}
		public List<RelationshipType> GetRelationshipTypes ( IEnumerable<string> uids ) {

			var rels = new List<RelationshipType>();
			foreach ( var uid in uids ) { rels.Add( GetRelationshipType( uid ) ); }
			return rels;
		}
		public IList<RelationshipType> GetAllRelationshipTypes () {

			return new List<RelationshipType>( relationshipTypes.Values ).AsReadOnly();
		}

		// serialized data
		[JsonProperty( ItemConverterType = typeof( NodeConverter ) )] protected Dictionary<string, Node> nodes;
		[JsonProperty] protected Dictionary<string, Relationship> relationships;
		[JsonProperty] protected Dictionary<string, RelationshipType> relationshipTypes;


		// runtime processing
		[JsonIgnore] protected Dictionary<string, HashSet<string>> _relationshipUIDsByType;

		[OnDeserialized]
		private void OnAfterDeserialize ( StreamingContext context ) {

			ProcessAfterDeserialization();
		}
		protected virtual void ProcessAfterDeserialization () {

			TrackRelationshipsByType();
		}
		private void TrackRelationshipsByType () {

			// fill hashset with keys of type UIDs
			relationshipTypes.ForEach( ( uid, _ ) => {
				_relationshipUIDsByType[uid] = new HashSet<string>();
			} );
			foreach ( var relationshipType in relationshipTypes.Keys ) {
				_relationshipUIDsByType[relationshipType] = new HashSet<string>();
			}

			// populate hashset with relationship UIDs
			foreach ( var pair in relationships ) {
				var id = pair.Key;
				var relationship = pair.Value;
				_relationshipUIDsByType[relationship.TypeUID].Add( id );
			}
		}
	}

	[Serializable]
	public class KnowledgeGraph {


		public KnowledgeGraph () {

			_allTitles = new HashSet<string>();
		}

		// this happens once *ever*. we do not want this to happen everytime the program is run
		public void FirstInitialization () {

			graph = new Graph();
			titleRelationshipTypeUID = graph.CreateRelationshipType( name: "Title", dataType: Graph.NodeDataTypes.String );
			descriptionRelationshipTypeUID = graph.CreateRelationshipType( name: "Description", dataType: Graph.NodeDataTypes.String );
		}

		/* 

		Idea for what Node Query Returns

		NODE
		- [Name] [Type] [Value Type] [Value]
		- [Name] [Type] [Value Type] [Value]
		- [Name] [Type] [Value Type] [Value]

		*/

		// Concepts
		[JsonIgnore] public HashSet<string> AllTitles => _allTitles;
		public string NewConcept ( string title = null ) {

			// ensure no null titles make it in
			if ( title == null ) {
				title = GetEmptyTitle();
			}

			var rootUID = graph.CreateNode();
			var titleUID = graph.CreateNode( title );
			var descriptionUID = graph.CreateNode( "" );
			graph.CreateRelationship( rootUID, titleRelationshipTypeUID, titleUID );
			graph.CreateRelationship( rootUID, descriptionRelationshipTypeUID, descriptionUID );

			_allTitles.Add( title );

			return rootUID;
		}
		public string GetTitle ( string uid ) {

			var results = QueryFromNode( uid )
				.FilterNeighbors( titleRelationshipTypeUID )
				.ResultsOfType<string>();
			if ( results.Count > 0 ) {
				return results.First();
			} else {
				return "{ERROR}";
			}
		}
		public void SetTitle ( string uid, string title ) {

			var node = graph.GetNode( uid );
			var titleRels = graph.GetRelationships( node.RelationshipUIDs ).Filter( rel => rel.TypeUID == titleRelationshipTypeUID );
			var titleNode = graph.GetNode( titleRels.First().ToUID ) as Graph.Node<string>;
			var oldTitle = titleNode.Value;
			if ( oldTitle == title ) { return; }
			_allTitles.Remove( oldTitle );
			_allTitles.Add( title );
			titleNode.updateValue( title );
		}
		public void SetDescription ( string uid, string description ) {

			var node = graph.GetNode( uid );
			var descriptionRels = graph.GetRelationships( node.RelationshipUIDs ).Filter( rel => rel.TypeUID == descriptionRelationshipTypeUID );
			var descriptionNode = graph.GetNode( descriptionRels.First().ToUID ) as Graph.Node<string>;
			descriptionNode.updateValue( description );
		}

		// Links
		public struct Link : IEquatable<Link> {

			public string TypeUID;
			public string NodeUID;
			public Link ( string typeUID, string nodeUID ) {
				TypeUID = typeUID;
				NodeUID = nodeUID;
			}

			// *** IEquatable<IdentifiableResource> ***
			public bool Equals ( Link other ) => other.TypeUID.Equals( TypeUID ) && other.NodeUID.Equals( NodeUID );

			// ********** Equality Overrides **********
			public override bool Equals ( object obj ) => obj is Link ? this.Equals( obj ) : false;
			public override int GetHashCode () => ( TypeUID + NodeUID ).GetHashCode();
		}
		public string LinkConcepts ( string from, string to ) {

			var relationshipUID = graph.CreateRelationship( fromUID: from, typeUID: null, toUID: to );
			return relationshipUID;
		}
		public void ChangeLinkType ( string relationshipUID, string typeUID ) {

			var relationship = graph.GetRelationship( relationshipUID );
			relationship.TypeUID = typeUID;
		}
		public List<Link> GetLinksFromConcept ( string uid ) {

			var node = graph.GetNode( uid );
			var rels = graph.GetRelationships( node.RelationshipUIDs );

			var links = rels
				.Filter( rel => ( rel.TypeUID != titleRelationshipTypeUID && rel.TypeUID != descriptionRelationshipTypeUID ) )
				.Convert( rel => new Link( typeUID: rel.TypeUID, nodeUID: rel.ToUID ) );

			return links;
		}

		// Relationship Types
		[JsonIgnore] public string TitleRelationshipUID => titleRelationshipTypeUID;
		[JsonIgnore] public string DescriptionRelationshipUID => descriptionRelationshipTypeUID;
		[JsonIgnore] public IList<Graph.RelationshipType> AllRelationshipTypes => GetAllRelationshipTypes();
		public void NewRelationshipType ( string name ) {

			graph.CreateRelationshipType( name, Graph.NodeDataTypes.Node );
		}
		public void DeleteRelationshipType ( string uid ) {

			graph.DeleteRelationshipType( uid );
		}
		public string GetRelationshipTypeName ( string uid ) {

			return graph.GetRelationshipType( uid ).Name;
		}


		// queries
		public Graph.Query QueryFromNode ( string uid ) => Graph.Query.WithGraph( graph ).FromNode( uid );
		public Graph.Query QueryFromRelationshipType ( string uid ) => Graph.Query.WithGraph( graph ).FromRelationshipType( uid );

		// private helpers
		private IList<Graph.RelationshipType> GetAllRelationshipTypes () {

			return graph.GetAllRelationshipTypes();
		}
		private string GetEmptyTitle () {

			return StringHelpers.IncrementedString.Generate(
				baseString: "Untitled",
				validateUniqueness: candidate => !AllTitles.Contains( candidate )
			);
		}


		// internal data
		[JsonProperty] private Graph graph = null;
		[JsonProperty] private string titleRelationshipTypeUID = null;
		[JsonProperty] private string descriptionRelationshipTypeUID = null;


		// runtime processing
		[JsonIgnore] private HashSet<string> _allTitles;

		[OnDeserialized]
		private void OnAfterDeserialize ( StreamingContext context ) {

			TrackAllTitles();
		}
		protected void TrackAllTitles () {

			var allTitles = QueryFromRelationshipType( titleRelationshipTypeUID ).ResultsOfType<string>();

			_allTitles.Clear();
			_allTitles.UnionWith( allTitles );
		}
	}
}

