using Newtonsoft.Json;

namespace Explorer.View.Model {

	/*
		Data that completely describes a user's workspace for a given
		graph. Holds all view state information as well as any local
		user preference data related to the workspace.
	*/
	public class Workspace {

		// data
		[JsonProperty( propertyName: "uid" )] public string UID { get; private set; }
		[JsonProperty( propertyName: "graphUid" )] public string GraphUID { get; private set; }
		[JsonProperty( propertyName: "name" )] public string Name { get; set; }
		[JsonProperty( propertyName: "graphViewport" )] public GraphViewport GraphViewport { get; set; }

		public void Initialize ( string uid, string name, Jakintosh.Knowledge.Graph graph ) {

			UID = uid;
			Name = name;
			GraphUID = graph.UID;
			GraphViewport = GraphViewport.Empty;
		}
	}
}