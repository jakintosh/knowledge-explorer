using Jakintosh.Data;
using Jakintosh.View;
using System;
using UnityEngine.Events;

namespace Library.ViewModel {

	[Serializable]
	public class Concept : IIdentifiable<int>, IUpdatable<Concept> {

		// ***** interface implementations *****

		public int Identifier => _viewHandle;
		public UnityEvent<Concept> OnUpdated => _onUpdated;

		// ********** Public Interface **********

		// properties
		public string NodeUID => _nodeUid;
		public Float3 Position => _position;

		// constructor
		public Concept ( string uid, Float3 position ) {

			_viewHandle = ViewHandles.Generate();
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
		private int _viewHandle;
		private string _nodeUid;
		private Float3 _position;

		// internal data
		[NonSerialized] private UnityEvent<Concept> _onUpdated;
	}
}