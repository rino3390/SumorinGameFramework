using Rino.GameFramework.DDDCore;
using Rino.GameFramework.RinoUtility;
using System;
using System.Collections.Generic;

namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff Entity，效果容器
	/// </summary>
	public class Buff: Entity
	{
		/// <summary>
		/// Buff 名稱
		/// </summary>
		public string BuffName { get; }

		/// <summary>
		/// 擁有者識別碼
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		/// 來源識別碼（施加者）
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		/// 生命週期類型
		/// </summary>
		public LifetimeType LifetimeType { get; }

		/// <summary>
		/// 剩餘生命週期（秒或回合數）
		/// </summary>
		public float RemainingLifetime { get; private set; }

		/// <summary>
		/// 是否已過期
		/// </summary>
		public bool IsExpired => LifetimeType != LifetimeType.Permanent && RemainingLifetime <= 0 || StackCount <= 0;

		/// <summary>
		/// 最大堆疊數，-1 表示無上限
		/// </summary>
		public int MaxStack { get; }

		/// <summary>
		/// 當前堆疊數
		/// </summary>
		public int StackCount { get; private set; }

		/// <summary>
		/// Modifier 記錄列表
		/// </summary>
		public List<ModifierRecord> ModifierRecords { get; }

		/// <summary>
		/// 堆疊變化事件
		/// </summary>
		public ReactiveEvent<BuffStackChangedInfo> OnStackChanged { get; } = new();

		public Buff(string id, string buffName, string ownerId, string sourceId, int maxStack, LifetimeType lifetimeType, float lifetime): base(id)
		{
			if (string.IsNullOrEmpty(buffName)) throw new ArgumentException("BuffName cannot be null or empty.", nameof(buffName));

			if (string.IsNullOrEmpty(ownerId)) throw new ArgumentException("OwnerId cannot be null or empty.", nameof(ownerId));

			if (string.IsNullOrEmpty(sourceId)) throw new ArgumentException("SourceId cannot be null or empty.", nameof(sourceId));

			BuffName = buffName;
			OwnerId = ownerId;
			SourceId = sourceId;
			StackCount = 1;
			MaxStack = maxStack;
			LifetimeType = lifetimeType;
			RemainingLifetime = lifetime;
			ModifierRecords = new List<ModifierRecord>();
		}

		/// <summary>
		/// 變更疊層數
		/// </summary>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		public void ChangeStack(int delta)
		{
			var oldStack = StackCount;
			StackCount += delta;
			if (MaxStack >= 0)
			{
				StackCount = Math.Min(StackCount, MaxStack);
			}

			StackCount = Math.Max(0, StackCount);

			if (StackCount != oldStack)
			{
				OnStackChanged.Invoke(new BuffStackChangedInfo(Id, OwnerId, BuffName, oldStack, StackCount));
			}
		}

		/// <summary>
		/// 刷新生命週期
		/// </summary>
		/// <param name="lifetime">新的生命週期值</param>
		public void RefreshLifetime(float lifetime)
		{
			if (LifetimeType == LifetimeType.Permanent) return;

			RemainingLifetime = lifetime;
		}

		/// <summary>
		/// 調整生命週期
		/// </summary>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		public void AdjustLifetime(float delta)
		{
			if (LifetimeType == LifetimeType.Permanent) return;

			RemainingLifetime += delta;
		}

		/// <summary>
		/// 記錄 Modifier
		/// </summary>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="modifierId">Modifier 識別碼</param>
		public void RecordModifier(string attributeName, string modifierId)
		{
			ModifierRecords.Add(new ModifierRecord(attributeName, modifierId));
		}

		/// <summary>
		/// 移除最後一筆 Modifier 記錄（用於堆疊減少時，以 LIFO 順序移除對應的 Modifier）
		/// </summary>
		/// <returns>被移除的記錄，若無記錄則回傳 null</returns>
		public ModifierRecord RemoveLastModifierRecord()
		{
			if (ModifierRecords.Count == 0) return null;

			var last = ModifierRecords[^1];
			ModifierRecords.RemoveAt(ModifierRecords.Count - 1);
			return last;
		}
	}
}
