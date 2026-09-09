using System;
using ObservableCollections;
using R3;
using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Entity，效果容器
	/// </summary>
	public class Buff: Entity, IDisposable
	{
		/// <summary>
		///     是否已過期
		/// </summary>
		public bool IsExpired => (Config.LifetimeType != LifetimeType.Permanent && RemainingLifetime <= 0) || StackCount <= 0;

		/// <summary>
		///     Buff 配置
		/// </summary>
		public IBuffConfig Config { get; }

		/// <summary>
		///     Buff 配置 Id，同時是配置的查找鍵
		/// </summary>
		public string ConfigId => Config.Id;

		/// <summary>
		///     剩餘時間（秒或回合數）
		/// </summary>
		public float RemainingLifetime => remainingLifetime.Value;

		/// <summary>
		///     剩餘時間的值，Tick 期間每幀更新且不產生額外記憶體
		/// </summary>
		public ReadOnlyReactiveProperty<float> Lifetime => remainingLifetime;

		/// <summary>
		///     當前層數
		/// </summary>
		/// <remarks>
		///     直接讀集合，集合變動的回呼中取得的就是即時值。
		///     <see cref="Stack" /> 是給 View 的值面，於整批增減完成後才更新。
		/// </remarks>
		public int StackCount => StackRecords.Count;

		/// <summary>
		///     當前層數的值
		/// </summary>
		public ReadOnlyReactiveProperty<int> Stack => stackCount;

		/// <summary>
		///     擁有者 Id
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		///     來源 Id（施加者）
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		///     層數紀錄，供 Controller 訂閱增減以掛卸 Modifier
		/// </summary>
		public ObservableList<StackRecord> StackRecords { get; } = new();

		/// <summary>
		///     Buff 過期時發出
		/// </summary>
		public Observable<Unit> OnExpired => expired;

		private readonly Subject<Unit> expired = new();
		private readonly ReactiveProperty<float> remainingLifetime = new();
		private readonly ReactiveProperty<int> stackCount = new();

		/// <summary>
		///     建立 Buff
		/// </summary>
		/// <param name="id">唯一 Id</param>
		/// <param name="config">Buff 配置</param>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="sourceId">來源 Id</param>
		/// <exception cref="ArgumentNullException">config 為 null 時拋出</exception>
		/// <exception cref="ArgumentException">config.Id、ownerId 或 sourceId 為 null 或空字串時拋出</exception>
		public Buff(string id, IBuffConfig config, string ownerId, string sourceId): base(id)
		{
			if(config == null)
			{
				throw new ArgumentNullException(nameof(config));
			}

			if(string.IsNullOrEmpty(config.Id))
			{
				throw new ArgumentException("Config.Id cannot be null or empty.", nameof(config));
			}

			if(string.IsNullOrEmpty(ownerId))
			{
				throw new ArgumentException("OwnerId cannot be null or empty.", nameof(ownerId));
			}

			if(string.IsNullOrEmpty(sourceId))
			{
				throw new ArgumentException("SourceId cannot be null or empty.", nameof(sourceId));
			}

			Config = config;
			OwnerId = ownerId;
			SourceId = sourceId;

			remainingLifetime.Value = config.LifetimeType == LifetimeType.Permanent ? config.Lifetime : MathF.Max(1, config.Lifetime);

			StackRecords.Add(new StackRecord(config.Effects));
			SyncStackCount();
		}

	#region IDisposable Members
		/// <inheritdoc />
		public void Dispose()
		{
			expired.Dispose();
			remainingLifetime.Dispose();
			stackCount.Dispose();
		}
	#endregion

		/// <summary>
		///     重置時間至配置值，Permanent 類型不受影響
		/// </summary>
		public void RefreshLifetime()
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;

			if(IsExpired) return;

			remainingLifetime.Value = Config.Lifetime;
		}

		/// <summary>
		///     設定剩餘時效，Permanent 類型不受影響
		/// </summary>
		/// <param name="lifetime">新的時效值（單位依 LifetimeType 決定為秒或回合）</param>
		public void SetLifetime(float lifetime)
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;

			if(IsExpired) return;

			if(Config.LifetimeType == LifetimeType.TurnBased)
			{
				lifetime = (int)lifetime;
			}

			remainingLifetime.Value = lifetime;

			if(RemainingLifetime <= 0)
			{
				HandleLifetimeExpired();
			}
		}

		/// <summary>
		///     調整剩餘時效，Permanent 類型不受影響
		/// </summary>
		/// <param name="delta">變更量（正數延長，負數縮短）</param>
		public void AdjustLifetime(float delta)
		{
			if(Config.LifetimeType == LifetimeType.Permanent) return;

			if(IsExpired) return;

			if(Config.LifetimeType == LifetimeType.TurnBased)
			{
				delta = (int)delta;
			}

			if(delta == 0) return;

			remainingLifetime.Value += delta;

			if(RemainingLifetime <= 0)
			{
				HandleLifetimeExpired();
			}
		}

		/// <summary>
		///     調整層數，受配置的層數上限與 0 下限限制
		/// </summary>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		public void AdjustStack(int delta)
		{
			if(IsExpired) return;

			if(delta > 0)
			{
				AddStackRecord(delta);
			}
			else if(delta < 0)
			{
				RemoveStackRecord(Math.Abs(delta));
			}

			// 過期必須是最後一個通知，訂閱者收到後會移除並釋放本實例
			if(StackCount <= 0)
			{
				expired.OnNext(Unit.Default);
			}
		}

		/// <summary>
		///     清空所有層數（用於明確移除，不觸發過期）
		/// </summary>
		public void ClearStacks()
		{
			StackRecords.Clear();
			SyncStackCount();
		}

		private void AddStackRecord(int count)
		{
			var maxStack = Config.MaxStack < 0 ? int.MaxValue : Config.MaxStack;

			for(var i = 0; i < count && StackRecords.Count < maxStack; i++)
			{
				StackRecords.Add(new StackRecord(Config.Effects));
			}

			SyncStackCount();
		}

		private void RemoveStackRecord(int count)
		{
			for(var i = 0; i < count && StackRecords.Count > 0; i++)
			{
				StackRecords.RemoveAt(StackRecords.Count - 1);
			}

			SyncStackCount();
		}

		private void SyncStackCount()
		{
			stackCount.Value = StackRecords.Count;
		}

		private void HandleLifetimeExpired()
		{
			if(Config.RemoveAllOnExpire || StackCount <= 1)
			{
				expired.OnNext(Unit.Default);
				return;
			}

			RemoveStackRecord(1);
			remainingLifetime.Value = Config.Lifetime;
		}
	}
}