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

		[Serializable]
		private struct OutputBinding {
			public string Key;
			public IUnbindableOutput Output;
		}

		private List<OutputBinding> _outputBindings = new List<OutputBinding>();

		private bool _isInitialized = false;
		protected T _model;

		protected void Bind<U> ( Output<U>.Binding binding, Output<U> toOutput ) {
			toOutput.Bind( BindingKey, binding );
			_outputBindings.Add( new OutputBinding() { Key = BindingKey, Output = toOutput as IUnbindableOutput } );
		}
		private void UnbindView ()
			=> _outputBindings.ForEach( binding => binding.Output.Unbind( binding.Key ) );
	}

}