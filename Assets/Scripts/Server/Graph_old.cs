using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Server {

	// public class Example {

	// 	public Example () {

	// 		UnityEngine.Debug.Log( "begin example" );

	// 		var graph = new Graph();

	// 		var titleRelationship = new RelationshipType() {
	// 			UID = "title",
	// 			Name = "Title",
	// 			DataType = NodeDataTypes.String
	// 		};
	// 		var bodyRelationship = new RelationshipType() {
	// 			UID = "body",
	// 			Name = "Body",
	// 			DataType = NodeDataTypes.String
	// 		};
	// 		var ingredientRelationship = new RelationshipType() {
	// 			UID = "ingredient",
	// 			Name = "Ingredient",
	// 			DataType = NodeDataTypes.Node
	// 		};

	// 		graph.AddUserDefinedRelationshipType( titleRelationship );
	// 		graph.AddUserDefinedRelationshipType( bodyRelationship );
	// 		graph.AddUserDefinedRelationshipType( ingredientRelationship );


	// 		var muffinNode = new Node() {
	// 			UID = "muffin",
	// 			Relationships = new List<Relationship> {
	// 				new Relationship(
	// 					uid: "muffin-0",
	// 					subjectUID: "muffin",
	// 					objectUID: "muffin-title",
	// 					typeUID: titleRelationship.UID ),
	// 				new Relationship(
	// 					uid: "muffin-1",
	// 					subjectUID: "muffin",
	// 					objectUID: "muffin-body",
	// 					typeUID: bodyRelationship.UID ),
	// 				new Relationship(
	// 					uid: "muffin-2",
	// 					subjectUID: "muffin",
	// 					objectUID: "flour",
	// 					typeUID: ingredientRelationship.UID ),
	// 				new Relationship(
	// 					uid: "muffin-3",
	// 					subjectUID: "muffin",
	// 					objectUID: "sugar",
	// 					typeUID: ingredientRelationship.UID ),
	// 			}
	// 		};
	// 		var muffinTitleNode = new StringNode() {
	// 			UID = "muffin-title",
	// 			String = "Muffin",
	// 			Relationships = new List<Relationship>()
	// 		};
	// 		var muffinBodyNode = new StringNode() {
	// 			UID = "muffin-body",
	// 			String = "A muffin is a thing you eat.",
	// 			Relationships = new List<Relationship>()
	// 		};

	// 		graph.Nodes.Add( muffinNode.UID, muffinNode );
	// 		graph.Nodes.Add( muffinTitleNode.UID, muffinTitleNode );
	// 		graph.Nodes.Add( muffinBodyNode.UID, muffinBodyNode );


	// 		var flourNode = new Node() {
	// 			UID = "flour",
	// 			Relationships = new List<Relationship> {
	// 				new Relationship(
	// 					uid: "flour-0",
	// 					subjectUID: "flour",
	// 					objectUID: "flour-title",
	// 					typeUID: titleRelationship.UID ),
	// 				new Relationship(
	// 					uid: "flour-1",
	// 					subjectUID: "flour",
	// 					objectUID: "flour-body",
	// 					typeUID: bodyRelationship.UID ),
	// 			}
	// 		};
	// 		var flourTitleNode = new StringNode() {
	// 			UID = "flour-title",
	// 			String = "Flour",
	// 			Relationships = new List<Relationship>()
	// 		};
	// 		var flourBodyNode = new StringNode() {
	// 			UID = "flour-body",
	// 			String = "Flour is made out of grass or something.",
	// 			Relationships = new List<Relationship>()
	// 		};

	// 		graph.Nodes.Add( flourNode.UID, flourNode );
	// 		graph.Nodes.Add( flourTitleNode.UID, flourTitleNode );
	// 		graph.Nodes.Add( flourBodyNode.UID, flourBodyNode );


	// 		var sugarNode = new Node() {
	// 			UID = "sugar",
	// 			Relationships = new List<Relationship> {
	// 				new Relationship(
	// 					uid: "sugar-0",
	// 					subjectUID: "sugar",
	// 					objectUID: "sugar-title",
	// 					typeUID: titleRelationship.UID ),
	// 				new Relationship(
	// 					uid: "sugar-1",
	// 					subjectUID: "sugar",
	// 					objectUID: "sugar-body",
	// 					typeUID: bodyRelationship.UID ),
	// 			}
	// 		};
	// 		var sugarTitleNode = new StringNode() {
	// 			UID = "sugar-title",
	// 			String = "Sugar",
	// 			Relationships = new List<Relationship>()
	// 		};
	// 		var sugarBodyNode = new StringNode() {
	// 			UID = "sugar-body",
	// 			String = "Sugar is also made out of grass I think?",
	// 			Relationships = new List<Relationship>()
	// 		};

	// 		graph.Nodes.Add( sugarNode.UID, sugarNode );
	// 		graph.Nodes.Add( sugarTitleNode.UID, sugarTitleNode );
	// 		graph.Nodes.Add( sugarBodyNode.UID, sugarBodyNode );


	// 		// do some queries
	// 		var ingredientTitles = GraphQuery
	// 			.WithGraph( graph )
	// 			.FromNode( muffinNode )
	// 			.FilterNeighbors( ingredientRelationship )
	// 			.FilterNeighbors( titleRelationship )
	// 			.ResultsOfType<StringNode>();

	// 		// use result
	// 		ingredientTitles.ForEach( ingredientTitle => {
	// 			UnityEngine.Debug.Log( ingredientTitle.String );
	// 		} );


	// 	}
	// }

	// public class GraphQuery {

	// 	// data
	// 	private Graph Graph;
	// 	private HashSet<Node> Nodes;

	// 	private GraphQuery ( Graph graph ) {

	// 		Graph = graph;
	// 		Nodes = new HashSet<Node>();
	// 	}

	// 	public static GraphQuery WithGraph ( Graph graph ) {

	// 		return new GraphQuery( graph );
	// 	}
	// 	public GraphQuery FromNode ( Node node ) {

	// 		Nodes.Clear();
	// 		Nodes.Add( node );
	// 		return this;
	// 	}
	// 	public GraphQuery FilterNeighbors ( RelationshipType relationshipType )
	// 		=> FilterNeighbors( relationshipType.UID );
	// 	public GraphQuery FilterNeighbors ( string relationshipTypeUID ) {

	// 		UnityEngine.Debug.Log( $"filtering neighbors on {relationshipTypeUID}" );
	// 		var nodeUIDs = new List<string>();
	// 		foreach ( var node in Nodes ) {
	// 			var nodesUIDsWithRelationship = node.Relationships
	// 				.Filter( r => r.TypeUID == relationshipTypeUID )
	// 				.Convert( r => r.ObjectUID );
	// 			nodeUIDs.AddRange( nodesUIDsWithRelationship );
	// 			UnityEngine.Debug.Log( $"found {nodesUIDsWithRelationship.Count} links on {node.UID}" );
	// 		}

	// 		var result = new HashSet<Node>();
	// 		nodeUIDs.ForEach( uid => result.Add( Graph.GetNode( uid ) ) );
	// 		this.Nodes = result;
	// 		UnityEngine.Debug.Log( $"{Nodes.Count} total results after filtering on {relationshipTypeUID}" );

	// 		return this;
	// 	}
	// 	public GraphQuery FilterNodeDataType ( NodeDataTypes type ) {

	// 		foreach ( var node in Nodes ) {
	// 			if ( node.Type != type ) {
	// 				Nodes.Remove( node );
	// 			}
	// 		}

	// 		return this;
	// 	}
	// 	public List<T> ResultsOfType<T> () where T : Node {

	// 		UnityEngine.Debug.Log( $"filtering results on type {typeof( T ).ToString()}" );
	// 		var expectedType = typeof( T ) switch {
	// 			Type t when t == typeof( StringNode ) => NodeDataTypes.String,
	// 			Type t when t == typeof( IntegerNode ) => NodeDataTypes.Integer,
	// 			_ => NodeDataTypes.Node
	// 		};
	// 		UnityEngine.Debug.Log( $"expectedType is {expectedType}" );

	// 		var result = new List<T>();
	// 		foreach ( var node in Nodes ) {
	// 			UnityEngine.Debug.Log( $"node {{{node.UID}}} type is {{{node.Type}}}" );
	// 			if ( node.Type == expectedType ) {
	// 				result.Add( node as T );
	// 			}
	// 		}
	// 		UnityEngine.Debug.Log( $"{result.Count} total results found filtering on type {typeof( T ).ToString()}" );
	// 		return result;
	// 	}
	// }


	// public class Graph2 {

	// 	/*

	// 		A node is just data with an address
	// 		A relationship is just two adresses connected with a type

	// 		[ A, { Type, B } ]
	// 		[ B, { Type-1, A } ]

	// 		Delete ( A )


	// 	*/

	// 	public Dictionary<string, Node2> Nodes;
	// 	public Dictionary<string, List<Relationship2>> Relationships;
	// 	public Dictionary<string, List<Relationship2>> InverseRelationships;

	// 	public HashSet<string> AllUIDs;

	// 	public string CreateNode () {

	// 		var uid = GenerateUID();
	// 		var node = new Node2();
	// 		Nodes.Add( uid, node );
	// 		AllUIDs.Add( uid );
	// 		return uid;
	// 	}
	// 	public void DeleteNode ( string uid ) {

	// 		if ( Nodes.ContainsKey( uid ) ) {
	// 			Nodes.Remove( uid );
	// 		}

	// 		// if this node has relationships
	// 		if ( Relationships.TryGetValue( uid, out var relationships ) ) {
	// 			relationships.ForEach( rel => {

	// 				// delete back links
	// 				if ( Relationships.TryGetValue( rel.ObjectUID, out var relationships2 ) ) {

	// 				}

	// 			} );
	// 		}
	// 	}
	// 	public void RelateNodes ( string fromUID, string toUID, string relationshipTypeUID ) {

	// 		var fromRels = GetRelationships( fromUID );
	// 		var toRels = GetRelationships( toUID );

	// 		var relationship = new Relationship2() {
	// 			SubjectUID = fromUID,
	// 			ObjectUID = toUID,
	// 			TypeUID = relationshipTypeUID
	// 		};

	// 		fromRels.Add( relationship );
	// 		toRels.Add( relationship );
	// 	}

	// 	protected List<Relationship2> GetRelationships ( string uid ) {

	// 		if ( !Relationships.TryGetValue( uid, out var relationships ) ) {
	// 			relationships = new List<Relationship2>();
	// 			Relationships[uid] = relationships;
	// 		}
	// 		return relationships;
	// 	}

	// 	protected string GenerateUID ()
	// 		=> StringHelpers.UID.Generate( length: 10, candidate => !AllUIDs.Contains( candidate ) );
	// }
	// public class Node2 { }
	// public class Node2<T> : Node2 {
	// 	public T Value;
	// }
	// public struct Relationship2 {
	// 	public string SubjectUID;
	// 	public string TypeUID;
	// 	public string ObjectUID;
	// }

	// [Serializable]
	// public class Graph {

	// 	// permanent data
	// 	[JsonProperty] public string UID;
	// 	[JsonProperty] protected Dictionary<string, Node> Nodes;
	// 	[JsonProperty] protected Dictionary<string, RelationshipType> RelationshipTypes;

	// 	// inferred data
	// 	[JsonIgnore] protected Dictionary<string, Relationship> Relationships;
	// 	[JsonIgnore] protected Dictionary<string, Relationship> RelationshipsByType;
	// 	[JsonIgnore] protected HashSet<string> AllUIDs;

	// 	public Graph () {

	// 		Nodes = new Dictionary<string, Node>();
	// 		RelationshipTypes = new Dictionary<string, RelationshipType>();

	// 		Relationships = new Dictionary<string, Relationship>();
	// 		RelationshipsByType = new Dictionary<string, Relationship>();
	// 		AllUIDs = new HashSet<string>();
	// 	}


	// 	// public access functions
	// 	public Node GetNode ( string uid )
	// 		=> Nodes[uid];
	// 	public TNode GetNode<TNode> ( string uid ) where TNode : Node
	// 		=> Nodes[uid] as TNode;
	// 	public List<RelationshipType> GetAllRelationshipTypes ()
	// 		=> new List<RelationshipType>( RelationshipTypes.Values );

	// 	// public modifying functions
	// 	public void AddUserDefinedRelationshipType ( RelationshipType relationshipType )
	// 		=> RegisterRelationshipType( relationshipType );
	// 	public void RemoveUserDefinedRelationshipType ( string uid )
	// 		=> UnregisterRelationshipType( uid );

	// 	public string CreateNode ()
	// 		=> RegisterNode( new Node( GenerateUID() ) );
	// 	public string CreateIntegerNode ( int value )
	// 		=> RegisterNode( new IntegerNode( GenerateUID(), value ) );
	// 	public string CreateStringNode ( string value )
	// 		=> RegisterNode( new StringNode( GenerateUID(), value ) );
	// 	public void DeleteNode ( string uid )
	// 		=> UnregisterNode( uid );

	// 	public void LinkNode ( string uid, string toUID, string relationshipTypeUID )
	// 		=> LinkNode( Nodes[uid], Nodes[toUID], relationshipTypeUID );

	// 	public void LinkNode ( Node root, Node dest, string relationshipUID ) {

	// 		var relationshipType = RelationshipTypes[relationshipUID];

	// 		if ( root == null || dest == null ) {
	// 			// fail
	// 			return;
	// 		}

	// 		var linkUID = GenerateUID();
	// 		var relationship = root.RelateTo( destination: dest, type: relationshipType, uid: linkUID );
	// 		RegisterRelationship( relationship );
	// 	}

	// 	// internal modifying functions
	// 	protected void RegisterRelationshipType ( RelationshipType relationshipType ) {

	// 		var uid = relationshipType.UID;
	// 		if ( RelationshipTypes.KeyIsUnique( uid ) ) {

	// 			RelationshipTypes[uid] = relationshipType;
	// 			AllUIDs.Add( uid );

	// 		} else {
	// 			// ERROR, already defined for key
	// 			return;
	// 		}
	// 	}
	// 	protected void UnregisterRelationshipType ( string uid ) {

	// 		if ( RelationshipTypes.ContainsKey( uid ) ) {

	// 			RelationshipTypes.Remove( uid );
	// 			AllUIDs.Remove( uid );

	// 		} else {
	// 			// ERROR, key not defined
	// 			return;
	// 		}
	// 	}

	// 	protected string RegisterNode ( Node node ) {

	// 		Nodes[node.UID] = node;
	// 		AllUIDs.Add( node.UID );
	// 		return node.UID;
	// 	}
	// 	protected bool UnregisterNode ( string uid ) {

	// 		if ( Nodes.ContainsKey( uid ) ) {

	// 			var node = Nodes[uid];
	// 			node.Relationships.ForEach( relationship => {
	// 				UnregisterRelationship( relationship.UID );
	// 			} );

	// 			Nodes.Remove( uid );
	// 			AllUIDs.Remove( uid );
	// 			return true;
	// 		} else {
	// 			return false; // not real
	// 		}
	// 	}

	// 	protected void RegisterRelationship ( Relationship relationship ) {

	// 		Relationships.Add( relationship.UID, relationship );
	// 		AllUIDs.Add( relationship.UID );
	// 	}
	// 	protected void UnregisterRelationship ( string uid ) {

	// 	}

	// 	protected string GenerateUID ()
	// 		=> StringHelpers.UID.Generate( length: 10, candidate => !AllUIDs.Contains( candidate ) );
	// }




	// // Nodes

	// [Serializable]
	// public class Node {

	// 	[JsonProperty] public virtual NodeDataTypes Type => NodeDataTypes.Node;
	// 	[JsonProperty] public string UID;
	// 	[JsonProperty] public List<Relationship> Relationships;

	// 	public Node ( string uid ) {

	// 		UID = uid;
	// 		Relationships = new List<Relationship>();
	// 	}

	// 	public Relationship RelateTo ( Node destination, RelationshipType type, string uid ) {

	// 		var relationship = Relationship.From( this, to: destination, ofType: type, withUID: uid );
	// 		Relationships.Add( relationship );
	// 		return relationship;
	// 	}
	// }
	// [Serializable]
	// public class IntegerNode : Node {

	// 	[JsonProperty] public override NodeDataTypes Type => NodeDataTypes.Integer;
	// 	[JsonProperty] public int Integer;

	// 	public IntegerNode ( string uid, int value ) : base( uid ) {

	// 		Integer = value;
	// 	}
	// }
	// [Serializable]
	// public class StringNode : Node {

	// 	[JsonProperty] public override NodeDataTypes Type => NodeDataTypes.String;
	// 	[JsonProperty] public string String;

	// 	public StringNode ( string uid, string value ) : base( uid ) {

	// 		String = value;
	// 	}
	// }


	// [Serializable]
	// public class Relationship {

	// 	/*
	// 		Defines a relationship between two resources (via id), and the type
	// 		of relationship it is (via type id)
	// 	*/

	// 	[JsonProperty] public string UID;
	// 	[JsonProperty] public string TypeUID;
	// 	[JsonProperty] public string SubjectUID;
	// 	[JsonProperty] public string ObjectUID;

	// 	public Relationship ( string uid, string subjectUID, string objectUID, string typeUID ) {

	// 		UID = uid;
	// 		TypeUID = typeUID;
	// 		SubjectUID = subjectUID;
	// 		ObjectUID = objectUID;
	// 	}

	// 	public static Relationship From ( Node node, Node to, RelationshipType ofType, string withUID )
	// 		=> new Relationship(
	// 			uid: withUID,
	// 			subjectUID: node.UID,
	// 			objectUID: to.UID,
	// 			typeUID: ofType.UID );
	// }

	// [Serializable]
	// public struct RelationshipType {

	// 	/*
	// 		Defines a relationship type within a graph

	// 		Must define the name of the relationship, as well
	// 		as the data type that the relationship links to.
	// 	*/

	// 	[JsonProperty] public string UID;
	// 	[JsonProperty] public string Name;
	// 	[JsonProperty] public NodeDataTypes DataType;

	// 	public RelationshipType ( string uid, string name, NodeDataTypes type ) {

	// 		this.UID = uid;
	// 		this.Name = name;
	// 		this.DataType = type;
	// 	}
	// }


	// public enum NodeDataTypes {
	// 	Integer,
	// 	Float,
	// 	String,
	// 	Relationship,
	// 	Node,
	// 	Invalid
	// }
	// public static class NodeDataTypes_Extensions {
	// 	public static Type AssociatedType ( this NodeDataTypes NodeDataType ) {
	// 		return NodeDataType switch {
	// 			NodeDataTypes.Integer => typeof( IntegerNode ),
	// 			NodeDataTypes.String => typeof( StringNode ),
	// 			_ => typeof( Node )
	// 		};
	// 	}
	// }
}
