using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework {

	/*
        Output: data that can be bound to.
		Binding: something that can be bound to an output and receieve that data
    */


	/*
		[Serializable] public class Output : IUnbindableOutput {

			private interface IBindingInvokable {
				void Invoke ();
			}
			public class Binding : IBindingInvokable {

				void IBindingInvokable.Invoke () => _actionHandler();

				public Binding ( System.Action actionHandler ) => _actionHandler = actionHandler;

				private System.Action _actionHandler;
			}

			// accessors
			public void Invoke () {
				foreach ( var pair in _bindings ) {
					pair.Value.Invoke();
				}
			}

			// binding
			public void Bind ( string key, Binding binding ) {

				_bindings.Add( key, binding );
			}
			public void Unbind ( string key ) {

				_bindings.Remove( key );
			}

			// runtime data
			private Dictionary<string, IBindingInvokable> _bindings = new Dictionary<string, IBindingInvokable>();
		}
	*/


	public interface IUnbindableOutput {
		void Unbind ( string key );
	}

	[Serializable]
	public class Output<T> : IUnbindableOutput {

		private interface IBindingReceivable {

			void ReceiveValue ( T value );
		}
		public class Binding : IBindingReceivable {

			void IBindingReceivable.ReceiveValue ( T value ) => _valueHandler( value );
			public Binding ( System.Action<T> valueHandler ) => _valueHandler = valueHandler;
			private System.Action<T> _valueHandler;
		}

		public T Get () => this.value;
		public virtual void Set ( T value ) {

			// check for equality
			if ( this.value == null || value == null ) {
				if ( this.value == null && value == null ) {
					return;
				}
			} else if ( value.Equals( this.value ) ) {
				return;
			}

			this.value = value;
			SendValue( this.value );
		}

		public void Bind ( string key, Binding binding ) {

			var receiver = binding as IBindingReceivable;
			receiver.ReceiveValue( value );
			_bindings.Add( key, receiver );
		}
		public void Unbind ( string key ) {

			_bindings.Remove( key );
		}

		protected void SendValue ( T value ) {
			foreach ( var pair in _bindings ) {
				pair.Value.ReceiveValue( this.value );
			}
		}

		// data
		[JsonProperty] protected T value = default( T );
		[JsonIgnore] private Dictionary<string, IBindingReceivable> _bindings = new Dictionary<string, IBindingReceivable>();
	}

	[Serializable]
	public class ListOutput<U> : Output<IList<U>>
		where U : IEquatable<U> {

		// set is completely-overriden for list equality check
		public override void Set ( IList<U> value ) {

			// check for equality
			if ( this.value == null || value == null ) {
				if ( value == null && this.value == null ) {
					return;
				}
			} else if ( value.SequenceEqual( this._comparisonArray ) ) {
				return;
			}

			this._comparisonArray = value.ToArray<U>();
			this.value = value;
			SendValue( this.value );
		}

		// data
		private U[] _comparisonArray;
	}

	[Serializable]
	public class ValidatedOutput<T> : Output<T> {

		// nested output
		[JsonProperty] public Output<bool> IsValid = new Output<bool>();

		// set override for validation
		public override void Set ( T value ) {

			base.Set( value );

			var valid = true;
			foreach ( var validator in _validators ) {
				if ( !validator( value ) ) {
					valid = false;
					break;
				}
			}
			IsValid.Set( valid );
		}

		// validation modifiers
		public void AddValidator ( Func<T, bool> validator ) => _validators.Add( validator );
		public void ClearValidators () => _validators.Clear();

		// data
		private List<Func<T, bool>> _validators = new List<Func<T, bool>>();
	}

}