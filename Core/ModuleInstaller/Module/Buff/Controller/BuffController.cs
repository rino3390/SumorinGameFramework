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
    public class BuffController : IBuffController, IInitializable, IDisposable
    {
        private readonly IBuffRepository repository;
        private readonly Publisher publisher;

        private List<BuffConfig> configs = new();
        private readonly Dictionary<string, Subject<List<BuffInfo>>> subjects = new();
        private readonly CompositeDisposable disposables = new();

        /// <summary>
        /// 建立 BuffController
        /// </summary>
        /// <param name="repository">Buff Repository</param>
        /// <param name="publisher">事件發布者</param>
        [Inject]
        public BuffController(IBuffRepository repository, Publisher publisher)
        {
            this.repository = repository;
            this.publisher = publisher;
        }

        /// <inheritdoc />
        public void Initialize() { }

        /// <inheritdoc />
        public void Dispose()
        {
            disposables.Dispose();
        }

        /// <inheritdoc />
        public void RegisterConfigs(List<BuffConfig> configs)
        {
            this.configs = configs;
        }

        /// <inheritdoc />
        public IObservable<List<BuffInfo>> ObserveBuffs(string ownerId)
        {
            if (!subjects.ContainsKey(ownerId))
                subjects[ownerId] = new Subject<List<BuffInfo>>();
            return subjects[ownerId];
        }

        /// <inheritdoc />
        public string AddBuff(string ownerId, string buffName, string sourceId)
        {
            var config = GetConfig(buffName);

            // 檢查互斥
            if (!string.IsNullOrEmpty(config.MutualExclusionGroup))
            {
                var conflicting = repository
                    .GetByOwner(ownerId)
                    .Where(b => GetConfig(b.BuffName).MutualExclusionGroup == config.MutualExclusionGroup)
                    .ToList();

                foreach (var buff in conflicting)
                {
                    var otherConfig = GetConfig(buff.BuffName);
                    if (otherConfig.Priority > config.Priority)
                        return null;

                    if (buff.BuffName != buffName)
                        RemoveBuffInternal(buff, "Replaced");
                }
            }

            // 檢查堆疊
            var existing = repository.GetByOwner(ownerId)
                .FirstOrDefault(b => b.BuffName == buffName);

            if (existing != null)
                return HandleStacking(existing, config, sourceId);

            // 建立新 Buff
            var newBuff = new Buff(
                id: GUID.NewGuid(),
                buffName: buffName,
                ownerId: ownerId,
                sourceId: sourceId,
                maxStack: config.MaxStack,
                duration: config.Duration,
                turns: config.Turns
            );

            // 訂閱 Model 的狀態變化事件
            SubscribeTo(newBuff);

            repository.Add(newBuff);

            // 生命週期事件：Controller 直接發送
            publisher.Publish(new BuffApplied(newBuff.Id, ownerId, buffName, sourceId));

            NotifyBuffsChanged(ownerId);

            return newBuff.Id;
        }

        private void SubscribeTo(Buff buff)
        {
            // 狀態變化事件：訂閱後轉發為 DomainEvent
            buff.OnStackChanged
                .Subscribe(info => publisher.Publish(new BuffStackChanged(
                    info.BuffId, info.OwnerId, info.BuffName, info.OldStack, info.NewStack)))
                .AddTo(disposables);

            buff.OnDurationRefreshed
                .Subscribe(info => publisher.Publish(new BuffDurationRefreshed(
                    info.BuffId, info.OwnerId, info.BuffName)))
                .AddTo(disposables);
        }

        private string HandleStacking(Buff existing, BuffConfig config, string sourceId)
        {
            switch (config.StackBehavior)
            {
                case StackBehavior.Independent:
                    var newBuff = new Buff(
                        id: GUID.NewGuid(),
                        buffName: config.BuffName,
                        ownerId: existing.OwnerId,
                        sourceId: sourceId,
                        maxStack: config.MaxStack,
                        duration: config.Duration,
                        turns: config.Turns
                    );
                    SubscribeTo(newBuff);
                    repository.Add(newBuff);
                    publisher.Publish(new BuffApplied(newBuff.Id, existing.OwnerId, config.BuffName, sourceId));
                    NotifyBuffsChanged(existing.OwnerId);
                    return newBuff.Id;

                case StackBehavior.RefreshDuration:
                    existing.RefreshDuration(config.Duration);
                    existing.RefreshTurns(config.Turns);
                    return existing.Id;

                case StackBehavior.IncreaseStack:
                    if (existing.CanAddStack())
                        existing.AddStack();
                    existing.RefreshDuration(config.Duration);
                    existing.RefreshTurns(config.Turns);
                    NotifyBuffsChanged(existing.OwnerId);
                    return existing.Id;

                case StackBehavior.Replace:
                    RemoveBuffInternal(existing, "Replaced");
                    return AddBuff(existing.OwnerId, config.BuffName, sourceId);

                default:
                    return null;
            }
        }

        /// <inheritdoc />
        public void RemoveBuff(string buffId)
        {
            var buff = repository.Get(buffId);
            if (buff != null)
                RemoveBuffInternal(buff, "Manual");
        }

        /// <inheritdoc />
        public void RemoveBuffsBySource(string ownerId, string sourceId)
        {
            var buffs = repository.GetByOwner(ownerId)
                .Where(b => b.SourceId == sourceId)
                .ToList();

            foreach (var buff in buffs)
                RemoveBuffInternal(buff, "SourceRemoved");
        }

        /// <inheritdoc />
        public void RemoveBuffsByName(string ownerId, string buffName)
        {
            var buffs = repository.GetByOwner(ownerId)
                .Where(b => b.BuffName == buffName)
                .ToList();

            foreach (var buff in buffs)
                RemoveBuffInternal(buff, "Manual");
        }

        private void RemoveBuffInternal(Buff buff, string reason)
        {
            var ownerId = buff.OwnerId;
            var buffId = buff.Id;
            var buffName = buff.BuffName;
            var modifierRecords = buff.ModifierRecords.ToList();

            repository.Remove(buffId);

            // 生命週期事件：Controller 直接發送
            publisher.Publish(new BuffRemoved(buffId, ownerId, buffName, modifierRecords, reason));

            NotifyBuffsChanged(ownerId);
        }

        /// <inheritdoc />
        public void TickTime(float deltaTime)
        {
            var allBuffs = repository.GetAll().ToList();

            foreach (var buff in allBuffs)
            {
                buff.TickTime(deltaTime);

                if (buff.IsExpired)
                    RemoveBuffInternal(buff, "Expired");
            }
        }

        /// <inheritdoc />
        public void TickTurn(string ownerId)
        {
            var buffs = repository.GetByOwner(ownerId).ToList();

            foreach (var buff in buffs)
            {
                buff.TickTurn();

                if (buff.IsExpired)
                    RemoveBuffInternal(buff, "Expired");
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
        public BuffConfig GetConfig(string buffName) => configs.First(c => c.BuffName == buffName);

        private void NotifyBuffsChanged(string ownerId)
        {
            if (subjects.TryGetValue(ownerId, out var subject))
            {
                var buffs = repository.GetByOwner(ownerId)
                    .Select(b => new BuffInfo(b.Id, b.BuffName, b.StackCount, b.RemainingDuration, b.RemainingTurns))
                    .ToList();
                subject.OnNext(buffs);
            }
        }
    }
}
