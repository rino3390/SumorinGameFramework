using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.AttributeSystem;
using Rino.GameFramework.RinoUtility;
using Zenject;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 效果 Controller（協調型），處理效果施加/移除，由 Flow 呼叫
    /// </summary>
    public class BuffEffectController
    {
        [Inject] private IAttributeController attributeController;
        [Inject] private IBuffController buffController;

        private List<BuffConfig> configs = new();

        /// <summary>
        /// 註冊 Buff 配置
        /// </summary>
        /// <param name="configs">配置列表</param>
        public void RegisterConfigs(List<BuffConfig> configs)
        {
            this.configs = configs;
        }

        /// <summary>
        /// 套用 Buff 效果
        /// </summary>
        /// <param name="buffId">Buff 識別碼</param>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="buffName">Buff 名稱</param>
        public void ApplyEffects(string buffId, string ownerId, string buffName)
        {
            var config = GetConfig(buffName);

            foreach (var effect in config.Effects)
            {
                var modifierId = GUID.NewGuid();
                var modifier = new Modifier(
                    id: modifierId,
                    modifyType: effect.ModifyType,
                    value: effect.Value,
                    sourceId: buffId,
                    description: $"{buffName}"
                );

                attributeController.AddModifier(ownerId, effect.AttributeName, modifier);
                buffController.RecordModifier(buffId, effect.AttributeName, modifierId);
            }
        }

        /// <summary>
        /// 移除 Buff 效果
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="modifierRecords">Modifier 記錄列表</param>
        public void RemoveEffects(string ownerId, List<ModifierRecord> modifierRecords)
        {
            foreach (var record in modifierRecords)
            {
                attributeController.RemoveModifierById(ownerId, record.AttributeName, record.ModifierId);
            }
        }

        /// <summary>
        /// 堆疊增加時處理
        /// </summary>
        /// <param name="buffId">Buff 識別碼</param>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="buffName">Buff 名稱</param>
        /// <param name="addedStacks">增加的堆疊數</param>
        public void OnStackIncreased(string buffId, string ownerId, string buffName, int addedStacks)
        {
            var config = GetConfig(buffName);

            for (var i = 0; i < addedStacks; i++)
            {
                foreach (var effect in config.Effects)
                {
                    var modifierId = GUID.NewGuid();
                    var modifier = new Modifier(
                        id: modifierId,
                        modifyType: effect.ModifyType,
                        value: effect.Value,
                        sourceId: buffId,
                        description: $"{buffName}"
                    );

                    attributeController.AddModifier(ownerId, effect.AttributeName, modifier);
                    buffController.RecordModifier(buffId, effect.AttributeName, modifierId);
                }
            }
        }

        /// <summary>
        /// 堆疊減少時處理
        /// </summary>
        /// <param name="buffId">Buff 識別碼</param>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="removedStacks">減少的堆疊數</param>
        public void OnStackDecreased(string buffId, string ownerId, int removedStacks)
        {
            var config = GetConfig(buffController.GetBuff(buffId).BuffName);
            var effectCount = config.Effects.Count;
            var modifiersToRemove = removedStacks * effectCount;

            for (var i = 0; i < modifiersToRemove; i++)
            {
                var record = buffController.RemoveLastModifierRecord(buffId);
                if (record != null)
                    attributeController.RemoveModifierById(ownerId, record.AttributeName, record.ModifierId);
            }
        }

        private BuffConfig GetConfig(string buffName) => configs.First(c => c.BuffName == buffName);
    }
}
