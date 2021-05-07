using Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

using GraphDatabase = Graph.Database;

namespace Knowledge {

	public class Graph {

		// data types
		public enum DataAction {
			Created,
			Updated,
			Deleted
		}
		public struct ResourceEventData {
			public string UID;
			public DataAction Action;
			public ResourceEventData ( string uid, DataAction action ) {
				UID = uid;
				Action = action;
			}
		}


		// events
		public UnityEvent<ResourceEventData> OnNodeEvent = new UnityEvent<ResourceEventData>();
		public UnityEvent<ResourceEventData> OnLinkEvent = new UnityEvent<ResourceEventData>();
		public UnityEvent<ResourceEventData> OnRelationTypeEvent = new UnityEvent<ResourceEventData>();


		// properties
		[JsonIgnore] public string UID => uid;


		// Initialization
		public Graph () {

			UnityEngine.Debug.Log( "First Init" );

			graph = new GraphDatabase();
			titleRelationTypeUID = graph.CreateRelationType( name: "Title" );
			bodyRelationTypeUID = graph.CreateRelationType( name: "Description" );

			SubscribeToEvents( graph );
		}
		[JsonConstructor]
		public Graph ( GraphDatabase graph, string titleRelationTypeUID, string bodyRelationTypeUID ) {

			UnityEngine.Debug.Log( "Json Init" );

			this.graph = graph;
			this.titleRelationTypeUID = titleRelationTypeUID;
			this.bodyRelationTypeUID = bodyRelationTypeUID;

			SubscribeToEvents( this.graph );
		}
		public void Initialize ( string uid ) {

			this.uid = uid;
		}

		// Concepts
		public string NewConcept ( string title = null ) {

			// ensure no null titles make it in
			if ( title == null ) {
				title = GetEmptyTitle();
			}

			var rootUID = graph.CreateNode();
			var titleUID = graph.CreateNode( title );
			var descriptionUID = graph.CreateNode( "" );
			graph.CreateLink( rootUID, titleRelationTypeUID, titleUID );
			graph.CreateLink( rootUID, bodyRelationTypeUID, descriptionUID );

			return rootUID;
		}
		public string GetConceptTitle ( string conceptUID ) {

			var results = QueryFromConcept( conceptUID )
				.FilterNeighbors( titleRelationTypeUID )
				.ResultsOfType<string>();
			if ( results.Count > 0 ) {
				return results.First();
			} else {
				return "{ERROR}";
			}
		}
		public string GetConceptBody ( string conceptUID ) {

			var results = QueryFromConcept( conceptUID )
				.FilterNeighbors( bodyRelationTypeUID )
				.ResultsOfType<string>();
			if ( results.Count > 0 ) {
				return results.First();
			} else {
				return "{ERROR}";
			}
		}
		public void SetConceptTitle ( string conceptUID, string title ) {

			var node = graph.GetNode( conceptUID );
			var titleLink = graph.GetLinks( node.LinkUIDs ).Filter( link => link.TypeUID == titleRelationTypeUID ).First();
			var titleNode = graph.GetNode( titleLink.ToUID ) as Node<string>;
			var oldTitle = titleNode.Value;
			if ( oldTitle == title ) { return; }
			graph.UpdateNodeValue( conceptUID, title );
		}
		public void SetConceptBody ( string conceptUID, string body ) {

			var node = graph.GetNode( conceptUID );
			var bodyLink = graph.GetLinks( node.LinkUIDs ).Filter( link => link.TypeUID == bodyRelationTypeUID ).First();
			var bodyNode = graph.GetNode( bodyLink.ToUID ) as Node<string>;
			graph.UpdateNodeValue( conceptUID, body );
		}

