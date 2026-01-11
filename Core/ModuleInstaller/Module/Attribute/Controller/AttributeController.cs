using System;
using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.Core.AttributeSystem.Common;
using Rino.GameFramework.Core.AttributeSystem.Model;
using Rino.GameFramework.Core.AttributeSystem.Repository;
using Rino.GameFramework.Core.RinoUtility.Misc;
using UniRx;
using Attribute = Rino.GameFramework.Core.AttributeSystem.Model.Attribute;

namespace Rino.GameFramework.Core.AttributeSystem.Controller
{
    /// <summary>
    /// 屬性 Controller（資源型），管理屬性的建立、修改、關聯更新
    /// </summary>
    public class AttributeController : IAttributeController
    {
		private readonly Dictionary<(string, string), ReactiveProperty<AttributeChangedInfo>> observablesByOwnerAndName = new();
		private readonly Dictionary<string, IDisposable> attributeSubscriptions = new();
		private readonly Dictionary<string, Subject<AttributeChangedInfo>> observablesByName = new();
		private readonly IAttributeRepository repository;
		private readonly List<AttributeConfig> configs = new();

		/// <summary>
        /// 建立 AttributeController
        /// </summary>
        /// <param name="repository">屬性 Repository</param>
        public AttributeController(IAttributeRepository repository)
        {
            this.repository = repository;
        }

	#region IAttributeController Members
		/// <inheritdoc />
        public IObservable<AttributeChangedInfo> ObserveAttribute(string ownerId, string attributeName)
        {
            var key = (ownerId, attributeName);
            if (observablesByOwnerAndName.TryGetValue(key, out var existing))
                return existing;

            var attribute = repository.Get(ownerId, attributeName);
            if (attribute == null)
                return Observable.Empty<AttributeChangedInfo>();

            var initialInfo = new AttributeChangedInfo
            {
                OwnerId = ownerId,
                AttributeName = attributeName,
                OldValue = attribute.Value,
                NewValue = attribute.Value,
                MinValue = attribute.MinValue,
                MaxValue = attribute.MaxValue
            };
            observablesByOwnerAndName[key] = new ReactiveProperty<AttributeChangedInfo>(initialInfo);
            return observablesByOwnerAndName[key];
        }

		/// <inheritdoc />
        public IObservable<AttributeChangedInfo> ObserveAttribute(string attributeName)
        {
            if (!observablesByName.ContainsKey(attributeName))
                observablesByName[attributeName] = new Subject<AttributeChangedInfo>();
            return observablesByName[attributeName];
        }

		/// <inheritdoc />
        public int GetValue(string ownerId, string attributeName)
        {
            var attribute = repository.Get(ownerId, attributeName);
            return attribute?.Value ?? 0;
        }

		/// <inheritdoc />
        public void SetBaseValue(string ownerId, string attributeName, int value)
        {
            var attribute = repository.Get(ownerId, attributeName);

            attribute?.SetBaseValue(value);
        }

		/// <inheritdoc />
        public void SetMinValue(string ownerId, string attributeName, int value)
        {
            var config = configs.FirstOrDefault(c => c.AttributeName == attributeName);
            if (!string.IsNullOrEmpty(config.RelationMin)) return;

            var attribute = repository.Get(ownerId, attributeName);
            attribute?.SetMinValue(value);
        }

		/// <inheritdoc />
        public void SetMaxValue(string ownerId, string attributeName, int value)
        {
            var config = configs.FirstOrDefault(c => c.AttributeName == attributeName);
            if (!string.IsNullOrEmpty(config.RelationMax)) return;

            var attribute = repository.Get(ownerId, attributeName);
            attribute?.SetMaxValue(value);
        }

		/// <inheritdoc />
        public void AddModifier(string ownerId, string attributeName, Modifier modifier)
        {
            var attribute = repository.Get(ownerId, attributeName) ?? CreateAttribute(ownerId, attributeName, 0);

            attribute.AddModifier(modifier);
        }

		/// <inheritdoc />
        public void RemoveModifierById(string ownerId, string attributeName, string modifierId)
        {
            var attribute = repository.Get(ownerId, attributeName);

			attribute?.RemoveModifierById(modifierId);
        }

