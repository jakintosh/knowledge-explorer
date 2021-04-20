using System;
using Newtonsoft.Json;

namespace Explorer.Model.View {

	[Serializable]
	public class Relationship {

		// properties
		[JsonIgnore] public string NodeUID => nodeUid;
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public Float3 Position => position;
		[JsonIgnore] public Presence.Sizes Size => size;

		// constructor
		public Relationship ( string nodeUid, string graphUid, Float3 position, Presence.Sizes size ) {

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

}
