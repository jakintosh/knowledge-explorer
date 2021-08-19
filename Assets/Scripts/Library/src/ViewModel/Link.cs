using Jakintosh.View;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Library.ViewModel {

	[System.Serializable]
	public class Link : BaseViewModel, ISubscribableDictionaryElement<ViewHandle, Link> {

		// ***** ISubscribableDictionaryElement *****

		public UnityEvent<Link> OnUpdated => _onUpdated;
		public ViewHandle GetKey () => GetViewHandle();


		// ********** Public Interface **********

		// properties
		[JsonIgnore] public string UID => _uid;

		// constructor
		public Link ( string uid ) {
			_uid = uid;
		}


		// ********** Private Interface **********

		// serialized data
		[JsonProperty( propertyName: "uid" )] private string _uid;

		// private data
		[JsonIgnore] private UnityEvent<Link> _onUpdated;
	}
}