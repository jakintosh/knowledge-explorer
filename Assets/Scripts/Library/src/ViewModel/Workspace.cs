using Newtonsoft.Json;

namespace Library.ViewModel {

	/*
		Data that completely describes a user's workspace for a given
		graph. Holds all view state information as well as any local
		user preference data related to the workspace.
	*/
	public class Workspace {

		// data
		[JsonProperty( propertyName: "uid" )] public string UID { get; private set; }
		[JsonProperty( propertyName: "name" )] public string Name { get; set; }
		[JsonProperty( propertyName: "graphViewport" )] public GraphViewport GraphViewport { get; set; }

		public void Initialize ( string uid, string name ) {

			UID = uid;
			Name = name;
			GraphViewport = GraphViewport.Empty;
		}
	}
}