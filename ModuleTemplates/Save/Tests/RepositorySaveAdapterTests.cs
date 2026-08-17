using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.DDDCore;

namespace Sumorin.Save.Tests
{
	[TestFixture]
	public class RepositorySaveAdapterTests
	{
		private Repository<FakeEntity> repository;
		private ISerializer serializer;
		private RepositorySaveAdapter<FakeEntity> adapter;

		[SetUp]
		public void Setup()
		{
			repository = new();
			serializer = Substitute.For<ISerializer>();
			adapter = new(repository, serializer, "entities", 0, false);
		}

		[Test]
		public void Export_WithEntities_SerializesEveryEntity()
		{
			repository.Save(new("e-1", "A"));
			repository.Save(new("e-2", "B"));
			List<FakeEntity> captured = null;
			serializer.Serialize(Arg.Do<List<FakeEntity>>(entities => captured = entities)).Returns("payload");

			var result = adapter.Export();

			result.Should().Be("payload");
			captured.Should()
					.BeEquivalentTo(
						new[]
						{
							new { Id = "e-1", Name = "A" },
							new { Id = "e-2", Name = "B" }
						}
					);
		}

		[Test]
		public void Export_WithEmptyRepository_SerializesEmptyList()
		{
			List<FakeEntity> captured = null;
			serializer.Serialize(Arg.Do<List<FakeEntity>>(entities => captured = entities)).Returns("[]");

			var result = adapter.Export();

			result.Should().Be("[]");
			captured.Should().BeEmpty();
		}

		[TestCase(null, TestName = "匯入 null 跳過不處理")]
		[TestCase("", TestName = "匯入空字串跳過不處理")]
		public void Import_WithBlankData_KeepsRepositoryUntouched(string data)
		{
			repository.Save(new("e-1", "A"));

			adapter.Import(data);

			serializer.DidNotReceive().Deserialize<List<FakeEntity>>(Arg.Any<string>());
			repository.Values.Should().BeEquivalentTo(new[] { new { Id = "e-1", Name = "A" } });
		}

		[Test]
		public void Import_WithSerializedEntities_WritesThemBackToRepository()
		{
			serializer.Deserialize<List<FakeEntity>>("payload")
					  .Returns(
						  new List<FakeEntity>
						  {
							  new("e-1", "A"),
							  new("e-2", "B")
						  }
					  );

			adapter.Import("payload");

			repository.Values.Should()
					  .BeEquivalentTo(
						  new[]
						  {
							  new { Id = "e-1", Name = "A" },
							  new { Id = "e-2", Name = "B" }
						  }
					  );
		}

		[Test]
		public void Import_WhenDeserializeReturnsNull_KeepsRepositoryUntouched()
		{
			repository.Save(new("e-1", "A"));
			serializer.Deserialize<List<FakeEntity>>("broken").Returns((List<FakeEntity>)null);

			adapter.Import("broken");

			repository.Values.Should().BeEquivalentTo(new[] { new { Id = "e-1", Name = "A" } });
		}

		[Test]
		public void Clear_WithDisposableEntities_ReleasesThemBeforeDeleting()
		{
			var disposableRepository = new Repository<DisposableEntity>();
			var disposableAdapter = new RepositorySaveAdapter<DisposableEntity>(disposableRepository, serializer, "disposables", 0, false);
			var entity = new DisposableEntity("e-1");
			disposableRepository.Save(entity);

			disposableAdapter.Clear();

			entity.Disposed.Should().BeTrue();
			disposableRepository.Count.Should().Be(0);
		}

		[Test]
		public void Clear_WithNonDisposableEntities_DeletesAll()
		{
			repository.Save(new("e-1", "A"));

			adapter.Clear();

			repository.Count.Should().Be(0);
		}
	}

	internal class FakeEntity: Entity
	{
		public string Name { get; }

		public FakeEntity(string id, string name): base(id)
		{
			Name = name;
		}
	}

	internal class DisposableEntity: Entity, IDisposable
	{
		public bool Disposed { get; private set; }

		public DisposableEntity(string id): base(id) { }

	#region IDisposable Members
		/// <inheritdoc />
		public void Dispose()
		{
			Disposed = true;
		}
	#endregion
	}
}