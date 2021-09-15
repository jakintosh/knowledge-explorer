using Jakintosh.Data;
using Jakintosh.Resources;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

using Graph = Jakintosh.Knowledge.Graph;
using Workspace = Library.ViewModel.Workspace;

namespace Library.Resources {

	[Serializable]
	public class NodeDiff :
		IBytesSerializable {

		// properties
		public StringDiff Title => title;
		public StringDiff Body => body;

		// interfaces
		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );

		// constructor
		public NodeDiff ( StringDiff titleDiff, StringDiff bodyDiff ) {

			title = titleDiff;
			body = bodyDiff;
		}

		// serialized data
		private StringDiff title;
		private StringDiff body;
	}

	[Serializable]
	public class Node :
		IBytesSerializable,
		IDiffable<Node, NodeDiff>,
		IDuplicatable<Node>,
		IUpdatable<Node> {

		// properties
		public string Title {
			get => title;
			set {
				if ( title == value ) { return; }
				title = value;
				_onUpdated?.Invoke( this );
			}
		}
		public string Body {
			get => body;
			set {
				if ( body == value ) { return; }
				body = value;
				_onUpdated?.Invoke( this );
			}
		}

		// interfaces
		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );
		public UnityEvent<Node> OnUpdated
			=> _onUpdated;
		public Node Apply ( NodeDiff diff ) {

			var node = new Node();
			node.Title = diff.Title.ApplyTo( Title );
			node.Body = diff.Body.ApplyTo( Body );
			return node;
		}
		public NodeDiff Diff ( Node from ) {

			return new NodeDiff(
				titleDiff: DiffUtil.CreateStringDiff( from.Title, Title ),
				bodyDiff: DiffUtil.CreateStringDiff( from.Body, Body )
			);
		}
		public Node Duplicate () {

			var duplicate = new Node();
			duplicate.Title = Title;
			duplicate.Body = Body;
			return duplicate;
		}

		// serialized data
		private string title;
		private string body;

		// runtime data
		[NonSerialized] private UnityEvent<Node> _onUpdated = new UnityEvent<Node>();
	}

	[Serializable]
	public class Link :
		IBytesSerializable,
		IDuplicatable<Link>,
		IUpdatable<Link> {

		// properties
		public string Type {
			get => type;
			set {
				if ( type == value ) { return; }
				type = value;
				_onUpdated?.Invoke( this );
			}
		}
		public string Source {
			get => source;
			set {
				if ( source == value ) { return; }
				source = value;
				_onUpdated?.Invoke( this );
			}
		}
		public string Destination {
			get => destination;
			set {
				if ( destination == value ) { return; }
				destination = value;
				_onUpdated?.Invoke( this );
			}
		}

		// interface
		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );
		public UnityEvent<Link> OnUpdated
			=> _onUpdated;
		public Link Duplicate () {

			var duplicate = new Link();
			return duplicate;
		}

		// serialized data
		private string type;
		private string source;
		private string destination;

		// runtime data
		[NonSerialized] private UnityEvent<Link> _onUpdated = new UnityEvent<Link>();
	}

	public class Data {

		private DiffableData<Node, NodeDiff> _nodeDeltas;

		private AddressableData<Node> _nodes;
		private AddressableData<Link> _links;
	}

	public class Graphs {

		// ********** Public Interface **********

		public Graph Default {
			get {
				if ( _defaultGraph == null ) {
					_defaultGraph = GetDefault();
				}
				return _defaultGraph;
			}
		}

		public Graphs ( string rootDataPath ) {

			_graphs = new Resources<Metadata, Graph>(
				resourcePath: $"{rootDataPath}/graph",
				resourceExtension: "graph",
				uidLength: 6
			);

			_graphs.LoadMetadataFromDisk();

			// make sure there is default
			EnsureGraph();
		}

		public void Close () {

			_graphs.Close();
		}


		// ********** Private Interface **********

		private string _defaultName = "default";
		private Graph _defaultGraph;
		private Resources<Metadata, Graph> _graphs;

		private Graph GetDefault () {

			var defaultGraphQuery = _graphs.GetAllMetadata().Filter( meta => meta.Name == _defaultName );
			if ( defaultGraphQuery.Count == 1 && defaultGraphQuery[0].UID != null ) {
				try {

					return _graphs.RequestResource( defaultGraphQuery[0].UID, load: true );

				} catch ( ResourceMetadataNotFoundException ) {
					UnityEngine.Debug.LogError( "Library.Resources.Graphs.Get: Failed due to missing graph." );
					return null;
				}
			} else {
				UnityEngine.Debug.Log( "For some reason, there is not 1 default graph" );
				return null;
			}
		}
		private void EnsureGraph () {

			// if graph exists, abort
			if ( GetDefault() != null ) {
				return;
			}

			// try create graph
			Metadata graphMetadata;
			Graph graphResource;
			try {

				(graphMetadata, graphResource) = _graphs.Create( _defaultName );
				_graphs.Insert( graphMetadata, graphResource );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Graphs.Create: Failed due to Resource Name Empty" );
				return;

			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Graphs.Create: Failed due to Resource Name Conflict" );
				return;
			}
			graphResource.Initialize( graphMetadata.UID );

			// save graph
			_defaultGraph = graphResource;
		}
	}

	public class Workspaces {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnAnyMetadataChanged;
		public event Framework.Event<Metadata>.Signature OnMetadataAdded;
		public event Framework.Event<Metadata>.Signature OnMetadataUpdated;
		public event Framework.Event<Metadata>.Signature OnMetadataDeleted;

		public Workspaces ( string rootDataPath ) {

			_workspaces = new Resources<Metadata, Workspace>(
				resourcePath: $"{rootDataPath}/workspace",
				resourceExtension: "workspace",
				uidLength: 6
			);

			// update name when metadata updates
			_workspaces.OnMetadataUpdated += metadata => {
				Get( metadata.UID )?.Rename( metadata.Name );
			};

			// pass through event
			_workspaces.OnAnyMetadataChanged += metadata => OnAnyMetadataChanged?.Invoke( metadata );
			_workspaces.OnMetadataAdded += metadata => OnMetadataAdded?.Invoke( metadata );
			_workspaces.OnMetadataUpdated += metadata => OnMetadataUpdated?.Invoke( metadata );
			_workspaces.OnMetadataDeleted += metadata => OnMetadataDeleted?.Invoke( metadata );

			// load metadata
			_workspaces.LoadMetadataFromDisk();
		}
		public void Close () {

			_workspaces.Close();
		}

		public (Metadata metadata, Workspace workspace) Create ( string name ) {

			// try create workspace
			Metadata workspaceMetadata;
			Workspace workspaceResource;
			try {

				(workspaceMetadata, workspaceResource) = _workspaces.Create( name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Create: Failed due to empty resource name." );
				return (null, null);
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Create: Failed due to name conflict." );
				return (null, null);
			}

			// init workspace
			workspaceResource.Initialize(
				uid: workspaceMetadata.UID,
				name: workspaceMetadata.Name
			);

			// return metadata
			return (workspaceMetadata, workspaceResource);
		}
		public bool Insert ( Metadata metadata, Workspace workspace ) {

			return _workspaces.Insert( metadata, workspace );
		}
		public bool Rename ( string uid, string name ) {

			try {

				// rename workspace
				return _workspaces.Rename( uid, name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Rename: Failed due to Resource Name Empty" );
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Rename: Failed due to Resource Name Conflict" );
			} catch ( ResourceFileNameConflictException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Rename: Failed due to Resource File Name Conflict, but not Metadata name conflict. Data may be corrupted." );
			}
			return false;
		}
		public bool Delete ( string uid ) {

			return _workspaces.Delete( uid );
		}

		public Workspace Get ( string uid ) {

			if ( uid == null ) { return null; }

			try {

				return _workspaces.RequestResource( uid, load: true );

			} catch ( ResourceMetadataNotFoundException ) {

				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.Get: Failed due to missing workspace." );
				return null;
			}
		}
		public Metadata GetMetadata ( string uid ) {

			if ( uid == null ) { return null; }

			try {

				return _workspaces.RequestMetadata( uid );

			} catch ( ResourceMetadataNotFoundException ) {
				UnityEngine.Debug.LogError( "Library.Resources.Workspaces.GetMetadata: Failed due to missing workspace." );
				return null;
			}
		}
		public IList<Metadata> GetAll () {

			return _workspaces.GetAllMetadata();
		}

		public bool ValidateName ( string name ) {

			if ( string.IsNullOrEmpty( name ) ) return false;
			return _workspaces.NameIsUnique( name );
		}


		// ********** Private Interface **********

		private Resources<Metadata, Workspace> _workspaces;
	}
}