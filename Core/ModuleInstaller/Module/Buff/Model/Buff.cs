using System;
using System.Collections.Generic;
using Rino.GameFramework.DDDCore;
using UniRx;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff Entity，效果容器
    /// </summary>
    public class Buff : Entity
    {
        private readonly Subject<BuffStackChangedInfo> stackChangedSubject = new();
        private readonly Subject<BuffDurationRefreshedInfo> durationRefreshedSubject = new();

        /// <summary>
        /// 堆疊變化事件
        /// </summary>
        public IObservable<BuffStackChangedInfo> OnStackChanged => stackChangedSubject;

        /// <summary>
        /// 時間刷新事件
        /// </summary>
        public IObservable<BuffDurationRefreshedInfo> OnDurationRefreshed => durationRefreshedSubject;

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
        /// 當前堆疊數
        /// </summary>
        public int StackCount { get; private set; }

        /// <summary>
        /// 最大堆疊數，null 表示無上限
        /// </summary>
        public int? MaxStack { get; }

        /// <summary>
        /// 剩餘持續時間（秒），null 表示永久
        /// </summary>
        public float? RemainingDuration { get; private set; }

        /// <summary>
        /// 剩餘回合數，null 表示永久
        /// </summary>
        public int? RemainingTurns { get; private set; }

        /// <summary>
        /// Modifier 記錄列表
        /// </summary>
        public List<ModifierRecord> ModifierRecords { get; }

        /// <summary>
        /// 是否已過期
        /// </summary>
        public bool IsExpired =>
            (RemainingDuration.HasValue && RemainingDuration <= 0) ||
            (RemainingTurns.HasValue && RemainingTurns <= 0);

        public Buff(
            string id,
            string buffName,
            string ownerId,
            string sourceId,
            int? maxStack,
            float? duration,
            int? turns)
            : base(id)
        {
            if (string.IsNullOrEmpty(buffName))
                throw new ArgumentException("BuffName cannot be null or empty.", nameof(buffName));

            if (string.IsNullOrEmpty(ownerId))
                throw new ArgumentException("OwnerId cannot be null or empty.", nameof(ownerId));

            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentException("SourceId cannot be null or empty.", nameof(sourceId));

            BuffName = buffName;
            OwnerId = ownerId;
            SourceId = sourceId;
            StackCount = 1;
            MaxStack = maxStack;
            RemainingDuration = duration;
            RemainingTurns = turns;
            ModifierRecords = new List<ModifierRecord>();
        }

        /// <summary>
        /// 是否可以增加堆疊
        /// </summary>
        public bool CanAddStack() => !MaxStack.HasValue || StackCount < MaxStack.Value;

        /// <summary>
        /// 增加堆疊數
        /// </summary>
        /// <param name="count">增加數量</param>
        public void AddStack(int count = 1)
        {
            var oldStack = StackCount;
            StackCount += count;

            if (MaxStack.HasValue)
                StackCount = Math.Min(StackCount, MaxStack.Value);

            if (StackCount != oldStack)
            {
                stackChangedSubject.OnNext(new BuffStackChangedInfo(
                    Id, OwnerId, BuffName, oldStack, StackCount));
            }
        }

        /// <summary>
        /// 減少堆疊數
        /// </summary>
        /// <param name="count">減少數量</param>
        public void RemoveStack(int count = 1)
        {
            var oldStack = StackCount;
            StackCount = Math.Max(0, StackCount - count);

            if (StackCount != oldStack)
            {
                stackChangedSubject.OnNext(new BuffStackChangedInfo(
                    Id, OwnerId, BuffName, oldStack, StackCount));
            }
        }

        /// <summary>
        /// 刷新持續時間
        /// </summary>
        /// <param name="duration">新的持續時間</param>
        public void RefreshDuration(float? duration)
        {
            RemainingDuration = duration;
            durationRefreshedSubject.OnNext(new BuffDurationRefreshedInfo(
                Id, OwnerId, BuffName));
        }

        /// <summary>
        /// 刷新回合數
        /// </summary>
        /// <param name="turns">新的回合數</param>
        public void RefreshTurns(int? turns)
        {
            RemainingTurns = turns;
        }

        /// <summary>
        /// 時間流逝
        /// </summary>
        /// <param name="deltaTime">經過的時間（秒）</param>
        public void TickTime(float deltaTime)
        {
            if (RemainingDuration.HasValue)
                RemainingDuration -= deltaTime;
        }

        /// <summary>
        /// 回合流逝
        /// </summary>
        /// <param name="count">經過的回合數</param>
        public void TickTurn(int count = 1)
        {
            if (RemainingTurns.HasValue)
                RemainingTurns -= count;
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
        /// 移除最後一筆 Modifier 記錄
        /// </summary>
        /// <returns>被移除的記錄，若無記錄則回傳 null</returns>
        public ModifierRecord RemoveLastModifierRecord()
        {
            if (ModifierRecords.Count == 0)
                return null;

            var last = ModifierRecords[^1];
            ModifierRecords.RemoveAt(ModifierRecords.Count - 1);
            return last;
        }
    }
}
