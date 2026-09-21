using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板的建置入口
	/// </summary>
	/// <remarks>
	///     結果提示播放期間暫時打開 <see cref="Application.runInBackground" />。
	///     Unity 預設在視窗失焦時整個暫停，不打開的話提示會卡在半路。
	///     這是整個遊戲的設定，所以由 Presenter 管，面板只負責播完時通知。
	/// </remarks>
	public class GmPresenter: IGmPresenter, IDisposable
	{
		[Inject]
		private GmPanelView panel;

		[Inject]
		private GmFieldCatalog catalog;

		[Inject]
		private GmOperationRunner runner;

		private bool keepingRunInBackground;
		private bool runInBackgroundBefore;

	#region IDisposable Members
		/// <inheritdoc />
		/// <remarks>
		///     換場景時 GM 的 scope 會連同面板一起銷毀，提示播到一半的話要在這裡把設定還回去。
		/// </remarks>
		public void Dispose() => RestoreRunInBackground();
	#endregion

	#region IGmPresenter Members
		/// <inheritdoc />
		public void BuildPanel(IReadOnlyList<GmOperation> operations)
		{
			var layouts = new List<GmOperationLayout>(operations.Count);

			foreach(var operation in operations)
			{
				layouts.Add(new(operation, BuildersFor(operation), arguments => Execute(operation, arguments)));
			}

			panel.Build(layouts, RestoreRunInBackground);
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

		// 提示還在播時又執行一次，面板會直接換成新提示。只在第一次記下原值，否則會把「已被打開」當成原值還回去
		private string Execute(GmOperation operation, object[] arguments)
		{
			if(!keepingRunInBackground)
			{
				runInBackgroundBefore = Application.runInBackground;
				keepingRunInBackground = true;
			}

			Application.runInBackground = true;
			return runner.Run(operation, arguments);
		}

		private void RestoreRunInBackground()
		{
			if(!keepingRunInBackground) return;

			Application.runInBackground = runInBackgroundBefore;
			keepingRunInBackground = false;
		}
	}
}