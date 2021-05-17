using Newtonsoft.Json;

namespace Explorer.View.Model {

	public class Link {

		// properties
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public string LinkUID => linkUid;

		// constructor
		public Link ( string graphUid, string linkUid ) {

			this.graphUid = graphUid;
			this.linkUid = linkUid;
		}

		// serialized data
		[JsonProperty] private string graphUid;
		[JsonProperty] private string linkUid;
	}
}