		// Links
		public Link GetLink ( string linkUID ) {

			var link = graph.GetLink( linkUID );
			var link2 = link;
			var equal = ( link as IEquatable<Link> ).Equals( link2 );
			return graph.GetLink( linkUID );
		}
		public List<Link> GetLinksFromConcept ( string conceptUID ) {

			var node = graph.GetNode( conceptUID );
			var links = graph
				.GetLinks( node.LinkUIDs )
				.Filter( link => ( link.TypeUID != titleRelationTypeUID && link.TypeUID != bodyRelationTypeUID ) );

			return links;
		}
		public List<Link> GetBacklinksFromConcept ( string conceptUID ) {

			var node = graph.GetNode( conceptUID );
			var links = graph.GetLinks( node.BacklinkUIDs );

			return links;
		}
		public string CreateLink ( string fromConceptUID, string toConceptUID ) {

			var linkUID = graph.CreateLink( fromNodeUID: fromConceptUID, relationTypeUID: null, toNodeUID: toConceptUID );
			return linkUID;
		}
		public void UpdateLinkType ( string linkUID, string relationTypeUID ) {

			graph.UpdateLinkType( linkUID, relationTypeUID );
		}
		public void DeleteLink ( string linkUID ) {

			graph.DeleteLink( linkUID );
		}

		// Relation Types
		[JsonIgnore] public string TitleRelationUID => titleRelationTypeUID;
		[JsonIgnore] public string BodyRelationUID => bodyRelationTypeUID;
		[JsonIgnore] public IReadOnlyDictionary<string, RelationType> AllRelationTypes => graph.GetAllRelationTypes();
		public RelationType GetRelationType ( string relationTypeUID ) {

			return graph.GetRelationType( relationTypeUID );
		}
		public void NewRelationType ( string name ) {

			graph.CreateRelationType( name );
		}
		public void UpdateRelationTypeName ( string relationTypeUID, string name ) {

			graph.UpdateRelationTypeName( relationTypeUID, name );
		}
		public void DeleteRelationType ( string relationTypeUID ) {

			graph.DeleteRelationType( relationTypeUID );
		}

		// Queries
		public Query QueryFromConcept ( string conceptUID ) => Query.WithGraph( graph ).FromNode( conceptUID );
		public Query QueryFromRelationType ( string uid ) => Query.WithGraph( graph ).FromRelationType( uid );

		// Validation
		public bool ValidateConceptTitle ( string title ) {
			return !QueryFromRelationType( titleRelationTypeUID ).ResultsOfType<string>().Contains( title );
		}
		public bool ValidateRelationTypeName ( string name ) {
			return !graph.GetAllRelationTypes().Values.Convert( relationType => relationType.Name ).Contains( name );
		}

		// private helpers
		private void SubscribeToEvents ( GraphDatabase graph ) {

			graph.OnNodeCreated += nodeUID => OnNodeEvent?.Invoke( new ResourceEventData( nodeUID, DataAction.Created ) );
			graph.OnNodeUpdated += nodeUID => OnNodeEvent?.Invoke( new ResourceEventData( nodeUID, DataAction.Updated ) );
			graph.OnNodeDeleted += nodeUID => OnNodeEvent?.Invoke( new ResourceEventData( nodeUID, DataAction.Deleted ) );

			graph.OnLinkCreated += linkUID => OnLinkEvent?.Invoke( new ResourceEventData( linkUID, DataAction.Created ) );
			graph.OnLinkUpdated += linkUID => OnLinkEvent?.Invoke( new ResourceEventData( linkUID, DataAction.Updated ) );
			graph.OnLinkDeleted += linkUID => OnLinkEvent?.Invoke( new ResourceEventData( linkUID, DataAction.Deleted ) );

			graph.OnRelationTypeCreated += relationTypeUID => OnRelationTypeEvent?.Invoke( new ResourceEventData( relationTypeUID, DataAction.Created ) );
			graph.OnRelationTypeUpdated += relationTypeUID => OnRelationTypeEvent?.Invoke( new ResourceEventData( relationTypeUID, DataAction.Updated ) );
			graph.OnRelationTypeDeleted += relationTypeUID => OnRelationTypeEvent?.Invoke( new ResourceEventData( relationTypeUID, DataAction.Deleted ) );
		}

		private string GetEmptyTitle () {

			return StringHelpers.IncrementedString.Generate(
				baseString: "Untitled",
				validateUniqueness: ValidateConceptTitle
			);
		}

		// internal data
		[JsonProperty] private GraphDatabase graph = null;
		[JsonProperty] private string uid = null;
		[JsonProperty] private string titleRelationTypeUID = null;
		[JsonProperty] private string bodyRelationTypeUID = null;
	}
}