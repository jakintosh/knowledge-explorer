using Newtonsoft.Json;
using System.Collections.Generic;

namespace Explorer.Model.View {

	public class Workspace {

		// data
		[JsonIgnore] public string UID => uid;
		[JsonIgnore] public string GraphUID => graphUid;
		[JsonIgnore] public string Name => name;
		[JsonIgnore] public List<View.Concept> Concepts => _conceptWindows;
		[JsonIgnore] public List<string> Links => _openLinkUIDs;

		public Workspace () {

			_conceptWindows = new List<View.Concept>();
			_openLinkUIDs = new List<string>();
			_relationTypeColors = new Dictionary<string, string>();
		}

		public void Initialize ( string uid, string name, string graphUID, Knowledge.Graph graph ) {

			this.uid = uid;
			this.name = name;
			this.graphUid = graphUID;

			// init relation type colors
			graph.AllRelationTypes.ForEach( ( uid, _ ) => _relationTypeColors.Add( uid, "FF00FF" ) );
			UnityEngine.Debug.Log( "colors\n---" );
			_relationTypeColors.ForEach( ( uid, color ) => UnityEngine.Debug.Log( $"{uid} - {color}" ) );
		}

		public string GetRelationTypeColor ( string uid ) {

			return _relationTypeColors.TryGetValue( uid, out var color ) ? color : null;
		}

		public void SetRelationTypeColor ( string uid, string color ) {

			_relationTypeColors[uid] = color;
		}
		public void SetConcepts ( IEnumerable<View.Concept> concepts ) {

			_conceptWindows.Clear();
			_conceptWindows.AddRange( concepts );
		}
		public void SetOpenLinks ( IEnumerable<string> linkUIDs ) {

			_openLinkUIDs.Clear();
			_openLinkUIDs.AddRange( linkUIDs );
		}

		// private data
		[JsonProperty] private string uid;
		[JsonProperty] private string graphUid;
		[JsonProperty] private string name;
		[JsonProperty] private List<View.Concept> _conceptWindows;
		[JsonProperty] private List<string> _openLinkUIDs;
		[JsonProperty] private Dictionary<string, string> _relationTypeColors;
	}

}