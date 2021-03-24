using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Server {

	public class Example {

		public Example () {

			UnityEngine.Debug.Log( "begin example" );

			var graph = new Graph();

			var titleRelationship = new RelationshipType() {
				UID = "title",
				Name = "Title",
				DataType = EntityDataTypes.String
			};
			var bodyRelationship = new RelationshipType() {
				UID = "body",
				Name = "Body",
				DataType = EntityDataTypes.String
			};
			var ingredientRelationship = new RelationshipType() {
				UID = "ingredient",
				Name = "Ingredient",
				DataType = EntityDataTypes.Node
			};

			graph.RelationshipTypes.Add( titleRelationship.UID, titleRelationship );
			graph.RelationshipTypes.Add( bodyRelationship.UID, bodyRelationship );
			graph.RelationshipTypes.Add( ingredientRelationship.UID, ingredientRelationship );


			var muffinNode = new Entity() {
				UID = "muffin",
				Relationships = new List<Relationship> {
					new Relationship() {
						UID = "muffin-0",
						TypeUID = titleRelationship.UID,
						SubjectUID = "muffin",
						ObjectUID = "muffin-title"
					},
					new Relationship() {
						UID = "muffin-1",
						TypeUID = bodyRelationship.UID,
						SubjectUID = "muffin",
						ObjectUID = "muffin-body"
					},
					new Relationship() {
						UID = "muffin-2",
						TypeUID = ingredientRelationship.UID,
						SubjectUID = "muffin",
						ObjectUID = "flour"
					},
					new Relationship() {
						UID = "muffin-3",
						TypeUID = ingredientRelationship.UID,
						SubjectUID = "muffin",
						ObjectUID = "sugar"
					}
				}
			};
			var muffinTitleNode = new StringEntity() {
				UID = "muffin-title",
				String = "Muffin",
				Relationships = new List<Relationship>()
			};
			var muffinBodyNode = new StringEntity() {
				UID = "muffin-body",
				String = "A muffin is a thing you eat.",
				Relationships = new List<Relationship>()
			};

			graph.Nodes.Add( muffinNode.UID, muffinNode );
			graph.Nodes.Add( muffinTitleNode.UID, muffinTitleNode );
			graph.Nodes.Add( muffinBodyNode.UID, muffinBodyNode );


			var flourNode = new Entity() {
				UID = "flour",
				Relationships = new List<Relationship> {
					new Relationship() {
						UID = "flour-0",
						TypeUID = titleRelationship.UID,
						SubjectUID = "flour",
						ObjectUID = "flour-title"
					},
					new Relationship() {
						UID = "flour-1",
						TypeUID = bodyRelationship.UID,
						SubjectUID = "flour",
						ObjectUID = "flour-body"
					}
				}
			};
			var flourTitleNode = new StringEntity() {
				UID = "flour-title",
				String = "Flour",
				Relationships = new List<Relationship>()
			};
			var flourBodyNode = new StringEntity() {
				UID = "flour-body",
				String = "Flour is made out of grass or something.",
				Relationships = new List<Relationship>()
			};

			graph.Nodes.Add( flourNode.UID, flourNode );
			graph.Nodes.Add( flourTitleNode.UID, flourTitleNode );
			graph.Nodes.Add( flourBodyNode.UID, flourBodyNode );


			var sugarNode = new Entity() {
				UID = "sugar",
				Relationships = new List<Relationship> {
					new Relationship() {
						UID = "sugar-0",
						TypeUID = titleRelationship.UID,
						SubjectUID = "sugar",
						ObjectUID = "sugar-title"
					},
					new Relationship() {
						UID = "sugar-1",
						TypeUID = bodyRelationship.UID,
						SubjectUID = "sugar",
						ObjectUID = "sugar-body"
					}
				}
			};
			var sugarTitleNode = new StringEntity() {
				UID = "sugar-title",
				String = "Sugar",
				Relationships = new List<Relationship>()
			};
			var sugarBodyNode = new StringEntity() {
				UID = "sugar-body",
				String = "Sugar is also made out of grass I think?",
				Relationships = new List<Relationship>()
			};

			graph.Nodes.Add( sugarNode.UID, sugarNode );
			graph.Nodes.Add( sugarTitleNode.UID, sugarTitleNode );
			graph.Nodes.Add( sugarBodyNode.UID, sugarBodyNode );


			// do some queries
			var ingredientTitles = GraphQuery
				.WithGraph( graph )
				.FromNode( muffinNode )
				.FilterNeighbors( ingredientRelationship )
				.FilterNeighbors( titleRelationship )
				.ResultsOfType<StringEntity>();

			// use result
			ingredientTitles.ForEach( ingredientTitle => {
				UnityEngine.Debug.Log( ingredientTitle.String );
			} );


		}
	}

	public class GraphQuery {

		// data
		private Graph Graph;
		private HashSet<Entity> Nodes;

		public GraphQuery ( Graph graph ) {

			Graph = graph;
			Nodes = new HashSet<Entity>();
		}

		public static GraphQuery WithGraph ( Graph graph ) {

			return new GraphQuery( graph );
		}
		public GraphQuery FromNode ( Entity node ) {

			Nodes.Clear();
			Nodes.Add( node );
			return this;
		}


		public GraphQuery FilterNeighbors ( RelationshipType relationshipType )
			=> FilterNeighbors( relationshipType.UID );

		public GraphQuery FilterNeighbors ( string relationshipTypeUID ) {

			UnityEngine.Debug.Log( $"filtering neighbors on {relationshipTypeUID}" );
			var nodeUIDs = new List<string>();
			foreach ( var node in Nodes ) {
				var nodesUIDsWithRelationship = node.Relationships
					.Filter( r => r.TypeUID == relationshipTypeUID )
					.Convert( r => r.ObjectUID );
				nodeUIDs.AddRange( nodesUIDsWithRelationship );
				UnityEngine.Debug.Log( $"found {nodesUIDsWithRelationship.Count} links on {node.UID}" );
			}

			var result = new HashSet<Entity>();
			nodeUIDs.ForEach( UID => result.Add( Graph.Nodes[UID] ) );
			this.Nodes = result;
			UnityEngine.Debug.Log( $"{Nodes.Count} total results after filtering on {relationshipTypeUID}" );

			return this;
		}
		public GraphQuery FilterEntityDataType ( EntityDataTypes type ) {

			foreach ( var node in Nodes ) {
				if ( node.Type != type ) {
					Nodes.Remove( node );
				}
			}

			return this;
		}
		public List<T> ResultsOfType<T> () where T : Entity {

			UnityEngine.Debug.Log( $"filtering results on type {typeof( T ).ToString()}" );
			var expectedType = typeof( T ) switch {
				Type t when t == typeof( StringEntity ) => EntityDataTypes.String,
				Type t when t == typeof( IntegerEntity ) => EntityDataTypes.Integer,
				_ => EntityDataTypes.Node
			};
			UnityEngine.Debug.Log( $"expectedType is {expectedType}" );

			var result = new List<T>();
			foreach ( var node in Nodes ) {
				UnityEngine.Debug.Log( $"node {{{node.UID}}} type is {{{node.Type}}}" );
				if ( node.Type == expectedType ) {
					result.Add( node as T );
				}
			}
			UnityEngine.Debug.Log( $"{result.Count} total results found filtering on type {typeof( T ).ToString()}" );
			return result;
		}
	}


	[Serializable]
	public class Graph {

		// permanent data
		[JsonProperty] public string UID;
		[JsonProperty] public Dictionary<string, Entity> Nodes;
		[JsonProperty] public Dictionary<string, RelationshipType> RelationshipTypes;

		// inferred data
		[JsonIgnore] public Dictionary<string, Relationship> Relationships;
		[JsonIgnore] public Dictionary<string, Relationship> RelationshipsByType;

		public Graph () {

			Nodes = new Dictionary<string, Entity>();
			RelationshipTypes = new Dictionary<string, RelationshipType>();

			Relationships = new Dictionary<string, Relationship>();
			RelationshipsByType = new Dictionary<string, Relationship>();
		}
	}


	// Entities

	[Serializable]
	public class Entity {

		[JsonProperty] public virtual EntityDataTypes Type => EntityDataTypes.Node;
		[JsonProperty] public string UID;
		[JsonProperty] public List<Relationship> Relationships;
	}
	[Serializable]
	public class IntegerEntity : Entity {

		[JsonProperty] public override EntityDataTypes Type => EntityDataTypes.Integer;
		[JsonProperty] public int Integer;
	}
	[Serializable]
	public class StringEntity : Entity {

		[JsonProperty] public override EntityDataTypes Type => EntityDataTypes.String;
		[JsonProperty] public string String;
	}


	[Serializable]
	public class Relationship {

		/*
			Defines a relationship between two resources (via id), and the type
			of relationship it is (via type id)
		*/

		[JsonProperty] public string UID;
		[JsonProperty] public string TypeUID;
		[JsonProperty] public string SubjectUID;
		[JsonProperty] public string ObjectUID;
	}

	[Serializable]
	public class RelationshipType {

		/*
			Defines a relationship type within a graph

			Must define the name of the relationship, as well
			as the data type that the relationship links to.
		*/

		[JsonProperty] public string UID;
		[JsonProperty] public string Name;
		[JsonProperty] public EntityDataTypes DataType;
	}


	public enum EntityDataTypes {
		Integer,
		Float,
		String,
		Relationship,
		Node
	}
	public static class EntityDataTypes_Extensions {
		public static Type AssociatedType ( this EntityDataTypes entityDataType ) {
			return entityDataType switch {
				EntityDataTypes.Integer => typeof( IntegerEntity ),
				EntityDataTypes.String => typeof( StringEntity ),
				_ => typeof( Entity )
			};
		}
	}
}
