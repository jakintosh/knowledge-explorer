using Jakintosh.View;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Library.ViewModel {

	[System.Serializable]
	public class GraphViewport {

		// ********** Public Interface **********

		// properties
		[JsonIgnore] public SubscribableDictionary<ViewHandle, Concept> Concepts => _concepts;
		[JsonIgnore] public SubscribableDictionary<ViewHandle, Link> Links => _links;

		// constructor
		public GraphViewport ( List<Concept> concepts, List<Link> links ) {

			_concepts = new SubscribableDictionary<ViewHandle, Concept>();
			_links = new SubscribableDictionary<ViewHandle, Link>();

			concepts?.ForEach( concept => _concepts.Register( concept.GetKey(), concept ) );
			links?.ForEach( link => _links.Register( link.GetKey(), link ) );
		}
		public static GraphViewport Empty
			=> new GraphViewport(
				concepts: null,
				links: null
			);


		// ********** Private Interface **********

		[JsonProperty( propertyName: "concepts" )] private SubscribableDictionary<ViewHandle, Concept> _concepts;
		[JsonProperty( propertyName: "links" )] private SubscribableDictionary<ViewHandle, Link> _links;
	}
}