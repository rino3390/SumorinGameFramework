using System.Collections.Generic;
using VContainer;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板的建置入口
	/// </summary>
	public class GmPresenter: IGmPresenter
	{
		[Inject]
		private GmPanelView panel;

		[Inject]
		private GmFieldCatalog catalog;

		[Inject]
		private GmOperationRunner runner;

	#region IGmPresenter Members
		/// <inheritdoc />
		public void BuildPanel(IReadOnlyList<GmOperation> operations)
		{
			var layouts = new List<GmOperationLayout>(operations.Count);

			foreach(var operation in operations)
			{
				layouts.Add(new(operation, BuildersFor(operation), arguments => runner.Run(operation, arguments)));
			}

			panel.Build(layouts);
		}
	#endregion

		private GmFieldBuilder[] BuildersFor(GmOperation operation)
		{
			var builders = new GmFieldBuilder[operation.Parameters.Count];

			for(var index = 0; index < builders.Length; index++)
			{
				builders[index] = catalog.Resolve(operation, operation.Parameters[index]);
			}

			return builders;
		}
	}
}