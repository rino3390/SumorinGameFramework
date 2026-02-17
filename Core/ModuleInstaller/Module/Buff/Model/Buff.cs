using System;
using System.Collections.Generic;
using Sumorin.GameFramework.DDDCore;
using System.Linq;
using UniRx;

namespace Sumorin.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff Entity，效果容器
	/// </summary>
	public class Buff: Entity
	{
		/// <summary>
		/// 是否已過期
		/// </summary>
		public bool IsExpired => Config.LifetimeType != LifetimeType.Permanent && RemainingLifetime <= 0 || StackCount <= 0;

		/// <summary>
		/// Buff 配置
		/// </summary>
		public BuffConfig Config { get; }

		/// <summary>
		/// 剩餘生命週期（秒或回合數）
		/// </summary>
		public float RemainingLifetime { get; private set; }

		/// <summary>
		/// 當前堆疊數
		/// </summary>
		public int StackCount => StackRecords.Count;

		/// <summary>
		/// 擁有者識別碼
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		/// 來源識別碼（施加者）
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		/// Stack 記錄列表
		/// </summary>
		public ReactiveCollection<StackRecord> StackRecords { get; } = new();

		/// <summary>
		/// 當 Buff 過期時觸發
		/// </summary>
		public event Action OnExpired;

		/// <summary>
		/// 當 Buff 狀態變更時觸發
		/// </summary>
		public event Action OnChanged;

		/// <summary>
		/// 建立 Buff
		/// </summary>
		/// <param name="id">唯一識別碼</param>
		/// <param name="config">Buff 配置</param>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="sourceId">來源識別碼</param>
		public Buff(string id, BuffConfig config, string ownerId, string sourceId): base(id)
		{
			if(string.IsNullOrEmpty(ownerId)) throw new ArgumentException("OwnerId cannot be null or empty.", nameof(ownerId));

			if(string.IsNullOrEmpty(sourceId)) throw new ArgumentException("SourceId cannot be null or empty.", nameof(sourceId));

			Config = config;
			OwnerId = ownerId;
			SourceId = sourceId;

			RemainingLifetime = config.LifetimeType == LifetimeType.Permanent ? config.Lifetime : MathF.Max(1, config.Lifetime);

			StackRecords.Add(new StackRecord(config.Effects ?? new List<AttributeSystem.ModifyEffectInfo>()));
		}

		/// <summary>
		/// 刷新生命週期
		/// </summary>
		public void RefreshLifetime()
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;
			if(IsExpired) return;

			RemainingLifetime = Config.Lifetime;
			OnChanged?.Invoke();
		}

		/// <summary>
		/// 設定生命週期
		/// </summary>
		/// <param name="lifetime">新的生命週期值</param>
		public void SetLifetime(float lifetime)
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;
			if(IsExpired) return;

			if(Config.LifetimeType == LifetimeType.TurnBased)
			{
				lifetime = (int)lifetime;
			}

			RemainingLifetime = lifetime;
			OnChanged?.Invoke();

			if(RemainingLifetime <= 0)
			{
				HandleLifetimeExpired();
			}
		}

		/// <summary>
		/// 調整生命週期
		/// </summary>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		public void AdjustLifetime(float delta)
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;
			if(IsExpired) return;

			if(Config.LifetimeType == LifetimeType.TurnBased)
			{
				delta = (int)delta;
			}

			if(delta == 0) return;

			RemainingLifetime += delta;
			OnChanged?.Invoke();

			if(RemainingLifetime <= 0)
			{
				HandleLifetimeExpired();
			}
		}

		/// <summary>
		/// 調整堆疊數
		/// </summary>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		public void AdjustStack(int delta)
		{
			if(IsExpired) return;

			var oldCount = StackCount;

			if(delta > 0)
			{
				AddStackRecord(delta);
			}
			else if(delta < 0)
			{
				RemoveStackRecord(Math.Abs(delta));
			}

			if(oldCount != StackCount)
			{
				OnChanged?.Invoke();
			}
		}

		/// <summary>
		/// 清空所有堆疊（用於明確移除，不觸發 OnExpired）
		/// </summary>
		public void ClearStacks()
		{
			StackRecords.Clear();
		}

		private void AddStackRecord(int count = 1)
		{
			var maxStack = Config.MaxStack < 0 ? int.MaxValue : Config.MaxStack;

			for(var i = 0; i < count && StackCount < maxStack; i++)
			{
				var record = new StackRecord(Config.Effects ?? new List<AttributeSystem.ModifyEffectInfo>());
				StackRecords.Add(record);
			}
		}

		private void RemoveStackRecord(int count = 1)
		{
			for(var i = 0; i < count && StackRecords.Count > 0; i++)
			{
				StackRecords.RemoveAt(StackRecords.Count - 1);
			}

			if(StackCount <= 0)
			{
				OnExpired?.Invoke();
			}
		}

		private void HandleLifetimeExpired()
		{
			if(Config.RemoveAllOnExpire || StackCount <= 1)
			{
				OnExpired?.Invoke();
			}
			else
			{
				RemoveStackRecord();
				RemainingLifetime = Config.Lifetime;
			}
		}
	}
}