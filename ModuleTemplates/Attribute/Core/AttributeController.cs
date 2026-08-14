using System;
using System.Collections.Generic;
using Sumorin.DDDCore;
using Sumorin.SumorinUtility;
using UniRx;
using Zenject;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Controller（資源型），管理屬性的建立、修改、關聯更新
	/// </summary>
	public class AttributeController: IAttributeController, IDisposable
	{
		[Inject]
		private IAttributeRepository repository;

		[Inject]
		private ConfigManager configs;

		private readonly Dictionary<string, IDisposable> subscriptions = new();

	#region IAttributeController Members
		/// <inheritdoc />
		public IReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string attributeName)
		{
			return repository.Get(ownerId, attributeName)?.Current;
		}

		/// <inheritdoc />
		public int GetValue(string ownerId, string attributeName)
		{
			return repository.Get(ownerId, attributeName)?.Value ?? 0;
		}

		/// <inheritdoc />
		public CommandResult SetBaseValue(string ownerId, string attributeName, int value)
		{
			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetBaseValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SetMinValue(string ownerId, string attributeName, int value)
		{
			var config = configs.Get<IAttributeConfig>(attributeName);
			if(!string.IsNullOrEmpty(config?.RelationMin)) return CommandResult.Fail("下限由關聯屬性決定");

			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetMinValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SetMaxValue(string ownerId, string attributeName, int value)
		{
			var config = configs.Get<IAttributeConfig>(attributeName);
			if(!string.IsNullOrEmpty(config?.RelationMax)) return CommandResult.Fail("上限由關聯屬性決定");

			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetMaxValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult AddModifier(string ownerId, ModifyEffectInfo effect, string sourceId, string description = "")
		{
			if(string.IsNullOrEmpty(sourceId)) return CommandResult.Fail("來源識別碼不得為空");

			var attribute = repository.Get(ownerId, effect.AttributeName) ?? CreateAttributeInternal(ownerId, effect.AttributeName, 0);

			var modifierId = GUID.NewGuid();
			attribute.AddModifier(new Modifier(modifierId, effect.ModifyType, effect.Value, sourceId, description));
			return CommandResult.Ok(modifierId);
		}

		/// <inheritdoc />
		public CommandResult AddModifiers(string ownerId, List<ModifyEffectInfo> effects, string sourceId, string description = "")
		{
			if(effects == null) return CommandResult.Fail("效果列表不得為 null");

			foreach(var effect in effects)
			{
				var result = AddModifier(ownerId, effect, sourceId, description);
				if(!result.IsSuccess) return result;
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifierById(string ownerId, string attributeName, string modifierId)
		{
			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.RemoveModifierById(modifierId);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifiersBySource(string ownerId, string attributeName, string sourceId)
		{
			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.RemoveModifiersBySource(sourceId);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifier(string ownerId, ModifyEffectInfo effect, string sourceId)
		{
			var attribute = repository.Get(ownerId, effect.AttributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.RemoveFirstModifier(effect.ModifyType, effect.Value, sourceId);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveAllModifiersBySource(string ownerId, string sourceId)
		{
			foreach(var attribute in repository.GetByOwnerId(ownerId))
			{
				attribute.RemoveModifiersBySource(sourceId);
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult CreateAttribute(string ownerId, string attributeName, int baseValue)
		{
			if(string.IsNullOrEmpty(attributeName)) return CommandResult.Fail("屬性名稱不得為空");

			return CommandResult.Ok(CreateAttributeInternal(ownerId, attributeName, baseValue).Id);
		}

		/// <inheritdoc />
		public CommandResult RemoveAttribute(string ownerId, string attributeName)
		{
			var attribute = repository.Get(ownerId, attributeName);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			RemoveAttributeInternal(attribute);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveAttributesByOwner(string ownerId)
		{
			foreach(var attribute in repository.GetByOwnerId(ownerId))
			{
				RemoveAttributeInternal(attribute);
			}

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
		}
	#endregion

		private Attribute CreateAttributeInternal(string ownerId, string attributeName, int baseValue)
		{
			var config = configs.Get<IAttributeConfig>(attributeName);
			var minValue = GetRelationValue(ownerId, config?.RelationMin, config?.Min ?? int.MinValue);
			var maxValue = GetRelationValue(ownerId, config?.RelationMax, config?.Max ?? int.MaxValue);

			var attribute = new Attribute(GUID.NewGuid(), ownerId, attributeName, baseValue, minValue, maxValue);
			repository.Save(attribute);
			SubscribeTo(attribute);
			UpdateDependentAttributes(ownerId, attributeName);
			return attribute;
		}

		private void RemoveAttributeInternal(Attribute attribute)
		{
			if(subscriptions.TryGetValue(attribute.Id, out var subscription))
			{
				subscription.Dispose();
				subscriptions.Remove(attribute.Id);
			}

			repository.DeleteById(attribute.Id);
			attribute.Dispose();
		}

		private void SubscribeTo(Attribute attribute)
		{
			subscriptions[attribute.Id] = attribute.Current.Skip(1).Subscribe(_ => UpdateDependentAttributes(attribute.OwnerId, attribute.AttributeName));
		}

		private void UpdateDependentAttributes(string ownerId, string sourceAttributeName)
		{
			var sourceAttribute = repository.Get(ownerId, sourceAttributeName);
			if(sourceAttribute == null) return;

			foreach(var entry in configs.GetAll<IAttributeConfig>())
			{
				var target = repository.Get(ownerId, entry.Key);

				if(target == null)
				{
					continue;
				}

				if(entry.Value.RelationMax == sourceAttributeName)
				{
					target.SetMaxValue(sourceAttribute.Value);
				}

				if(entry.Value.RelationMin == sourceAttributeName)
				{
					target.SetMinValue(sourceAttribute.Value);
				}
			}
		}

		private int GetRelationValue(string ownerId, string relationAttributeName, int defaultValue)
		{
			if(string.IsNullOrEmpty(relationAttributeName)) return defaultValue;

			return repository.Get(ownerId, relationAttributeName)?.Value ?? defaultValue;
		}
	}
}