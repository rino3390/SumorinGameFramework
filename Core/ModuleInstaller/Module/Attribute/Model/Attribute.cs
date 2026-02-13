using System;
using System.Collections.Generic;
using Sumorin.GameFramework.DDDCore;
using UniRx;

namespace Sumorin.GameFramework.AttributeSystem
{
    /// <summary>
    /// 通用屬性實例，不知道「生命」「攻擊」等具體概念
    /// </summary>
    public class Attribute : Entity
    {
		/// <summary>
        /// 基礎值
        /// </summary>
        public int BaseValue { get; private set; }

		/// <summary>
        /// 最大值
        /// </summary>
        public int MaxValue { get; private set; }

		/// <summary>
        /// 最小值
        /// </summary>
        public int MinValue { get; private set; }

		/// <summary>
        /// 計算後的最終值
        /// </summary>
        public int Value => CalculateValue();

		/// <summary>
        /// 屬性變化事件
        /// </summary>
        public IObservable<AttributeChangedInfo> OnChanged => changed;

		/// <summary>
        /// 所有修改器（唯讀）
        /// </summary>
        public IReadOnlyList<Modifier> Modifiers => modifiers;

		/// <summary>
        /// 屬性名稱
        /// </summary>
        public string AttributeName { get; }

		/// <summary>
        /// 擁有者識別碼
        /// </summary>
        public string OwnerId { get; }

		private readonly List<Modifier> modifiers = new();
		private readonly Subject<AttributeChangedInfo> changed = new();

		/// <summary>
        /// 建立屬性實例
        /// </summary>
        /// <param name="id">唯一識別碼</param>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="baseValue">基礎值</param>
        /// <param name="minValue">最小值（預設 int.MinValue）</param>
        /// <param name="maxValue">最大值（預設 int.MaxValue）</param>
        public Attribute(string id, string ownerId, string attributeName, int baseValue, int minValue = int.MinValue, int maxValue = int.MaxValue)
            : base(id)
        {
            OwnerId = ownerId;
            AttributeName = attributeName;
            BaseValue = baseValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }

		/// <summary>
        /// 設定基礎值
        /// </summary>
        public void SetBaseValue(int value)
        {
            var oldValue = Value;
            BaseValue = value;
            NotifyIfChanged(oldValue);
        }

		/// <summary>
        /// 設定最小值
        /// </summary>
        public void SetMinValue(int value)
        {
            var oldValue = Value;
            var oldMin = MinValue;
            MinValue = value;
            if (oldMin != value || oldValue != Value)
                NotifyChanged(oldValue);
        }

		/// <summary>
        /// 設定最大值
        /// </summary>
        public void SetMaxValue(int value)
        {
            var oldValue = Value;
            var oldMax = MaxValue;
            MaxValue = value;
            if (oldMax != value || oldValue != Value)
                NotifyChanged(oldValue);
        }

		/// <summary>
        /// 新增修改器
        /// </summary>
        public void AddModifier(Modifier modifier)
        {
            var oldValue = Value;
            modifiers.Add(modifier);
            NotifyIfChanged(oldValue);
        }

		/// <summary>
        /// 透過 Id 移除特定修改器
        /// </summary>
        public void RemoveModifierById(string modifierId)
        {
            var oldValue = Value;
            modifiers.RemoveAll(m => m.Id == modifierId);
            NotifyIfChanged(oldValue);
        }

		/// <summary>
        /// 移除指定來源的所有修改器
        /// </summary>
        public void RemoveModifiersBySource(string sourceId)
        {
            var oldValue = Value;
            modifiers.RemoveAll(m => m.SourceId == sourceId);
            NotifyIfChanged(oldValue);
        }

		/// <summary>
        /// 移除第一個符合條件的修改器（用於 Buff Stack 移除）
        /// </summary>
        /// <param name="modifyType">修改類型</param>
        /// <param name="value">修改數值</param>
        /// <param name="sourceId">來源識別碼</param>
        public void RemoveFirstModifier(ModifyType modifyType, int value, string sourceId)
        {
            var oldValue = Value;
            var index = modifiers.FindIndex(m => m.ModifyType == modifyType && m.Value == value && m.SourceId == sourceId);
            if (index >= 0)
            {
                modifiers.RemoveAt(index);
                NotifyIfChanged(oldValue);
            }
        }

		private int CalculateValue()
        {
            var flat = BaseValue;
            var percent = 0f;
            var multiple = 1f;

            foreach (var mod in modifiers)
            {
                switch (mod.ModifyType)
                {
                    case ModifyType.Flat:
                        flat += mod.Value;
                        break;
                    case ModifyType.Percent:
                        percent += mod.Value;
                        break;
                    case ModifyType.Multiple:
                        multiple *= mod.Value;
                        break;
                }
            }

            var result = (flat + flat * percent / 100f) * multiple;

            // 處理 float 溢位與極端值 - 必須在 Math.Round 之前檢查
            if (float.IsInfinity(result) || float.IsNaN(result))
                return result > 0 ? MaxValue : MinValue;

			switch(result)
			{
				case >= int.MaxValue:
					return MaxValue;
				case <= int.MinValue:
					return MinValue;
				default:
				{
					var rounded = (int)Math.Round(result, MidpointRounding.AwayFromZero);
					return Math.Clamp(rounded, MinValue, MaxValue);
				}
			}
		}

		private void NotifyChanged(int oldValue)
        {
            var info = new AttributeChangedInfo
            {
                OwnerId = OwnerId,
                AttributeName = AttributeName,
                OldValue = oldValue,
                NewValue = Value,
                MinValue = MinValue,
                MaxValue = MaxValue
            };
            changed.OnNext(info);
        }

		private void NotifyIfChanged(int oldValue)
        {
            if (oldValue != Value)
                NotifyChanged(oldValue);
        }
	}
}
