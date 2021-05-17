using Framework;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Explorer.View {

	public class ValidatedTextEntryDialog : View {

		// ********** Public Interface **********

		// events
		public UnityEvent OnClose = new UnityEvent();
		public UnityEvent<string> OnConfirm = new UnityEvent<string>();

		// methods
		public void SetTitle ( string title ) {

			_titleDisplay.SetText( title );
		}
		public void SetValidators ( params Func<string, bool>[] validators ) {

			_text.SetValidators( validators );
		}


		// ********** Private Interface **********

		[Header( "UI Control" )]
		[SerializeField] private TMP_InputField _nameInputControl;
		[SerializeField] private Button _cancelControl;
		[SerializeField] private Button _confirmControl;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _titleDisplay;
		[SerializeField] private Image _validityDisplay;

		// model data
		private Func<string, bool> _textValidator;
		private ValidatedObservable<string> _text;

		protected override void OnInitialize () {

			// init observables
			_text = new ValidatedObservable<string>(
				initialValue: "",
				onChange: text => {
					_nameInputControl.SetTextWithoutNotify( text );
				},
				onValid: isValid => {
					_validityDisplay.color = isValid ? Color.green : Color.red;
					_confirmControl.interactable = isValid;
				}
			);

			_cancelControl.onClick.AddListener( () => {
				Close();
			} );
			_nameInputControl.onValueChanged.AddListener( name => {
				_text.Set( name );
			} );
			_nameInputControl.onSubmit.AddListener( _ => {
				Confirm();
			} );
			_confirmControl.onClick.AddListener( () => {
				Confirm();
			} );
		}
		protected override void OnCleanup () {

			_cancelControl.onClick.RemoveAllListeners();
			_nameInputControl.onValueChanged.RemoveAllListeners();
			_nameInputControl.onSubmit.RemoveAllListeners();
			_confirmControl.onClick.RemoveAllListeners();
		}

		private void Close () {

			OnClose?.Invoke();
		}
		private void Confirm () {

			OnConfirm?.Invoke( _text.Get() );
			Close();
		}
	}

}