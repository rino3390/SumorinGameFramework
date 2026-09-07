using System;
using System.Collections.Generic;
using System.Linq;
using Sumorin.Attribute;
using Sumorin.DDDCore;
using Sumorin.SumorinUtility;
using UniRx;
using Zenject;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Controller（資源型），管理生命週期、堆疊、互斥與屬性效果掛卸
	/// </summary>
	public class BuffController: IBuffController, IDisposable
	{
		[Inject]
		private IBuffRepository repository;

		[Inject]
		private IAttributeController attributeController;

		[Inject]
		private ConfigManager configs;

		[Inject]
		private IPublisher publisher;

		private readonly Dictionary<string, CompositeDisposable> subscriptions = new();
		private readonly List<Buff> timedBuffs = new();

	#region IBuffController Members
		/// <inheritdoc />
		public IReadOnlyReactiveProperty<int> ObserveStackCount(string buffId) => repository.Get(buffId)?.Stack;

		/// <inheritdoc />
		public IReadOnlyReactiveProperty<float> ObserveLifetime(string buffId) => repository.Get(buffId)?.Lifetime;

		/// <inheritdoc />
		public BuffInfo? GetBuffInfo(string buffId)
		{
			var buff = repository.Get(buffId);
			return buff == null ? null : ToInfo(buff);
		}

		/// <inheritdoc />
		public List<BuffInfo> GetBuffInfosByOwner(string ownerId)
		{
			return repository.GetByOwner(ownerId).Select(ToInfo).ToList();
		}

		/// <inheritdoc />
		public CommandResult AddBuff(string ownerId, string configId, string sourceId)
		{
			if(string.IsNullOrEmpty(ownerId)) return CommandResult.Fail("擁有者 Id 不得為空");

			var config = configs.Get<IBuffConfig>(configId);
			if(config == null) return CommandResult.Fail($"找不到 Buff 配置：{configId}");

			if(!IsAllowedByMutualExclusion(ownerId, configId, config)) return CommandResult.Fail("被同群組的高優先度 Buff 擋下");

			var existing = repository.Find(buff => buff.OwnerId == ownerId && buff.ConfigId == configId);

			return existing == null ? CommandResult.Ok(CreateNewBuff(ownerId, sourceId, config).Id) : HandleStacking(existing, sourceId);
		}

		/// <inheritdoc />
		public CommandResult RemoveBuff(string buffId)
		{
			var buff = repository.Get(buffId);
			if(buff == null) return CommandResult.Fail("Buff 不存在");

			RemoveBuffInternal(buff, BuffRemoveReason.Manual);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveBuffsBySource(string ownerId, string sourceId)
		{
			return RemoveMatching(ownerId, buff => buff.SourceId == sourceId, BuffRemoveReason.SourceRemoved);
		}

		/// <inheritdoc />
		public CommandResult RemoveBuffsByOwner(string ownerId)
		{
			return RemoveMatching(ownerId, _ => true, BuffRemoveReason.Manual);
		}

		/// <inheritdoc />
		public CommandResult RemoveBuffsByTag(string ownerId, string tag)
		{
			return RemoveMatching(ownerId, buff => buff.Config.Tags.Contains(tag), BuffRemoveReason.TagRemoved);
		}

		/// <inheritdoc />
		public CommandResult TickTime(float deltaTime)
		{
			if(timedBuffs.Count == 0) return CommandResult.Ok();

			// 倒序走訪：到期的 Buff 會在推進當下把自己移出集合
			for(var i = timedBuffs.Count - 1; i >= 0; i--)
			{
				timedBuffs[i].AdjustLifetime(-deltaTime);
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult TickTurn(string ownerId, int turns = 1)
		{
			var turnBasedBuffs = repository.GetByOwner(ownerId).Where(buff => buff.Config.LifetimeType == LifetimeType.TurnBased).ToList();

			foreach(var buff in turnBasedBuffs)
			{
				buff.AdjustLifetime(-turns);
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult AdjustBuffLifetime(string buffId, float delta)
		{
			var buff = repository.Get(buffId);
			if(buff == null) return CommandResult.Fail("Buff 不存在");

			buff.AdjustLifetime(delta);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SetBuffLifetime(string buffId, float lifetime)
		{
			var buff = repository.Get(buffId);
			if(buff == null) return CommandResult.Fail("Buff 不存在");

			buff.SetLifetime(lifetime);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult AddStack(string buffId) => AdjustStack(buffId, 1);

		/// <inheritdoc />
		public CommandResult RemoveStack(string buffId) => AdjustStack(buffId, -1);

		/// <inheritdoc />
		public CommandResult AdjustStack(string buffId, int delta)
		{
			var buff = repository.Get(buffId);
			if(buff == null) return CommandResult.Fail("Buff 不存在");

			buff.AdjustStack(delta);
			return CommandResult.Ok();
		}
	#endregion

	#region IDisposable Members
		/// <inheritdoc />
		public void Dispose()
		{
			foreach(var subscription in subscriptions.Values)
			{
				subscription.Dispose();
			}

			subscriptions.Clear();
			timedBuffs.Clear();
		}
	#endregion

		private static BuffInfo ToInfo(Buff buff) => new(buff.Id, buff.ConfigId, buff.StackCount, buff.Config.LifetimeType, buff.RemainingLifetime);

		private Buff CreateNewBuff(string ownerId, string sourceId, IBuffConfig config)
		{
			var buff = new Buff(GUID.NewGuid(), config, ownerId, sourceId);

			// 建構時已有第一層，先掛上效果再訂閱，避免施加當下多發一次層數變化事實
			ApplyStackEffects(buff, buff.StackRecords[0]);
			SubscribeTo(buff);

			repository.Save(buff);

			if(config.LifetimeType == LifetimeType.TimeBased)
			{
				timedBuffs.Add(buff);
			}

			publisher.Publish(new BuffApplied(buff.Id, ownerId, config.Id, sourceId));

			return buff;
		}

		private CommandResult HandleStacking(Buff buff, string sourceId)
		{
			switch(buff.Config.StackBehavior)
			{
				case StackBehavior.Independent:
					return CommandResult.Ok(CreateNewBuff(buff.OwnerId, sourceId, buff.Config).Id);

				case StackBehavior.RefreshDuration:
					buff.RefreshLifetime();
					return CommandResult.Ok(buff.Id);

				case StackBehavior.IncreaseStack:
					buff.AdjustStack(1);
					buff.RefreshLifetime();
					return CommandResult.Ok(buff.Id);

				case StackBehavior.Replace:
					var ownerId = buff.OwnerId;
					var config = buff.Config;
					RemoveBuffInternal(buff, BuffRemoveReason.Replaced);
					return CommandResult.Ok(CreateNewBuff(ownerId, sourceId, config).Id);

				default:
					return CommandResult.Fail($"未支援的堆疊行為：{buff.Config.StackBehavior}");
			}
		}

		private CommandResult RemoveMatching(string ownerId, Func<Buff, bool> predicate, BuffRemoveReason reason)
		{
			var targets = repository.GetByOwner(ownerId).Where(predicate).ToList();

			foreach(var buff in targets)
			{
				RemoveBuffInternal(buff, reason);
			}

			return CommandResult.Ok();
		}

		private void RemoveBuffInternal(Buff buff, BuffRemoveReason reason)
		{
			var ownerId = buff.OwnerId;
			var buffId = buff.Id;
			var configId = buff.ConfigId;

			// 清空層數會觸發 Reset，連帶撤除這個 Buff 掛在屬性上的所有 Modifier
			buff.ClearStacks();

			if(subscriptions.TryGetValue(buffId, out var subscription))
			{
				subscription.Dispose();
				subscriptions.Remove(buffId);
			}

			repository.DeleteById(buffId);
			timedBuffs.Remove(buff);
			buff.Dispose();

			publisher.Publish(new BuffRemoved(buffId, ownerId, configId, reason));
		}

		private void SubscribeTo(Buff buff)
		{
			var disposables = new CompositeDisposable();

			buff.OnExpired.Subscribe(_ => RemoveBuffInternal(buff, BuffRemoveReason.Expired)).AddTo(disposables);
			buff.StackRecords.ObserveAdd().Subscribe(e => HandleStackAdded(buff, e.Value)).AddTo(disposables);
			buff.StackRecords.ObserveRemove().Subscribe(e => HandleStackRemoved(buff, e.Value)).AddTo(disposables);
			buff.StackRecords.ObserveReset().Subscribe(_ => HandleStacksCleared(buff)).AddTo(disposables);

			subscriptions[buff.Id] = disposables;
		}

		private bool IsAllowedByMutualExclusion(string ownerId, string configId, IBuffConfig config)
		{
			if(string.IsNullOrEmpty(config.MutualExclusionGroup)) return true;

			var conflicting = repository.GetByOwner(ownerId).Where(buff => buff.Config.MutualExclusionGroup == config.MutualExclusionGroup).ToList();

			foreach(var buff in conflicting)
			{
				if(buff.Config.Priority > config.Priority) return false;

				if(buff.ConfigId != configId)
				{
					RemoveBuffInternal(buff, BuffRemoveReason.Replaced);
				}
			}

			return true;
		}

		private void ApplyStackEffects(Buff buff, StackRecord record)
		{
			attributeController.AddModifiers(buff.OwnerId, record.Effects, buff.Id, buff.ConfigId);
		}

		private void HandleStackAdded(Buff buff, StackRecord record)
		{
			ApplyStackEffects(buff, record);
			publisher.Publish(new BuffStackChanged(buff.Id, buff.OwnerId, buff.ConfigId, buff.StackCount - 1, buff.StackCount));
		}

		private void HandleStackRemoved(Buff buff, StackRecord record)
		{
			foreach(var effect in record.Effects)
			{
				attributeController.RemoveModifier(buff.OwnerId, effect, buff.Id);
			}

			publisher.Publish(new BuffStackChanged(buff.Id, buff.OwnerId, buff.ConfigId, buff.StackCount + 1, buff.StackCount));
		}

		private void HandleStacksCleared(Buff buff)
		{
			attributeController.RemoveAllModifiersBySource(buff.OwnerId, buff.Id);
		}
	}
}