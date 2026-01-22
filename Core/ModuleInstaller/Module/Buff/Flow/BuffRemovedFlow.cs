using System;
using Rino.GameFramework.DDDCore;
using Zenject;

namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff 移除 Flow，處理 Buff 移除時清理效果
	/// </summary>
	public class BuffRemovedFlow: IInitializable, IDisposable
	{
		[Inject]
		private Subscriber subscriber;

		[Inject]
		private BuffEffectController buffEffectController;

		/// <inheritdoc />
		public void Initialize() => subscriber.Subscribe<BuffRemoved>(OnBuffRemoved);

		/// <inheritdoc />
		public void Dispose() => subscriber.UnSubscribe<BuffRemoved>(OnBuffRemoved);

		private void OnBuffRemoved(BuffRemoved evt)
		{
			buffEffectController.RemoveEffects(evt.OwnerId, evt.ModifierRecords);
		}
	}
}