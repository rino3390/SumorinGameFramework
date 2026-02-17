using System;
using System.Collections.Generic;
using System.Linq;
using Sumorin.GameFramework.AttributeSystem;
using Sumorin.GameFramework.DDDCore;
using Sumorin.GameFramework.SumorinUtility;
using UniRx;
using Zenject;

namespace Sumorin.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff Controller 實作（資源型），管理 Buff 生命週期、堆疊、互斥邏輯與效果套用
	/// </summary>
	public class BuffController: IBuffController, IInitializable, IDisposable
	{
		private readonly Dictionary<string, Subject<List<BuffInfo>>> subjects = new();
		private readonly IAttributeController attributeController;
		private readonly IBuffRepository repository;
		private readonly IPublisher publisher;

		private List<BuffConfig> configs = new();

		/// <summary>
		/// 建立 BuffController
		/// </summary>
		/// <param name="repository">Buff Repository</param>
		/// <param name="publisher">事件發布者</param>
		/// <param name="attributeController">屬性 Controller</param>
		[Inject]
		public BuffController(IBuffRepository repository, IPublisher publisher, IAttributeController attributeController)
		{
			this.repository = repository;
			this.publisher = publisher;
			this.attributeController = attributeController;
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
			if(!subjects.ContainsKey(ownerId))
			{
				subjects[ownerId] = new Subject<List<BuffInfo>>();
			}

			return subjects[ownerId];
		}

		/// <inheritdoc />
		public string AddBuff(string ownerId, string buffName, string sourceId)
		{
			var config = GetConfig(buffName);

			if(!IsAllowedByMutualExclusion(ownerId, config)) return null;

			var existing = repository.Find(b => b.OwnerId == ownerId && b.Config.BuffName == buffName);

			return existing != null ? HandleStacking(existing, config, sourceId) : CreateNewBuff(ownerId, sourceId, config);
		}

		/// <inheritdoc />
		public void RemoveBuff(string buffId)
		{
			var buff = repository.Get(buffId);
			if(buff != null) RemoveBuffInternal(buff, "Manual");
		}

		/// <inheritdoc />
		public void RemoveBuffsBySource(string ownerId, string sourceId)
		{
			var buffs = repository.GetByOwner(ownerId).Where(b => b.SourceId == sourceId).ToList();

			foreach(var buff in buffs) RemoveBuffInternal(buff, "SourceRemoved");
		}

		/// <inheritdoc />
		public void RemoveBuffsByOwner(string ownerId)
		{
			var buffs = repository.GetByOwner(ownerId).ToList();

			foreach(var buff in buffs) RemoveBuffInternal(buff, "Manual");
		}

		/// <inheritdoc />
		public void RemoveBuffsByTag(string ownerId, string tag)
		{
			var buffs = repository.GetByOwner(ownerId).Where(b => b.Config.Tags.Contains(tag)).ToList();

			foreach(var buff in buffs) RemoveBuffInternal(buff, "TagRemoved");
		}

		/// <inheritdoc />
		public void TickTime(float deltaTime)
		{
			var timeBasedBuffs = repository.Values.Where(b => b.Config.LifetimeType == LifetimeType.TimeBased).ToList();

			foreach(var buff in timeBasedBuffs)
			{
				buff.AdjustLifetime(-deltaTime);
			}
		}

		/// <inheritdoc />
		public void TickTurn(string ownerId, int turns = 1)
		{
			var turnBasedBuffs = repository.GetByOwner(ownerId).Where(b => b.Config.LifetimeType == LifetimeType.TurnBased).ToList();

			foreach(var buff in turnBasedBuffs)
			{
				buff.AdjustLifetime(-turns);
			}
		}

		/// <inheritdoc />
		public Buff GetBuff(string buffId) => repository.Get(buffId);

		/// <inheritdoc />
		public List<Buff> GetBuffsByOwner(string ownerId) => repository.GetByOwner(ownerId).ToList();

		/// <inheritdoc />
		public void AdjustBuffLifetime(string buffId, float delta)
		{
			var buff = repository.Get(buffId);
			buff?.AdjustLifetime(delta);
		}

		/// <inheritdoc />
		public void SetBuffLifetime(string buffId, float lifetime)
		{
			var buff = repository.Get(buffId);
			buff?.SetLifetime(lifetime);
		}

		/// <inheritdoc />
		public void AddStack(string buffId) => AdjustStack(buffId, 1);

		/// <inheritdoc />
		public void RemoveStack(string buffId) => AdjustStack(buffId, -1);

		/// <inheritdoc />
		public void AdjustStack(string buffId, int delta)
		{
			var buff = repository.Get(buffId);
			buff?.AdjustStack(delta);
		}
	#endregion

	#region IDisposable Members
		/// <inheritdoc />
		public void Dispose() { }
	#endregion

	#region IInitializable Members
		/// <inheritdoc />
		public void Initialize() { }
	#endregion

		private string CreateNewBuff(string ownerId, string sourceId, BuffConfig config)
		{
			var buffId = GUID.NewGuid();
			var newBuff = new Buff(buffId, config, ownerId, sourceId);

			newBuff.OnExpired += () => HandleExpiration(newBuff);
			newBuff.OnChanged += () => NotifyBuffsChanged(newBuff.OwnerId);

			// 訂閱 stack 變化（StartWith 處理建構時已存在的 stack）
			newBuff.StackRecords
				.ObserveAdd()
				.StartWith(newBuff.StackRecords.Select((v, i) => new CollectionAddEvent<StackRecord>(i, v)))
				.Subscribe(e => HandleStackAdded(newBuff, e.Value));
			newBuff.StackRecords
				.ObserveRemove()
				.Subscribe(e => HandleStackRemoved(newBuff, e.Value));
			newBuff.StackRecords
				.ObserveReset()
				.Subscribe(_ => HandleStacksCleared(newBuff));

			repository.Save(newBuff);
			publisher.Publish(new BuffApplied(newBuff.Id, ownerId, config.BuffName, sourceId));
			NotifyBuffsChanged(ownerId);

			return newBuff.Id;
		}

		private string HandleStacking(Buff buff, BuffConfig config, string sourceId)
		{
			switch(config.StackBehavior)
			{
				case StackBehavior.Independent:
					return CreateNewBuff(buff.OwnerId, sourceId, config);

				case StackBehavior.RefreshDuration:
					buff.RefreshLifetime();
					return buff.Id;

				case StackBehavior.IncreaseStack:
					AddStack(buff.Id);
					buff.RefreshLifetime();
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
			if(!subjects.TryGetValue(ownerId, out var subject)) return;

			var buffs = repository.GetByOwner(ownerId)
								  .Select(b => new BuffInfo(b.Id, b.Config.BuffName, b.StackCount, b.Config.LifetimeType, b.RemainingLifetime))
								  .ToList();
			subject.OnNext(buffs);
		}

		private void RemoveBuffInternal(Buff buff, string reason)
		{
			var ownerId = buff.OwnerId;
			var buffId = buff.Id;
			var buffName = buff.Config.BuffName;
			var stackRecords = buff.StackRecords.ToList();

			buff.ClearStacks();
			repository.DeleteById(buffId);

			publisher.Publish(new BuffRemoved(buffId, ownerId, buffName, stackRecords, reason));
			NotifyBuffsChanged(ownerId);
		}

		/// <summary>
		/// 處理互斥群組邏輯，檢查是否允許新增 Buff
		/// </summary>
		/// <returns>true 表示可以繼續新增，false 表示被更高優先級 buff 阻擋</returns>
		private bool IsAllowedByMutualExclusion(string ownerId, BuffConfig config)
		{
			if(string.IsNullOrEmpty(config.MutualExclusionGroup)) return true;

			var conflicting = repository.GetByOwner(ownerId).Where(b => b.Config.MutualExclusionGroup == config.MutualExclusionGroup).ToList();

			foreach(var buff in conflicting)
			{
				if(buff.Config.Priority > config.Priority) return false;

				if(buff.Config.BuffName != config.BuffName) RemoveBuffInternal(buff, "Replaced");
			}

			return true;
		}

		private BuffConfig GetConfig(string buffName)
		{
			return configs.First(c => c.BuffName == buffName);
		}

		private void HandleExpiration(Buff buff)
		{
			RemoveBuffInternal(buff, "Expired");
		}

		private void HandleStackAdded(Buff buff, StackRecord record)
		{
			attributeController.AddModifiers(buff.OwnerId, record.Effects, buff.Id, buff.Config.BuffName);

			var oldStack = buff.StackCount - 1;
			publisher.Publish(new BuffStackChanged(buff.Id, buff.OwnerId, buff.Config.BuffName, oldStack, buff.StackCount));
		}

		private void HandleStackRemoved(Buff buff, StackRecord record)
		{
			foreach(var effect in record.Effects)
			{
				attributeController.RemoveModifier(buff.OwnerId, effect, buff.Id);
			}

			var oldStack = buff.StackCount + 1;
			publisher.Publish(new BuffStackChanged(buff.Id, buff.OwnerId, buff.Config.BuffName, oldStack, buff.StackCount));
		}

		private void HandleStacksCleared(Buff buff)
		{
			attributeController.RemoveAllModifiersBySource(buff.OwnerId, buff.Id);
		}
	}
}