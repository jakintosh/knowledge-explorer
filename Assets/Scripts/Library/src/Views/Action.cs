using Jakintosh.Actions;
using Jakintosh.View;
using TMPro;
using UnityEngine;

namespace Library.Views {

	public class Action : ReuseableView<IHistoryAction> {

		public override IHistoryAction GetState () => _action;

		[SerializeField] private TextMeshProUGUI _nameLabel;
		[SerializeField] private TextMeshProUGUI _descriptionLabel;

		private IHistoryAction _action;

		protected override void OnInitialize () {

		}
		protected override void OnPopulate ( IHistoryAction action ) {

			_action = action;

			if ( _action == null ) { return; }

			_nameLabel.text = _action.GetName();
			_descriptionLabel.text = _action.GetDescription();
		}
		protected override void OnRecycle () {

		}
		protected override void OnCleanup () {

		}
	}
}

