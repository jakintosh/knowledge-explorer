using Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

using ResourceMetadata = Framework.Data.Metadata.Resource;

namespace Client.ViewModel {

	[Serializable]
	public class Node : IEquatable<Node> {

		// ********** OUTPUTS **********

		[JsonProperty] public Output<string> UID = new Output<string>();


		// ********** INPUTS ***********



		// *****************************

		// components
		[JsonProperty] public Window Window = new Window();

		// constructors
		[JsonConstructor] public Node () { } // this is needed to prevent Json.NET from shitting itself
		public Node ( string uid ) {

			UID.Set( uid );
			Window.Frame.Size.Set( new Vector3( 4f, 6f, 0.25f ) );
		}

		bool IEquatable<Node>.Equals ( Node other ) => other?.UID?.Get().Equals( UID.Get() ) ?? false;
	}


	[Serializable]
	public class Workspace : IEquatable<Workspace> {


		// ********** OUTPUTS **********

		[JsonProperty] public Output<string> Name = new Output<string>();
		[JsonProperty] public Output<string> UID = new Output<string>();
		[JsonProperty] public Output<string> GraphUID = new Output<string>();
		[JsonIgnore] public ListOutput<Node> Nodes = new ListOutput<Node>();


		// ********** INPUTS ***********

		public void CreateNode ( string title = null, bool openImmediately = false ) {

			var uid = _graph.NewConcept( title: title );
			if ( openImmediately ) {
				OpenNode( uid );
			}
		}
		public void DeleteNode ( string uid ) {

			_graph.DeleteNode( uid );
			CloseNode( uid );
		}

		public void OpenNode ( string uid, string sourceID = null ) {

			var node = new Node( uid );
			nodes.Add( node );
			Nodes.Set( nodes );
		}
		public void CloseNode ( string uid ) {

			nodes.RemoveAll( node => node.UID.Get() == uid );
			Nodes.Set( nodes );
		}


		// *****************************


		public Workspace () {

			Nodes.Set( nodes );
		}

		// TODO: this should be part of some Resource Dependency system
		public void SetGraph ( Server.KnowledgeGraph graph ) {

			_graph = graph;
		}

		// runtime data
		[JsonProperty] private List<Node> nodes = new List<Node>();
		[JsonIgnore] private Server.KnowledgeGraph _graph;


		// ********** Equality Stuff **********

		public bool Equals ( Workspace other ) {
			return other?.UID.Get()?.Equals( UID.Get() ) ?? false;
		}
		public override bool Equals ( object obj ) {
			if ( obj == null ) { return false; }
			var workspaceObj = obj as Workspace;
			if ( workspaceObj == null ) { return false; }
			return Equals( workspaceObj );
		}
		public override int GetHashCode () {
			return UID.Get()?.GetHashCode() ?? "".GetHashCode();
		}

	}

}