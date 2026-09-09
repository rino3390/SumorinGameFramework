using System;
using System.Collections.Generic;
using R3;
using Sumorin.DDDCore;
using Sumorin.SumorinUtility;
using VContainer;

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
		public ReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string configId)
		{
			return repository.Get(ownerId, configId)?.Current;
		}

		/// <inheritdoc />
		public int GetValue(string ownerId, string configId)
		{
			return repository.Get(ownerId, configId)?.Value ?? 0;
		}

		/// <inheritdoc />
		public int GetMaxValue(string ownerId, string configId)
		{
			return repository.Get(ownerId, configId)?.MaxValue ?? 0;
		}

		/// <inheritdoc />
		public int GetMinValue(string ownerId, string configId)
		{
			return repository.Get(ownerId, configId)?.MinValue ?? 0;
		}

		/// <inheritdoc />
		public CommandResult SetBaseValue(string ownerId, string configId, int value)
		{
			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetBaseValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult AdjustBaseValue(string ownerId, string configId, int delta)
		{
			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.AdjustBaseValue(delta);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SetMinValue(string ownerId, string configId, int value)
		{
			var config = configs.Get<IAttributeConfig>(configId);
			if(!string.IsNullOrEmpty(config?.RelationMin)) return CommandResult.Fail("下限由關聯屬性決定");

			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetMinValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SetMaxValue(string ownerId, string configId, int value)
		{
			var config = configs.Get<IAttributeConfig>(configId);
			if(!string.IsNullOrEmpty(config?.RelationMax)) return CommandResult.Fail("上限由關聯屬性決定");

			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.SetMaxValue(value);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult AddModifier(string ownerId, ModifyEffectInfo effect, string sourceId, string description = "")
		{
			if(string.IsNullOrEmpty(sourceId)) return CommandResult.Fail("來源 Id 不得為空");
			if(IsResource(effect.AttributeConfigId)) return CommandResult.Fail($"資源型屬性不接受修改器：{effect.AttributeConfigId}");

			var attribute = repository.Get(ownerId, effect.AttributeConfigId) ?? CreateAttributeInternal(ownerId, effect.AttributeConfigId, 0);

			var modifierId = GUID.NewGuid();
			attribute.AddModifier(new Modifier(modifierId, effect.ModifyType, effect.Value, sourceId, description));
			return CommandResult.Ok(modifierId);
		}

		/// <inheritdoc />
		public CommandResult AddModifiers(string ownerId, List<ModifyEffectInfo> effects, string sourceId, string description = "")
		{
			if(effects == null) return CommandResult.Fail("效果列表不得為 null");

			// 先整批檢查再套用，避免前幾筆已掛上、後面才失敗的半套用狀態
			var resourceTarget = effects.Find(effect => IsResource(effect.AttributeConfigId));
			if(!string.IsNullOrEmpty(resourceTarget.AttributeConfigId)) return CommandResult.Fail($"資源型屬性不接受修改器：{resourceTarget.AttributeConfigId}");

			foreach(var effect in effects)
			{
				var result = AddModifier(ownerId, effect, sourceId, description);
				if(!result.IsSuccess) return result;
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifierById(string ownerId, string configId, string modifierId)
		{
			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.RemoveModifierById(modifierId);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifiersBySource(string ownerId, string configId, string sourceId)
		{
			var attribute = repository.Get(ownerId, configId);
			if(attribute == null) return CommandResult.Fail("屬性不存在");

			attribute.RemoveModifiersBySource(sourceId);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult RemoveModifier(string ownerId, ModifyEffectInfo effect, string sourceId)
		{
			var attribute = repository.Get(ownerId, effect.AttributeConfigId);
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
		public CommandResult CreateAttribute(string ownerId, string configId, int baseValue)
		{
			if(string.IsNullOrEmpty(configId)) return CommandResult.Fail("屬性配置 Id 不得為空");

			return CommandResult.Ok(CreateAttributeInternal(ownerId, configId, baseValue).Id);
		}

		/// <inheritdoc />
		public CommandResult RemoveAttribute(string ownerId, string configId)
		{
			var attribute = repository.Get(ownerId, configId);
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

		private Attribute CreateAttributeInternal(string ownerId, string configId, int baseValue)
		{
			var config = configs.Get<IAttributeConfig>(configId);
			var minValue = GetRelationValue(ownerId, config?.RelationMin, config?.Min ?? int.MinValue);
			var maxValue = GetRelationValue(ownerId, config?.RelationMax, config?.Max ?? int.MaxValue);

			var attribute = new Attribute(GUID.NewGuid(), ownerId, configId, baseValue, minValue, maxValue);
			repository.Save(attribute);
			SubscribeTo(attribute);
			UpdateDependentAttributes(ownerId, configId);
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
			subscriptions[attribute.Id] = attribute.Current.Skip(1).Subscribe(_ => UpdateDependentAttributes(attribute.OwnerId, attribute.ConfigId));
		}

		private void UpdateDependentAttributes(string ownerId, string sourceConfigId)
		{
			var sourceAttribute = repository.Get(ownerId, sourceConfigId);
			if(sourceAttribute == null) return;

			foreach(var entry in configs.GetAll<IAttributeConfig>())
			{
				var target = repository.Get(ownerId, entry.Key);

				if(target == null)
				{
					continue;
				}

				if(entry.Value.RelationMax == sourceConfigId)
				{
					target.SetMaxValue(sourceAttribute.Value);
				}

				if(entry.Value.RelationMin == sourceConfigId)
				{
					target.SetMinValue(sourceAttribute.Value);
				}
			}
		}

		private bool IsResource(string configId)
		{
			return configs.Get<IAttributeConfig>(configId)?.Kind == AttributeKind.Resource;
		}

		private int GetRelationValue(string ownerId, string relationConfigId, int defaultValue)
		{
			if(string.IsNullOrEmpty(relationConfigId)) return defaultValue;

			return repository.Get(ownerId, relationConfigId)?.Value ?? defaultValue;
		}
	}
}