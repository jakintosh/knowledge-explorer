using System;

namespace Explorer.View.Model {

	public struct ContextMenu {

		public ContextAction[] Actions { get; private set; }

		public ContextMenu ( params ContextAction[] actions ) {

			Actions = actions;
		}
	}

	public struct ContextAction {

		public string Name { get; private set; }
		public Action Action { get; private set; }

		public ContextAction ( string name, Action action ) {
			Name = name;
			Action = action;
		}
	}
}