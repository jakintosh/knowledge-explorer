using Newtonsoft.Json;

namespace Library.ViewModel {

	/*
		Data that completely describes a user's workspace for a given
		graph. Holds all view state information as well as any local
		user preference data related to the workspace.
	*/
	public class Workspace {

		[JsonIgnore] public string UID => _uid;
		[JsonIgnore] public string Name => _name;
		[JsonIgnore] public GraphViewport GraphViewport => _graphViewport;

		public void Initialize ( string uid, string name ) {

			_uid = uid;
			_name = name;
			_graphViewport = new GraphViewport();
		}
		public void Rename ( string to ) {
			_name = to;
		}

		// serializable data
		[JsonProperty( propertyName: "uid" )] private string _uid;
		[JsonProperty( propertyName: "name" )] private string _name;
		[JsonProperty( propertyName: "graphViewport" )] private GraphViewport _graphViewport;
	}
}