using System;
using Jakintosh.Data;

namespace Library.ViewModel {

	[Serializable]
	public class GraphViewport {

		// ********** Public Interface **********

		// properties
		public SubscribableDictionary<int, Concept> Concepts => _concepts;
		public SubscribableDictionary<int, Link> Links => _links;


		// ********** Private Interface **********

		// serialized data
		private SubscribableDictionary<int, Concept> _concepts = new SubscribableDictionary<int, Concept>();
		private SubscribableDictionary<int, Link> _links = new SubscribableDictionary<int, Link>();
	}
}