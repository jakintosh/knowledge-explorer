using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server {

	public class Example {

		public Example () {

			var knowledgeGraph = new KnowledgeGraph();
			knowledgeGraph.FirstInitialization();
			knowledgeGraph.CreateRelationshipType( "Ingredient", Server.Graph.NodeDataTypes.String );

			var muffinNode = knowledgeGraph.NewConcept( title: "Muffin" );
			knowledgeGraph.SetBody( uid: muffinNode, body: "A muffin is a yummy, yummy boy." );

			var graph = new Graph();

			var title = graph.CreateRelationshipType( "title", Server.Graph.NodeDataTypes.String );
			var body = graph.CreateRelationshipType( "body", Server.Graph.NodeDataTypes.String );
			var ingredient = graph.CreateRelationshipType( "ingredient", Server.Graph.NodeDataTypes.Node );

			var flour = graph.CreateNode();
			var flourTitle = graph.CreateNode( "flour" );
			var flourBody = graph.CreateNode( "flour is made out of grass" );
			graph.CreateRelationship( fromUID: flour, typeUID: title, toUID: flourTitle );
			graph.CreateRelationship( fromUID: flour, typeUID: body, toUID: flourBody );

			var sugar = graph.CreateNode();
			var sugarTitle = graph.CreateNode( "sugar" );
			var sugarBody = graph.CreateNode( "sugar is also made out of grass" );
			graph.CreateRelationship( fromUID: sugar, typeUID: title, toUID: sugarTitle );
			graph.CreateRelationship( fromUID: sugar, typeUID: body, toUID: sugarBody );

			var muffin = graph.CreateNode();
			var muffinTitle = graph.CreateNode( "muffin" );
			var muffinBody = graph.CreateNode( "A muffin is something you eat" );
			graph.CreateRelationship( fromUID: muffin, typeUID: title, toUID: muffinTitle );
			graph.CreateRelationship( fromUID: muffin, typeUID: body, toUID: muffinBody );
			graph.CreateRelationship( fromUID: muffin, typeUID: ingredient, toUID: flour );
			graph.CreateRelationship( fromUID: muffin, typeUID: ingredient, toUID: sugar );

			var muffinIngredients = Graph.Query
				.WithGraph( graph )
				.FromNode( muffin )
				.FilterNeighbors( ingredient );

			muffinIngredients.Duplicate()
				.FilterNeighbors( title )
				.ResultsOfType<string>()
				.ForEach( title => UnityEngine.Debug.Log( $"Title: {title}" ) );

			muffinIngredients.Duplicate()
				.FilterNeighbors( body )
				.ResultsOfType<string>()
				.ForEach( body => UnityEngine.Debug.Log( $"Body: {body}" ) );
		}
	}


	public partial class Graph {

		// public types
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

				UnityEngine.Debug.Log( $"{result.Count} total results found filtering on type {typeof( T ).ToString()}" );

				return new List<T>( result );
			}
		}

		// internal types
		protected class Node : IdentifiableResource {

			[JsonProperty] public virtual NodeDataTypes Type => NodeDataTypes.Node;
			[JsonProperty] public List<string> RelationshipUIDs { get; private set; }
			[JsonProperty] public List<string> InverseRelationshipUIDs { get; private set; }

			public Node ( string uid ) : base( uid ) {

				RelationshipUIDs = new List<string>();
				InverseRelationshipUIDs = new List<string>();
			}
		}
		protected class Node<T> : Node {

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
		protected class Relationship : IdentifiableResource {

			[JsonProperty] public string TypeUID { get; private set; }
			[JsonProperty] public string FromUID { get; private set; }
			[JsonProperty] public string ToUID { get; private set; }

			public Relationship ( string uid, string typeUID, string fromUID, string toUID ) : base( uid ) {

				TypeUID = typeUID;
				FromUID = fromUID;
				ToUID = toUID;
			}
		}
		protected class RelationshipType : IdentifiableResource {

			[JsonProperty] public string Name { get; private set; }
			[JsonProperty] public NodeDataTypes DataType { get; private set; }

			public RelationshipType ( string uid, string name, NodeDataTypes dataType ) : base( uid ) {

				DataType = dataType;
				Name = name;
			}
		}
	}

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


		// ********** Internal Interface **********

		protected string GetUID () {

			return StringHelpers.UID.Generate(
				length: 10,
				validateUniqueness: uid =>
					nodes.KeyIsUnique( uid ) &&
					relationships.KeyIsUnique( uid ) &&
					relationshipTypes.KeyIsUnique( uid ) );
		}
		protected NodeDataTypes GetType<T> ( T value = default( T ) ) {

			return typeof( T ) switch {
				Type t when t == typeof( string ) => NodeDataTypes.String,
				Type t when t == typeof( int ) => NodeDataTypes.Integer,
				_ => NodeDataTypes.Invalid
			};
		}
		protected Relationship GetRelationship ( string uid ) {

			return relationships[uid];
		}
		protected List<Relationship> GetRelationships ( IEnumerable<string> uids ) {

			var rels = new List<Relationship>();
			foreach ( var uid in uids ) { rels.Add( GetRelationship( uid ) ); }
			return rels;
		}
		protected HashSet<string> GetRelationshipUIDsOfType ( string uid ) {

			return _relationshipUIDsByType[uid];
		}
		protected Node GetNode ( string uid ) {

			return nodes[uid];
		}
		protected List<Node> GetNodes ( IEnumerable<string> uids ) {

			var nodes = new List<Node>();
			foreach ( var uid in uids ) { nodes.Add( GetNode( uid ) ); }
			return nodes;
		}

		// serialized data
		[JsonProperty] protected Dictionary<string, Node> nodes;
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

	public class KnowledgeGraph : Graph {

		public new class Query : Graph.Query {

			protected Query ( Graph graph ) : base( graph ) { }
		}

		[JsonIgnore] public HashSet<string> AllTitles => _allTitles;

		public KnowledgeGraph () {

			_allTitles = new HashSet<string>();
		}

		public void FirstInitialization () {

			titleRelTypeUID = base.CreateRelationshipType( name: "Title", dataType: NodeDataTypes.String );
			bodyRelTypeUID = base.CreateRelationshipType( name: "Body", dataType: NodeDataTypes.String );
		}
		public string NewConcept ( string title ) {

			// ensure no null titles make it in
			if ( title == null ) {
				title = GetEmptyTitle();
			}

			var rootUID = this.CreateNode();
			var titleUID = this.CreateNode( title );
			var bodyUID = this.CreateNode( "" );
			this.CreateRelationship( rootUID, titleRelTypeUID, titleUID );
			this.CreateRelationship( rootUID, bodyRelTypeUID, bodyUID );

			_allTitles.Add( title );

			return rootUID;
		}
		public void SetBody ( string uid, string body ) {

			var node = this.GetNode( uid );
			var bodyRels = this.GetRelationships( node.RelationshipUIDs ).Filter( rel => rel.TypeUID == bodyRelTypeUID );
			var bodyNode = this.GetNode( bodyRels.First().ToUID ) as Node<string>;
			bodyNode.updateValue( body );
		}


		private string GetEmptyTitle () {

			return StringHelpers.IncrementedString.Generate(
					baseString: "Untitled",
					validateUniqueness: candidate => !AllTitles.Contains( candidate )
				);
		}


		// internal data

		// built-in relationships
		[JsonProperty] private string titleRelTypeUID;
		[JsonProperty] private string bodyRelTypeUID;


		// runtime data
		[JsonIgnore] private HashSet<string> _allTitles;

		protected override void ProcessAfterDeserialization () {

			base.ProcessAfterDeserialization();

			var allTitles = Query.WithGraph( this )
				.FromRelationshipType( titleRelTypeUID )
				.ResultsOfType<string>();

			_allTitles.Clear();
			_allTitles.UnionWith( allTitles );
		}
	}
}