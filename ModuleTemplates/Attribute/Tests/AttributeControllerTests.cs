using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Sumorin.DDDCore;
using Zenject;

namespace Sumorin.Attribute.Tests
{
	[TestFixture]
	public class AttributeControllerTests: ZenjectUnitTestFixture
	{
		private class FakeAttributeConfig: IAttributeConfig
		{
			public int Min { get; }
			public int Max { get; }
			public string RelationMin { get; }
			public string RelationMax { get; }
			public int Ratio { get; }

			public FakeAttributeConfig(int min, int max, string relationMin = "", string relationMax = "", int ratio = 1)
			{
				Min = min;
				Max = max;
				RelationMin = relationMin;
				RelationMax = relationMax;
				Ratio = ratio;
			}
		}

		private static readonly ModifyEffectInfo HealthPlus50 = new()
		{
			AttributeName = "Health",
			ModifyType = ModifyType.Flat,
			Value = 50
		};

		private AttributeRepository repository;

		[SetUp]
		public override void Setup()
		{
			base.Setup();
			repository = new AttributeRepository();
		}

		private AttributeController CreateController(Dictionary<string, IConfig> configs = null)
		{
			Container.Bind<IAttributeRepository>().FromInstance(repository);
			Container.Bind<ConfigManager>().FromInstance(new ConfigManager(configs ?? DefaultConfigs()));
			return Container.Instantiate<AttributeController>();
		}

		private static Dictionary<string, IConfig> DefaultConfigs() =>
			new()
			{
				["Health"] = new FakeAttributeConfig(0, 999),
				["Attack"] = new FakeAttributeConfig(0, 999),
				["CritRate"] = new FakeAttributeConfig(0, 100, ratio: 100)
			};

		[Test]
		public void CreateAttribute_WithRegisteredConfig_AppliesConfiguredBoundary()
		{
			var controller = CreateController();

			var result = controller.CreateAttribute("owner-1", "CritRate", 20);

			result.IsSuccess.Should().BeTrue();
			repository.Get("owner-1", "CritRate")
					  .Should()
					  .BeEquivalentTo(
						  new
						  {
							  Id = result.Value,
							  BaseValue = 20,
							  MinValue = 0,
							  MaxValue = 100
						  }
					  );
		}

		[Test]
		public void CreateAttribute_WithoutConfig_UsesIntBoundary()
		{
			var controller = CreateController(new Dictionary<string, IConfig>());

			controller.CreateAttribute("owner-1", "Unregistered", 100);

			repository.Get("owner-1", "Unregistered")
					  .Should()
					  .BeEquivalentTo(
						  new
						  {
							  int.MinValue,
							  int.MaxValue
						  }
					  );
		}

		[TestCase(null, TestName = "屬性名稱為 null 時失敗")]
		[TestCase("", TestName = "屬性名稱為空字串時失敗")]
		public void CreateAttribute_WithoutAttributeName_Fails(string attributeName)
		{
			var controller = CreateController();

			controller.CreateAttribute("owner-1", attributeName, 100).IsSuccess.Should().BeFalse();
		}

		[Test]
		public void GetValue_WithExistingAttribute_ReturnsCurrentValue()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.GetValue("owner-1", "Health").Should().Be(100);
		}

		[Test]
		public void GetValue_WithUnknownAttribute_ReturnsZero()
		{
			CreateController().GetValue("owner-1", "Health").Should().Be(0);
		}

