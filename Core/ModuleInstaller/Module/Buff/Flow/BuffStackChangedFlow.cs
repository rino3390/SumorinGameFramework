using System;
using Rino.GameFramework.DDDCore;
using Zenject;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 堆疊變化 Flow，處理層數變化時增減效果
    /// </summary>
    public class BuffStackChangedFlow : IInitializable, IDisposable
    {
        [Inject] private Subscriber subscriber;
        [Inject] private BuffEffectController buffEffectController;

        /// <inheritdoc />
        public void Initialize() => subscriber.Subscribe<BuffStackChanged>(OnBuffStackChanged);

        /// <inheritdoc />
        public void Dispose() => subscriber.UnSubscribe<BuffStackChanged>(OnBuffStackChanged);

        private void OnBuffStackChanged(BuffStackChanged evt)
        {
            var diff = evt.NewStack - evt.OldStack;

            if (diff > 0)
                buffEffectController.OnStackIncreased(evt.BuffId, evt.OwnerId, evt.BuffName, diff);
            else if (diff < 0)
                buffEffectController.OnStackDecreased(evt.BuffId, evt.OwnerId, Math.Abs(diff));
        }
    }
}
