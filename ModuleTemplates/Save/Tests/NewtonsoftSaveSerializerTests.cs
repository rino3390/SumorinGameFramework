using FluentAssertions;
using NUnit.Framework;
using R3;
using Sumorin.DDDCore;

namespace Sumorin.Save.Tests
{
	[TestFixture]
	public class NewtonsoftSaveSerializerTests
	{
		private NewtonsoftSaveSerializer serializer;

		[SetUp]
		public void Setup()
		{
			serializer = new();
		}

		[Test]
		public void Serialize_WithValueFacetMember_WritesPlainValue()
		{
			var result = serializer.Serialize(new FacetEntity("e-1", "Poison", 3));

			result.Should().Contain("\"Stack\":3");
			result.Should().NotContain("HasValue");
		}

		[Test]
		public void Deserialize_AfterSerialize_RestoresReadOnlyMembersAndValueFacet()
		{
			var data = serializer.Serialize(new FacetEntity("e-1", "Poison", 3));

			var result = serializer.Deserialize<FacetEntity>(data);

			result.Should()
				  .BeEquivalentTo(
					  new
					  {
						  Id = "e-1",
						  Name = "Poison"
					  }
				  );
			result.Stack.CurrentValue.Should().Be(3);
		}

		[Test]
		public void Deserialize_WithValueFacetConstructorParameter_RebuildsTheValueFacet()
		{
			var data = serializer.Serialize(new FacetParameterEntity("e-1", new ReactiveProperty<int>(3)));

			var result = serializer.Deserialize<FacetParameterEntity>(data);

			result.Stack.CurrentValue.Should().Be(3);
		}

		[TestCase(null, TestName = "null 回傳 null")]
		[TestCase("", TestName = "空字串回傳 null")]
		[TestCase("not json", TestName = "無法解析的內容回傳 null")]
		public void Deserialize_WithUnusableData_ReturnsNull(string data)
		{
			serializer.Deserialize<FacetEntity>(data).Should().BeNull();
		}
	}

	internal class FacetEntity: Entity
	{
		public string Name { get; }

		public ReadOnlyReactiveProperty<int> Stack => stack;

		private readonly ReactiveProperty<int> stack;

		public FacetEntity(string id, string name, int stack): base(id)
		{
			Name = name;
			this.stack = new(stack);
		}
	}

	internal class FacetParameterEntity: Entity
	{
		public ReadOnlyReactiveProperty<int> Stack { get; }

		public FacetParameterEntity(string id, ReadOnlyReactiveProperty<int> stack): base(id)
		{
			Stack = stack;
		}
	}
}