		[Test]
		public void SetBaseValue_WithExistingAttribute_UpdatesValue()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.SetBaseValue("owner-1", "Health", 150).IsSuccess.Should().BeTrue();
			controller.GetValue("owner-1", "Health").Should().Be(150);
		}

		[Test]
		public void SetMinValue_WithoutRelation_UpdatesBoundary()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.SetMinValue("owner-1", "Health", 150).IsSuccess.Should().BeTrue();
			controller.GetValue("owner-1", "Health").Should().Be(150);
		}

		[Test]
		public void SetMaxValue_WithoutRelation_UpdatesBoundary()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.SetMaxValue("owner-1", "Health", 50).IsSuccess.Should().BeTrue();
			controller.GetValue("owner-1", "Health").Should().Be(50);
		}

		[Test]
		public void SetMaxValue_WithRelationMax_FailsAndKeepsBoundary()
		{
			var controller = CreateController(
				new Dictionary<string, IConfig>
				{
					["Health"] = new FakeAttributeConfig(0, 9999, relationMax: "MaxHealth"),
					["MaxHealth"] = new FakeAttributeConfig(1, 9999)
				}
			);
			controller.CreateAttribute("owner-1", "MaxHealth", 100);
			controller.CreateAttribute("owner-1", "Health", 80);

			controller.SetMaxValue("owner-1", "Health", 50).IsSuccess.Should().BeFalse();
			repository.Get("owner-1", "Health").MaxValue.Should().Be(100);
		}

		[Test]
		public void SetMinValue_WithRelationMin_FailsAndKeepsBoundary()
		{
			var controller = CreateController(
				new Dictionary<string, IConfig>
				{
					["Shield"] = new FakeAttributeConfig(0, 9999, "MinShield"),
					["MinShield"] = new FakeAttributeConfig(0, 9999)
				}
			);
			controller.CreateAttribute("owner-1", "MinShield", 10);
			controller.CreateAttribute("owner-1", "Shield", 50);

			controller.SetMinValue("owner-1", "Shield", 30).IsSuccess.Should().BeFalse();
			repository.Get("owner-1", "Shield").MinValue.Should().Be(10);
		}

		[Test]
		public void SetBaseValue_OnRelationSource_UpdatesDependentMaxValue()
		{
			var controller = CreateController(
				new Dictionary<string, IConfig>
				{
					["Health"] = new FakeAttributeConfig(0, 9999, relationMax: "MaxHealth"),
					["MaxHealth"] = new FakeAttributeConfig(1, 9999)
				}
			);
			controller.CreateAttribute("owner-1", "Health", 80);
			controller.CreateAttribute("owner-1", "MaxHealth", 100);
			repository.Get("owner-1", "Health").MaxValue.Should().Be(100);

			controller.SetBaseValue("owner-1", "MaxHealth", 150);

			repository.Get("owner-1", "Health").MaxValue.Should().Be(150);
		}

		[Test]
		public void SetBaseValue_OnRelationSource_UpdatesDependentMinValue()
		{
			var controller = CreateController(
				new Dictionary<string, IConfig>
				{
					["Shield"] = new FakeAttributeConfig(0, 9999, "MinShield"),
					["MinShield"] = new FakeAttributeConfig(0, 9999)
				}
			);
			controller.CreateAttribute("owner-1", "MinShield", 0);
			controller.CreateAttribute("owner-1", "Shield", 50);
			repository.Get("owner-1", "Shield").MinValue.Should().Be(0);

			controller.SetBaseValue("owner-1", "MinShield", 30);

			repository.Get("owner-1", "Shield").MinValue.Should().Be(30);
		}

		[Test]
		public void AddModifier_WithExistingAttribute_ReturnsModifierIdAndAppliesEffect()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			var result = controller.AddModifier("owner-1", HealthPlus50, "source-1");

			result.Value.Should().NotBeNullOrEmpty();
			controller.GetValue("owner-1", "Health").Should().Be(150);
		}

		[Test]
		public void AddModifier_WithUnknownAttribute_CreatesAttributeFirst()
		{
			var controller = CreateController();

			controller.AddModifier("owner-1", HealthPlus50, "source-1");

			controller.GetValue("owner-1", "Health").Should().Be(50);
		}

		[TestCase(null, TestName = "來源為 null 時失敗")]
		[TestCase("", TestName = "來源為空字串時失敗")]
		public void AddModifier_WithoutSourceId_Fails(string sourceId)
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.AddModifier("owner-1", HealthPlus50, sourceId).IsSuccess.Should().BeFalse();
			controller.GetValue("owner-1", "Health").Should().Be(100);
		}

		[Test]
		public void AddModifiers_WithMultipleEffects_AppliesAll()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			controller.CreateAttribute("owner-1", "Attack", 50);

			var result = controller.AddModifiers(
				"owner-1",
				new List<ModifyEffectInfo>
				{
					HealthPlus50,
					new() { AttributeName = "Attack", ModifyType = ModifyType.Flat, Value = 10 }
				},
				"buff-1"
			);

			result.IsSuccess.Should().BeTrue();
			controller.GetValue("owner-1", "Health").Should().Be(150);
			controller.GetValue("owner-1", "Attack").Should().Be(60);
		}

		[Test]
		public void AddModifiers_WithEmptyList_Succeeds()
		{
			var controller = CreateController();

			controller.AddModifiers("owner-1", new List<ModifyEffectInfo>(), "buff-1").IsSuccess.Should().BeTrue();
		}

		[Test]
		public void RemoveModifierById_WithReturnedId_RemovesThatModifier()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			var modifierId = controller.AddModifier("owner-1", HealthPlus50, "source-1").Value;

			controller.RemoveModifierById("owner-1", "Health", modifierId).IsSuccess.Should().BeTrue();
			controller.GetValue("owner-1", "Health").Should().Be(100);
		}

		[Test]
		public void RemoveModifiersBySource_WithMixedSources_RemovesOnlyThatSource()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			controller.AddModifier("owner-1", HealthPlus50, "sword-1");
			controller.AddModifier("owner-1", HealthPlus50, "shield-1");

			controller.RemoveModifiersBySource("owner-1", "Health", "sword-1");

			controller.GetValue("owner-1", "Health").Should().Be(150);
		}

		[Test]
		public void RemoveModifier_WithDuplicateEffects_RemovesOnlyOne()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			controller.AddModifier("owner-1", HealthPlus50, "buff-1");
			controller.AddModifier("owner-1", HealthPlus50, "buff-1");

			controller.RemoveModifier("owner-1", HealthPlus50, "buff-1");

			controller.GetValue("owner-1", "Health").Should().Be(150);
		}

		[Test]
		public void RemoveAllModifiersBySource_AcrossAttributes_RemovesEverythingFromSource()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			controller.CreateAttribute("owner-1", "Attack", 50);
			controller.AddModifiers(
				"owner-1",
				new List<ModifyEffectInfo>
				{
					HealthPlus50,
					new() { AttributeName = "Attack", ModifyType = ModifyType.Flat, Value = 10 }
				},
				"buff-1"
			);

			controller.RemoveAllModifiersBySource("owner-1", "buff-1").IsSuccess.Should().BeTrue();

			controller.GetValue("owner-1", "Health").Should().Be(100);
			controller.GetValue("owner-1", "Attack").Should().Be(50);
		}

		[Test]
		public void RemoveAttribute_WithExistingAttribute_RemovesFromRepository()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.RemoveAttribute("owner-1", "Health").IsSuccess.Should().BeTrue();
			repository.Get("owner-1", "Health").Should().BeNull();
		}

		[Test]
		public void RemoveAttributesByOwner_WithMultipleOwners_RemovesOnlyThatOwner()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);
			controller.CreateAttribute("owner-1", "Attack", 50);
			controller.CreateAttribute("owner-2", "Health", 200);

			controller.RemoveAttributesByOwner("owner-1").IsSuccess.Should().BeTrue();

			repository.GetByOwnerId("owner-1").Should().BeEmpty();
			controller.GetValue("owner-2", "Health").Should().Be(200);
		}

		[Test]
		public void ObserveAttribute_WithExistingAttribute_ExposesCurrentSnapshot()
		{
			var controller = CreateController();
			controller.CreateAttribute("owner-1", "Health", 100);

			controller.ObserveAttribute("owner-1", "Health").Value.Should().Be(new AttributeValueInfo(100, 0, 999));
		}

		[Test]
		public void ObserveAttribute_WithUnknownAttribute_ReturnsNull()
		{
			CreateController().ObserveAttribute("owner-1", "Health").Should().BeNull();
		}

		[Test]
		public void SetBaseValue_OnUnknownAttribute_FailsInsteadOfIgnoring()
		{
			CreateController().SetBaseValue("owner-1", "Health", 1).IsSuccess.Should().BeFalse();
		}
	}
}