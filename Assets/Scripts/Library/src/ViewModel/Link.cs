using Jakintosh.Data;
using Jakintosh.View;
using System;
using UnityEngine.Events;

namespace Library.ViewModel {

	[Serializable]
	public class Link : IIdentifiable<int>, IUpdatable<Link> {

		// ***** interface implementations *****

		public UnityEvent<Link> OnUpdated => _onUpdated;
		public int Identifier => _viewHandle;


		// ********** Public Interface **********

		public string LinkUID => _linkUid;

		// constructor
		public Link ( string uid ) {

			_viewHandle = ViewHandles.Generate();
			_linkUid = uid;
		}


		// ********** Private Interface **********

		// serialized data
		private int _viewHandle;
		private string _linkUid;

		// private data
		private UnityEvent<Link> _onUpdated;
	}
}