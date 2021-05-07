using Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Graph {

	public enum NodeDataTypes {
		Invalid = -1,
		Node = 0,
		Integer = 1,
		String = 2
	}

	public class Database {

		// ********** Public Interface **********

		public event Event<string>.Signature OnNodeCreated;
		public event Event<string>.Signature OnNodeUpdated;
		public event Event<string>.Signature OnNodeDeleted;

		public event Event<string>.Signature OnLinkCreated;
		public event Event<string>.Signature OnLinkUpdated;
		public event Event<string>.Signature OnLinkDeleted;

		public event Event<string>.Signature OnRelationTypeCreated;
		public event Event<string>.Signature OnRelationTypeUpdated;
		public event Event<string>.Signature OnRelationTypeDeleted;

		public Database () {

			nodes = new Dictionary<string, Node>();
			links = new Dictionary<string, Link>();
			relationTypes = new Dictionary<string, RelationType>();

			_linkUIDsByType = new Dictionary<string, HashSet<string>>();
		}

		// nodes
		public Node GetNode ( string uid )
			=> nodes[uid];
		public List<Node> GetNodes ( IEnumerable<string> uids )
			=> uids.Convert( uid => GetNode( uid ) );
		public string CreateNode () {

			var uid = NewUID();
			var node = new Node( uid: uid );
			nodes[uid] = node;

			Event<string>.Fire(
				@event: OnNodeCreated,
				value: uid,
				id: $"Framework.Graph.Graph.OnNodeCreated(uid: {uid})"
			);

			return uid;
		}
		public string CreateNode<T> ( T value ) {

			var type = GetType( value );
			if ( type == NodeDataTypes.Invalid ) {
				UnityEngine.Debug.Log( $"Trying to create node of unsupported type {typeof( T ).ToString()}." );
				return null;
			}
			var uid = NewUID();
			var node = new Node<T>(
				uid: uid,
				value: value,
				dataType: type
			);
			nodes[uid] = node;

			Event<string>.Fire(
				@event: OnNodeCreated,
				value: uid,
				id: $"Framework.Graph.Graph.OnNodeCreated(uid: {uid})"
			);

			return uid;
		}
		public void UpdateNodeValue<T> ( string uid, T value ) {

			var node = GetNode( uid ) as IEditableNode<T>;
			node.SetValue( value );

			Event<string>.Fire(
				@event: OnNodeUpdated,
				value: uid,
				id: $"Framework.Graph.Graph.OnNodeUpdated(uid: {uid})"
			);
		}
		public void DeleteNode ( string uid ) {

			var node = nodes[uid];
			node.LinkUIDs.ForEach( linkUID => {
				var link = links[linkUID];
				var otherNode = nodes[link.ToUID];
				otherNode.BacklinkUIDs.Remove( linkUID );
				links.Remove( linkUID );
			} );
			node.BacklinkUIDs.ForEach( linkUID => {
				var link = links[linkUID];
				var otherNode = nodes[link.FromUID];
				otherNode.LinkUIDs.Remove( linkUID );
				links.Remove( linkUID );
			} );
			nodes.Remove( uid );

			Event<string>.Fire(
				@event: OnNodeDeleted,
				value: uid,
				id: $"Framework.Graph.Graph.OnNodeDeleted(uid: {uid})"
			);
		}

		// links
		public Link GetLink ( string uid )
			=> links[uid];
		public List<Link> GetLinks ( IEnumerable<string> uids )
			=> uids.Convert( uid => GetLink( uid ) );
		public HashSet<string> GetLinkUIDsOfRelationType ( string relationTypeUID )
			=> _linkUIDsByType[relationTypeUID];
		public string CreateLink ( string fromNodeUID, string relationTypeUID, string toNodeUID ) {

			var uid = NewUID();
			var link = new Link(
				uid: uid,
				typeUID: relationTypeUID,
				fromUID: fromNodeUID,
				toUID: toNodeUID
			);

			// track
			links[uid] = link;
			_linkUIDsByType[relationTypeUID ?? "null"].Add( uid );

			var from = nodes[fromNodeUID];
			from.LinkUIDs.Add( uid );

			var to = nodes[toNodeUID];
			to.BacklinkUIDs.Add( uid );

			Event<string>.Fire(
				@event: OnLinkCreated,
				value: uid,
				id: $"Framework.Graph.Graph.OnLinkCreated(uid: {uid})"
			);

			return uid;
		}
		public void UpdateLinkType ( string linkUID, string newRelationTypeUID ) {

			var link = GetLink( linkUID );
			if ( link.TypeUID == newRelationTypeUID ) { return; }

			_linkUIDsByType[link.TypeUID ?? "null"].Remove( linkUID );
			( link as IEditableLink ).SetTypeUID( newRelationTypeUID );
			_linkUIDsByType[link.TypeUID ?? "null"].Add( linkUID );

			Event<string>.Fire(
				@event: OnLinkUpdated,
				value: linkUID,
				id: $"Framework.Graph.Graph.OnLinkUpdated(uid: {linkUID})"
			);
		}
		public void DeleteLink ( string uid ) {

			var link = links[uid];

			var fromNode = nodes[link.FromUID];
			fromNode.LinkUIDs.Remove( uid );

			var toNode = nodes[link.ToUID];
			toNode.BacklinkUIDs.Remove( uid );

			// untrack
			links.Remove( uid );
			_linkUIDsByType[link.TypeUID ?? "null"].Remove( uid );


			Event<string>.Fire(
				@event: OnLinkDeleted,
				value: uid,
				id: $"Framework.Graph.Graph.OnLinkDeleted(uid: {uid})"
			);
		}

		// relation types
		public ICollection<string> GetAllRelationTypeUIDs ()
			=> relationTypes.Keys;
		public IReadOnlyDictionary<string, RelationType> GetAllRelationTypes ()
			=> relationTypes;
		public RelationType GetRelationType ( string uid )
			=> relationTypes[uid];
		public IList<RelationType> GetRelationTypes ( IEnumerable<string> uids )
			=> uids.Convert( uid => GetRelationType( uid ) ).AsReadOnly();
		public string CreateRelationType ( string name ) {

			var uid = NewUID();
			var relationType = new RelationType(
				uid: uid,
				name: name
			);
			relationTypes[uid] = relationType;
			_linkUIDsByType[uid] = new HashSet<string>();

			Event<string>.Fire(
				@event: OnRelationTypeCreated,
				value: uid,
				id: $"Framework.Graph.Graph.OnRelationTypeCreated(uid: {uid})"
			);

			return uid;
		}
		public void UpdateRelationTypeName ( string relationTypeUID, string name ) {

			var relationType = GetRelationType( relationTypeUID ) as IEditableRelationType;
			relationType.SetName( name );

			Event<string>.Fire(
				@event: OnRelationTypeUpdated,
				value: relationTypeUID,
				id: $"Framework.Graph.Graph.OnRelationTypeUpdated(uid: {relationTypeUID})",
				priority: EventLogPriorities.Important
			);
		}
		public void DeleteRelationType ( string uid ) {

			// TODO: what should happen here
			Event<string>.Fire(
				@event: OnRelationTypeDeleted,
				value: uid,
				id: $"Framework.Graph.Graph.OnRelationTypeDeleted(uid: {uid})"
			);
		}


		// ********** Protected Interface **********

		[JsonProperty( ItemConverterType = typeof( NodeConverter ) )] protected Dictionary<string, Node> nodes;
		[JsonProperty] protected Dictionary<string, Link> links;
		[JsonProperty] protected Dictionary<string, RelationType> relationTypes;


		// ********** Private Interface **********

		private string NewUID () {

			return StringHelpers.UID.Generate(
				length: 10,
				validateUniqueness: uid =>
					nodes.KeyIsUnique( uid ) &&
					links.KeyIsUnique( uid ) &&
					relationTypes.KeyIsUnique( uid ) );
		}
		private NodeDataTypes GetType<T> ( T value = default( T ) ) {

			return typeof( T ) switch {
				Type t when t == typeof( string ) => NodeDataTypes.String,
				Type t when t == typeof( int ) => NodeDataTypes.Integer,
				_ => NodeDataTypes.Invalid
			};
		}


		// ********** runtime processing **********

		[JsonIgnore] protected Dictionary<string, HashSet<string>> _linkUIDsByType;

		[OnDeserialized] private void OnAfterDeserialize ( StreamingContext context ) => ProcessAfterDeserialization();
		protected virtual void ProcessAfterDeserialization () {

			TrackLinksByType();
		}
		private void TrackLinksByType () {

			// fill hashset with keys of type UIDs
			relationTypes.ForEach( ( uid, _ ) => {
				_linkUIDsByType[uid] = new HashSet<string>();
			} );
			_linkUIDsByType["null"] = new HashSet<string>();
			foreach ( var relationType in relationTypes.Keys ) {
				_linkUIDsByType[relationType] = new HashSet<string>();
			}

			// populate hashset with link UIDs
			foreach ( var pair in links ) {
				var id = pair.Key;
				var link = pair.Value;
				if ( link.TypeUID == null ) {
					_linkUIDsByType["null"].Add( id );
				} else {
					_linkUIDsByType[link.TypeUID].Add( id );
				}
			}
		}
	}
}