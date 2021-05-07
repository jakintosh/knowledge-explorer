using Explorer.Model;

namespace Explorer.Client.Subsystems {

	public class Resources : ISubsystem {

		// ********** Public Interface **********

		public IGraphCRUD Graphs => _graphs;
		public IWorkspaceCRUD Workspaces => _workspaces;

		public Resources () {

			_graphs = new GraphResources(
				rootDataPath: LocalDataPath
			);

			_workspaces = new WorkspaceResources(
				rootDataPath: LocalDataPath,
				graphs: _graphs
			);
		}

		public void Initialize () {

			_graphs.LoadMetadata();
			_workspaces.LoadMetadata();
		}
		public void Teardown () {

			_graphs.Close();
			_workspaces.Close();
		}


		// ********** Private Interface **********

		private string LocalDataPath => $"/data/local";

		private GraphResources _graphs;
		private WorkspaceResources _workspaces;
	}

}