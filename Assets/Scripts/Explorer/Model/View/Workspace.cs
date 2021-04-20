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
		The data that describes a workspace.

		All of the open concept nodes.
	*/
	[Serializable]
	public class Workspace {

		// data
		[JsonIgnore] public string UID => uid;
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public string Name => name;
		[JsonIgnore] public List<View.Concept> Concepts => _conceptWindows;
		[JsonIgnore] public List<string> Relationships => _openRelationshipUIDs;

		public Workspace () {

			_conceptWindows = new List<View.Concept>();
			_openRelationshipUIDs = new List<string>();
		}
		public void Initialize ( string uid, string graphUid, string name ) {

			this.uid = uid;
			this.graphUid = graphUid;
			this.name = name;
		}

		public void SetConcepts ( IEnumerable<View.Concept> concepts ) {

			_conceptWindows.Clear();
			_conceptWindows.AddRange( concepts );
		}
		public void SetOpenRelationships ( IEnumerable<string> relUIDs ) {

			_openRelationshipUIDs.Clear();
			_openRelationshipUIDs.AddRange( relUIDs );
		}

		// private data
		[JsonProperty] private string uid;
		[JsonProperty] private string graphUid;
		[JsonProperty] private string name;
		[JsonProperty] private List<View.Concept> _conceptWindows;
		[JsonProperty] private List<string> _openRelationshipUIDs;
	}

}