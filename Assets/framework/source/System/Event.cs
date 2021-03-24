using System;
using UnityEngine;

namespace Framework {

	public enum EventLogPriorities {
		None = -1,
		Important = 0,
		Verbose = 1
	}

	public static class EventLogPriority {

		public static EventLogPriorities Current = EventLogPriorities.Important;
	}

	public class Event {

		public delegate void Signature ();

		public static void Fire ( Signature @event, string id = "Event", EventLogPriorities priority = EventLogPriorities.Verbose ) {

			if ( !Application.isPlaying ) {
				Debug.LogWarning( "Trying to fire event with identifier '" + id + "' in edit mode. This is not allowed. " );
				return;
			}

			if ( Application.isEditor && priority <= EventLogPriority.Current ) {
				Debug.Log( $"Firing event: {id}" );
			}

			var handler = @event;
			if ( handler != null ) {
				var invocationList = handler.GetInvocationList();
				foreach ( Delegate currentHandler in invocationList ) {
					var currentSubscriber = (Signature)currentHandler;
					try {
						currentSubscriber();
					} catch ( Exception ex ) {
						Debug.LogWarning( id + " exception found: " + ex.ToString() );
					}
				}
			}
		}
	}

	public class Event<T> {

		public delegate void Signature ( T value );

		public static void Fire ( Signature @event, T value, string id = "Event<T>", EventLogPriorities priority = EventLogPriorities.Verbose ) {

			if ( !Application.isPlaying ) {
				Debug.LogWarning( "Trying to fire event with identifier '" + id + "' in edit mode. This is not allowed. " );
				return;
			}

			if ( Application.isEditor && priority <= EventLogPriority.Current ) {
				Debug.Log( $"Firing event: {id} with value {value}" );
			}

			var handler = @event;
			if ( handler != null ) {
				var invocationList = handler.GetInvocationList();
				foreach ( Delegate currentHandler in invocationList ) {
					var currentSubscriber = (Signature)currentHandler;
					try {
						currentSubscriber( value );
					} catch ( Exception ex ) {
						Debug.LogWarning( id + " exception found: " + ex.ToString() );
					}
				}
			}
		}
	}
}