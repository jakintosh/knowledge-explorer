using Framework;
using System;
using UnityEngine;

namespace Library.Model {

	[Serializable]
	public class Node {

		// ********** Serialized Backing Data **********

		[SerializeField] private string id;
		[SerializeField] private string title;
		[SerializeField] private string body;


		// ************* Public Interface **************

		// events
		public struct TitleChangedEventData {
			public string OldTitle;
			public string NewTitle;
			public TitleChangedEventData ( string oldTitle, string newTitle ) {
				OldTitle = oldTitle;
				NewTitle = newTitle;
			}
		}
		public event Event<TitleChangedEventData>.Signature OnTitleChanged;
		public event Event<string>.Signature OnBodyChanged;

		// properties
		public string ID {
			get => id;
		}
		public string Title {
			get => title;
			set {

				if ( title == value ) { return; }

				var oldTitle = title;
				title = value;

				Event<TitleChangedEventData>.Fire(
					@event: OnTitleChanged,
					value: new TitleChangedEventData( oldTitle, newTitle: title ),
					id: "Library.Model.Node.OnTitleChanged"
				);
			}
		}
		public string Body {
			get => body;
			set {

				if ( body == value ) { return; }

				body = value;

				Event<string>.Fire(
					@event: OnBodyChanged,
					value: body,
					id: "Library.Model.Node.OnBodyChanged"
				);
			}
		}

		// constructor
		public Node ( string id ) {

			this.id = id;
		}
	}

}