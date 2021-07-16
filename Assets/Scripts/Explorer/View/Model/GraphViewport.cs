using Newtonsoft.Json;
using System.Collections.Generic;

namespace Explorer.View.Model {

	public class GraphViewport {

		// static defaults
		public static GraphViewport Empty => new GraphViewport( new List<Concept>(), new List<Link>() );

		// properties
		[JsonProperty( propertyName: "concepts" )] public List<Concept> Concepts { get; private set; }
		[JsonProperty( propertyName: "link" )] public List<Link> Links { get; private set; }

		// constructor
		public GraphViewport ( List<Concept> concepts, List<Link> links ) {

			Concepts = new List<Concept>();
			Links = new List<Link>();

			if ( concepts != null ) { Concepts.AddRange( concepts ); }
			if ( links != null ) { Links.AddRange( links ); }
		}
	}
}