		/// <inheritdoc />
        public void RemoveModifiersBySource(string ownerId, string attributeName, string sourceId)
        {
            var attribute = repository.Get(ownerId, attributeName);

			attribute?.RemoveModifiersBySource(sourceId);
        }

		/// <inheritdoc />
        public Attribute CreateAttribute(string ownerId, string attributeName, int baseValue)
        {
            var config = configs.FirstOrDefault(c => c.AttributeName == attributeName);
            var minValue = GetRelationValue(ownerId, config.RelationMin, config.Min);
            var maxValue = GetRelationValue(ownerId, config.RelationMax, config.Max);

            var attribute = new Attribute(
                GUID.NewGuid(),
                ownerId,
                attributeName,
                baseValue,
                minValue,
                maxValue
            );
            repository.Save(attribute);
            SubscribeToAttributeChanges(attribute);
            UpdateDependentAttributes(ownerId, attributeName);
            return attribute;
        }

		/// <inheritdoc />
        public void RemoveAttribute(string ownerId, string attributeName)
        {
            var attribute = repository.Get(ownerId, attributeName);
            if (attribute == null) return;

            RemoveAttributeInternal(attribute);
        }

		/// <inheritdoc />
        public void RemoveAttributesByOwner(string ownerId)
        {
            var attributes = repository.GetByOwnerId(ownerId);
            foreach (var attribute in attributes)
            {
                RemoveAttributeInternal(attribute);
            }
        }
	#endregion

		/// <summary>
        /// 註冊屬性配置，供自動建立屬性時使用
        /// </summary>
        /// <param name="configs">屬性配置清單</param>
        public void RegisterConfigs(List<AttributeConfig> configs)
        {
            this.configs.Clear();
            this.configs.AddRange(configs);
        }

		private void NotifyAttributeChanged(AttributeChangedInfo info)
        {
            if (observablesByOwnerAndName.TryGetValue((info.OwnerId, info.AttributeName), out var property))
                property.Value = info;

            if (observablesByName.TryGetValue(info.AttributeName, out var nameSubject))
                nameSubject.OnNext(info);
        }

		private void RemoveAttributeInternal(Attribute attribute)
        {
            UnsubscribeFromAttributeChanges(attribute);
            repository.DeleteById(attribute.Id);
        }

		private void SubscribeToAttributeChanges(Attribute attribute)
        {
            var subscription = attribute.OnChanged.Subscribe(info =>
            {
                NotifyAttributeChanged(info);
                UpdateDependentAttributes(info.OwnerId, info.AttributeName);
            });
            attributeSubscriptions[attribute.Id] = subscription;
        }

		private void TryUpdateDependentAttribute(string ownerId, string attributeName, int sourceValue, Action<Attribute, int> updateAction)
        {
            var target = repository.Get(ownerId, attributeName);
            if (target == null) return;

            updateAction(target, sourceValue);
        }

		private void UnsubscribeFromAttributeChanges(Attribute attribute)
		{
			if (attributeSubscriptions.TryGetValue(attribute.Id, out var subscription))
			{
				subscription.Dispose();
				attributeSubscriptions.Remove(attribute.Id);
			}

			var key = (attribute.OwnerId, attribute.AttributeName);
			if (observablesByOwnerAndName.TryGetValue(key, out var property))
			{
				property.Dispose();
				observablesByOwnerAndName.Remove(key);
			}
		}

		private void UpdateDependentAttributes(string ownerId, string sourceAttributeName)
        {
            var sourceAttribute = repository.Get(ownerId, sourceAttributeName);
            if (sourceAttribute == null) return;

			foreach (var config in configs)
            {
                if (config.RelationMax == sourceAttributeName)
                    TryUpdateDependentAttribute(ownerId, config.AttributeName, sourceAttribute.Value, (attr, val) => attr.SetMaxValue(val));

                if (config.RelationMin == sourceAttributeName)
                    TryUpdateDependentAttribute(ownerId, config.AttributeName, sourceAttribute.Value, (attr, val) => attr.SetMinValue(val));
            }
        }

		private int GetRelationValue(string ownerId, string relationAttributeName, int defaultValue)
        {
            if (string.IsNullOrEmpty(relationAttributeName))
                return defaultValue;

            var relationAttribute = repository.Get(ownerId, relationAttributeName);
            return relationAttribute?.Value ?? defaultValue;
        }
	}
}
