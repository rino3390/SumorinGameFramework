using System;
using Rino.GameFramework.DDDCore;
using Zenject;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 施加 Flow，處理 Buff 施加時套用效果
    /// </summary>
    public class BuffAppliedFlow : IInitializable, IDisposable
    {
        [Inject] private Subscriber subscriber;
        [Inject] private BuffEffectController buffEffectController;

        /// <inheritdoc />
        public void Initialize() => subscriber.Subscribe<BuffApplied>(OnBuffApplied);

        /// <inheritdoc />
        public void Dispose() => subscriber.UnSubscribe<BuffApplied>(OnBuffApplied);

        private void OnBuffApplied(BuffApplied evt)
        {
            buffEffectController.ApplyEffects(evt.BuffId, evt.OwnerId, evt.BuffName);
        }
    }
}
