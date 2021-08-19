using Jakintosh.View;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Library.ViewModel {

	[System.Serializable]
	public class Concept : BaseViewModel, ISubscribableDictionaryElement<ViewHandle, Concept> {

		// ***** ISubscribableDictionaryElement *****

		public UnityEvent<Concept> OnUpdated => _onUpdated;
		public ViewHandle GetKey () => GetViewHandle();


		// ********** Public Interface **********

		// properties
		[JsonIgnore] public string NodeUID => _nodeUid;
		[JsonIgnore] public Float3 Position => _position;

		// constructor
		public Concept ( string uid, Float3 position ) {

			_nodeUid = uid;
			_position = position;
			_onUpdated = new UnityEvent<Concept>();
		}
		public static Concept Default ( string uid )
			=> new Concept(
				uid: uid,
				position: Float3.Zero
			);


		// ********** Private Interface **********

		// serialized data
		[JsonProperty( propertyName: "nodeUid" )] private string _nodeUid;
		[JsonProperty( propertyName: "position" )] private Float3 _position;

		// internal data
		[JsonIgnore] private UnityEvent<Concept> _onUpdated;
	}

}