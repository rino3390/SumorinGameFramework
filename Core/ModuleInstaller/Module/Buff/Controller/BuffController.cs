using System;
using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.DDDCore;
using Rino.GameFramework.RinoUtility;
using UniRx;
using Zenject;

namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff Controller 實作（資源型），管理 Buff 生命週期、堆疊、互斥邏輯
	/// </summary>
	public class BuffController: IBuffController, IInitializable, IDisposable
	{
		private readonly CompositeDisposable disposables = new();
		private readonly Dictionary<string, Subject<List<BuffInfo>>> subjects = new();
		private readonly IBuffRepository repository;

		private List<BuffConfig> configs = new();
		private readonly IPublisher publisher;

		/// <summary>
		/// 建立 BuffController
		/// </summary>
		/// <param name="repository">Buff Repository</param>
		/// <param name="publisher">事件發布者</param>
		[Inject]
		public BuffController(IBuffRepository repository, IPublisher publisher)
		{
			this.repository = repository;
			this.publisher = publisher;
		}

	#region IBuffController Members
		/// <inheritdoc />
		public void RegisterConfigs(List<BuffConfig> configs)
		{
			this.configs = configs;
		}

		/// <inheritdoc />
		public IObservable<List<BuffInfo>> ObserveBuffs(string ownerId)
		{
			if (!subjects.ContainsKey(ownerId))
			{
				subjects[ownerId] = new Subject<List<BuffInfo>>();
			}

			return subjects[ownerId];
		}

		/// <inheritdoc />
		public string AddBuff(string ownerId, string buffName, string sourceId)
		{
			var config = GetConfig(buffName);

			if (!TryHandleMutualExclusion(ownerId, config)) return null;

			var existing = repository.Find(b => b.OwnerId == ownerId && b.BuffName == buffName);

			return existing != null ? HandleStacking(existing, config, sourceId) : CreateNewBuff(ownerId, buffName, sourceId, config);
		}

		/// <inheritdoc />
		public void RemoveBuff(string buffId)
		{
			var buff = repository.Get(buffId);
			if (buff != null) RemoveBuffInternal(buff, "Manual");
		}

		/// <inheritdoc />
		public void RemoveBuffsBySource(string ownerId, string sourceId)
		{
			var buffs = repository.GetByOwner(ownerId).Where(b => b.SourceId == sourceId).ToList();

			foreach (var buff in buffs) RemoveBuffInternal(buff, "SourceRemoved");
		}

		/// <inheritdoc />
		public void RemoveBuffsByOwner(string ownerId)
		{
			var buffs = repository.GetByOwner(ownerId).ToList();

			foreach (var buff in buffs) RemoveBuffInternal(buff, "Manual");
		}

		/// <inheritdoc />
		public void RemoveBuffsByTag(string ownerId, string tag)
		{
			var buffs = repository.GetByOwner(ownerId).Where(b => GetConfig(b.BuffName).Tags.Contains(tag)).ToList();

			foreach (var buff in buffs) RemoveBuffInternal(buff, "Manual");
		}

		/// <inheritdoc />
		public void TickTime(float deltaTime)
		{
			var timeBasedBuffs = repository.Values.Where(b => b.LifetimeType == LifetimeType.TimeBased).ToList();

			foreach (var buff in timeBasedBuffs)
			{
				buff.AdjustLifetime(-deltaTime);

				if (buff.IsExpired)
				{
					RemoveBuffInternal(buff, "Expired");
				}
			}
		}

		/// <inheritdoc />
		public void TickTurn(string ownerId)
		{
			var turnBasedBuffs = repository.GetByOwner(ownerId).Where(b => b.LifetimeType == LifetimeType.TurnBased).ToList();

			foreach (var buff in turnBasedBuffs)
			{
				buff.AdjustLifetime(-1);

				if (buff.IsExpired)
				{
					RemoveBuffInternal(buff, "Expired");
				}
			}
		}

		/// <inheritdoc />
		public void RecordModifier(string buffId, string attributeName, string modifierId)
		{
			var buff = repository.Get(buffId);
			buff?.RecordModifier(attributeName, modifierId);
		}

		/// <inheritdoc />
		public ModifierRecord RemoveLastModifierRecord(string buffId)
		{
			var buff = repository.Get(buffId);
			return buff?.RemoveLastModifierRecord();
		}

		/// <inheritdoc />
		public Buff GetBuff(string buffId) => repository.Get(buffId);

		/// <inheritdoc />
		public List<Buff> GetBuffsByOwner(string ownerId) => repository.GetByOwner(ownerId).ToList();

		/// <inheritdoc />
		public void AdjustBuffLifetime(string buffId, float delta)
		{
			var buff = repository.Get(buffId);
			if (buff == null) return;

			buff.AdjustLifetime(delta);
			NotifyBuffsChanged(buff.OwnerId);
		}
	#endregion

	#region IDisposable Members
		/// <inheritdoc />
		public void Dispose()
		{
			disposables.Dispose();
		}
	#endregion

	#region IInitializable Members
		/// <inheritdoc />
		public void Initialize() { }
	#endregion

		private string CreateNewBuff(string ownerId, string buffName, string sourceId, BuffConfig config)
		{
			var newBuff = new Buff(GUID.NewGuid(), buffName, ownerId, sourceId, config.MaxStack, config.LifetimeType, config.Lifetime);

			SubscribeTo(newBuff);
			repository.Save(newBuff);
			publisher.Publish(new BuffApplied(newBuff.Id, ownerId, buffName, sourceId));
			NotifyBuffsChanged(ownerId);

			return newBuff.Id;
		}

		private BuffConfig GetConfig(string buffName) => configs.First(c => c.BuffName == buffName);

		private string HandleStacking(Buff buff, BuffConfig config, string sourceId)
		{
			switch (config.StackBehavior)
			{
				case StackBehavior.Independent:
					return CreateNewBuff(buff.OwnerId, config.BuffName, sourceId, config);

				case StackBehavior.RefreshDuration:
					buff.RefreshLifetime(config.Lifetime);
					NotifyBuffsChanged(buff.OwnerId);
					return buff.Id;

				case StackBehavior.IncreaseStack:
					buff.ChangeStack(1);
					buff.RefreshLifetime(config.Lifetime);
					NotifyBuffsChanged(buff.OwnerId);
					return buff.Id;

				case StackBehavior.Replace:
					RemoveBuffInternal(buff, "Replaced");
					return AddBuff(buff.OwnerId, config.BuffName, sourceId);

				default:
					return null;
			}
		}

		private void NotifyBuffsChanged(string ownerId)
		{
			if (!subjects.TryGetValue(ownerId, out var subject)) return;

			var buffs = repository.GetByOwner(ownerId)
								  .Select(b => new BuffInfo(b.Id, b.BuffName, b.StackCount, b.LifetimeType, b.RemainingLifetime))
								  .ToList();
			subject.OnNext(buffs);
		}

		private void RemoveBuffInternal(Buff buff, string reason)
		{
			var ownerId = buff.OwnerId;
			var buffId = buff.Id;
			var buffName = buff.BuffName;
			var modifierRecords = buff.ModifierRecords.ToList();

			repository.DeleteById(buffId);

			publisher.Publish(new BuffRemoved(buffId, ownerId, buffName, modifierRecords, reason));

			NotifyBuffsChanged(ownerId);
		}

		private void SubscribeTo(Buff buff)
		{
			// 狀態變化事件：訂閱後轉發為 DomainEvent
			buff.OnStackChanged
				.Subscribe(info => publisher.Publish(new BuffStackChanged(info.BuffId, info.OwnerId, info.BuffName, info.OldStack, info.NewStack)))
				.AddTo(disposables);
		}

		/// <summary>
		/// 處理互斥群組邏輯
		/// </summary>
		/// <returns>true 表示可以繼續新增，false 表示被更高優先級 buff 阻擋</returns>
		private bool TryHandleMutualExclusion(string ownerId, BuffConfig config)
		{
			if (string.IsNullOrEmpty(config.MutualExclusionGroup)) return true;

			var conflicting = repository.GetByOwner(ownerId).Where(b => GetConfig(b.BuffName).MutualExclusionGroup == config.MutualExclusionGroup).ToList();

			foreach (var buff in conflicting)
			{
				var otherConfig = GetConfig(buff.BuffName);
				if (otherConfig.Priority > config.Priority) return false;

				if (buff.BuffName != config.BuffName) RemoveBuffInternal(buff, "Replaced");
			}

			return true;
		}
	}
}
