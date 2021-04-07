using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework {

	public abstract class ModelHandler<T> : MonoBehaviour
		where T : class {


		// ************ Public Interface ************

		public void SetModel ( T model ) {

			if ( _isInitialized ) {
				if ( _model == model ) { return; }
			} else {
				_isInitialized = true;
			}

			if ( _model != null ) {
				UnbindView();
			}

			_model = model;
			PropogateModel( _model );

			if ( _model != null ) {
				HandleNonNullModel();
				BindViewToOutputs( _model );
			} else {
				HandleNullModel();
			}
		}


		// ********** Abstract Interface ***********

		protected abstract string BindingKey { get; }

		protected abstract void PropogateModel ( T model );
		protected abstract void BindViewToOutputs ( T model );

		protected virtual void HandleNonNullModel () { }
		protected virtual void HandleNullModel () { }

		protected virtual void OnDestroy () {
			if ( _model != null ) { UnbindView(); }
		}


		// ********** Internal Interface ***********

		private Dictionary<string, IUnbindableOutput> _outputBindings = new Dictionary<string, IUnbindableOutput>();

		private bool _isInitialized = false;
		protected T _model;

		protected void Bind<U> ( Output<U>.Binding binding, Output<U> toOutput ) {

			var key = BindingKey;
			toOutput.Bind( key, binding );
			_outputBindings[key] = toOutput;
		}
		private void UnbindView () {

			foreach ( var bindingPair in _outputBindings ) {
				var key = bindingPair.Key;
				var binding = bindingPair.Value;
				binding.Unbind( key );
			}
		}
	}

}