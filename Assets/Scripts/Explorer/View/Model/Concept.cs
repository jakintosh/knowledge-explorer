using Newtonsoft.Json;

using Presence = Explorer.View.PresenceControl;

namespace Explorer.View.Model {

	public class Concept {

		// properties
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public string NodeUID => nodeUid;
		[JsonIgnore] public Float3 Position => position;
		[JsonIgnore] public Presence.Sizes Size => size;

		// constructor
		public Concept ( string graphUid, string nodeUid, Float3 position, Presence.Sizes size ) {

			this.graphUid = graphUid;
			this.nodeUid = nodeUid;
			this.position = position;
			this.size = size;
		}
		public static Concept Default ( string graphUid, string nodeUid ) =>
			new Concept(
				graphUid: graphUid,
				nodeUid: nodeUid,
				position: Float3.Zero,
				size: Presence.Sizes.Expanded
			);

		// private data
		[JsonProperty] private string graphUid;
		[JsonProperty] private string nodeUid;
		[JsonProperty] private Float3 position;
		[JsonProperty] private Presence.Sizes size;
	}

}