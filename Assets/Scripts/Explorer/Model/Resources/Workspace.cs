using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Explorer.Model {

	namespace Presence {

		// data types
		public enum Contexts {
			Floating,
			Focused
		}
		public enum Sizes {
			Expanded,
			Compact
		}

	}

	/*
		The data that describes a concept node.

		The UID of the node, along with its size and position.
	*/
	[Serializable]
	public class Concept {

		// properties
		[JsonIgnore] public string NodeUID => nodeUid;
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public Float3 Position => position;
		[JsonIgnore] public Presence.Sizes Size => size;

		// constructor
		public Concept ( string nodeUid, string graphUid, Float3 position, Presence.Sizes size ) {

			this.nodeUid = nodeUid;
			this.graphUid = graphUid;
			this.position = position;
			this.size = size;
		}

		public static Concept Default ( string nodeUid, string graphUid )
			=> new Concept(
				nodeUid: nodeUid,
				graphUid: graphUid,
				position: Float3.Zero,
				size: Model.Presence.Sizes.Expanded
			);


		// private data
		[JsonProperty] private string nodeUid;
		[JsonProperty] private string graphUid;
		[JsonProperty] private Float3 position;
		[JsonProperty] private Presence.Sizes size;
	}

	/*
		The data that describes a workspace.

		All of the open concept nodes.
	*/
	[Serializable]
	public class Workspace {

		// data
		[JsonIgnore] public string UID => uid;
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public string Name => name;
		[JsonIgnore] public List<Concept> Concepts => _conceptWindows;

		public Workspace () {

			_conceptWindows = new List<Concept>();
		}
		public void Initialize ( string uid, string graphUid, string name ) {

			this.uid = uid;
			this.graphUid = graphUid;
			this.name = name;
		}

		public void SetConcepts ( IEnumerable<Concept> concepts ) {

			_conceptWindows.Clear();
			_conceptWindows.AddRange( concepts );
		}

		// private data
		[JsonProperty] private string uid;
		[JsonProperty] private string graphUid;
		[JsonProperty] private string name;
		[JsonProperty] private List<Concept> _conceptWindows;
	}

}