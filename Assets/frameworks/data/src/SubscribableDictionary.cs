using Coalescent.Computer;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Jakintosh.Data {

	[Serializable]
	public class SubscribableDictionary<TKey, TModel>
		where TModel : IUpdatable<TModel>, IIdentifiable<TKey> {

		// events
		[NonSerialized] public UnityEvent<TKey> OnAdded = new UnityEvent<TKey>();
		[NonSerialized] public UnityEvent<TKey> OnUpdated = new UnityEvent<TKey>();
		[NonSerialized] public UnityEvent<TKey> OnRemoved = new UnityEvent<TKey>();

		// crud
		public TModel Get ( TKey handle ) {

			return _data[handle];
		}
		public List<TModel> GetAll () {

			return new List<TModel>( _data.Values );
		}
		public void Register ( TModel data ) {

			var handle = data.Identifier;
			data.OnUpdated.AddListener( HandleDataUpdated );
			_data.Add( handle, data );
			OnAdded?.Invoke( handle );
		}
		public void Unregister ( TKey handle ) {

			_data[handle]?.OnUpdated.RemoveListener( HandleDataUpdated );
			_data.Remove( handle );
			OnRemoved?.Invoke( handle );
		}

		// data
		private Dictionary<TKey, TModel> _data = new Dictionary<TKey, TModel>();

		// event handlers
		private void HandleDataUpdated ( TModel data )
			=> OnUpdated?.Invoke( data.Identifier );
	}
}