using Jakintosh.Observable;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Explorer.View {

	public class ValidatedTextInput : View {

		// *********** Public Interface ***********

		// events
		public UnityEvent<(string text, bool isValid)> OnTextChanged = new UnityEvent<(string text, bool isValid)>();
		public UnityEvent OnSubmit = new UnityEvent();

		// functions
		public void SetInteractable ( bool interactable ) {

			_input.SetEditable( interactable );
		}
		public void SetText ( string text ) {

			_text.Set( text );
		}
		public void SetValidators ( params Func<string, bool>[] validators ) {

			_validators = validators;
			Validate( _text.Get(), _validators, _validationExceptions );
		}
		public void SetValidationExceptions ( params string[] exceptions ) {

			_validationExceptions = exceptions;
			Validate( _text.Get(), _validators, _validationExceptions );
		}


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private TextEdit.Text _input;

		[Header( "UI Display" )]
		[SerializeField] private Image _validIndicator;

		// view model
		private Observable<string> _text;
		private Observable<bool> _isValid;

		// data
		private Func<string, bool>[] _validators = new Func<string, bool>[0];
		private string[] _validationExceptions = new string[0];

		// View overrides
		protected override void OnInitialize () {

			// init subviews
			_input.Init();

			// init observables
			_isValid = new Observable<bool>(
				initialValue: false,
				onChange: isValid => {
					_validIndicator.color = isValid ? Client.Colors.Action : Client.Colors.Error;
					OnTextChanged?.Invoke( (_text?.Get(), isValid) );
				}
			);
			_text = new Observable<string>(
				initialValue: null,
				onChange: text => {

					// update text field
					_input.SetText( text );

					// validate
					Validate( text, _validators, _validationExceptions );

					OnTextChanged?.Invoke( (text, _isValid.Get()) );
				}
			);

			// sub to controls
			_input.OnTextChanged.AddListener( text => {
				_text.Set( text );
			} );
			_input.OnSubmit.AddListener( () => {
				OnSubmit?.Invoke();
			} );
		}
		protected override void OnCleanup () { }

		// private functions
		private void Validate ( string text, Func<string, bool>[] validators, string[] exceptions ) {

			// check for exception
			var isExcepted = exceptions.Reduce( startValue: false, ( isExcepted, exceptionString ) => {
				return isExcepted || exceptionString == text;
			} );
			if ( isExcepted ) {
				_isValid.Set( true );
				return;
			}

			// check validity
			var isValid = validators.Reduce( startValue: true, ( isValid, validator ) => {
				return isValid && validator( text );
			} );
			_isValid.Set( isValid );
		}
	}
}
