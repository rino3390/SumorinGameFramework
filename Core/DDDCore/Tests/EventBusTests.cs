using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePipe;
using NUnit.Framework;
using Zenject;

namespace Rino.GameFramework.DDDCore.Tests
{
    [TestFixture]
    public class EventBusTests : ZenjectUnitTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            var options = Container.BindMessagePipe();
            Container.BindMessageBroker<TestEvent>(options);
            Container.Bind<IEventBus>().To<EventBus>().AsSingle();
            Container.Bind<Publisher>().AsSingle();
            Container.Bind<Subscriber>().AsSingle();
        }

        #region Happy Path

        [Test]
        public void Publish_WhenSubscribed_SubscriberReceivesEvent()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            TestEvent receivedEvent = null;

            subscriber.Subscribe<TestEvent>(evt => receivedEvent = evt);
            var testEvent = new TestEvent { Value = 42 };

            // Act
            publisher.Publish(testEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.Value, Is.EqualTo(42));
        }

        [Test]
        public async Task PublishAsync_WhenSubscribedAsync_SubscriberHandlesWithUniTask()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            TestEvent receivedEvent = null;

            subscriber.SubscribeAsync<TestEvent>(async evt =>
            {
                await UniTask.Delay(10);
                receivedEvent = evt;
            });
            var testEvent = new TestEvent { Value = 100 };

            // Act
            await publisher.PublishAsync(testEvent);

            // Assert
            Assert.That(receivedEvent, Is.Not.Null);
            Assert.That(receivedEvent.Value, Is.EqualTo(100));
        }

        [Test]
        public void Publish_WithMultipleSubscribers_AllSubscribersReceiveEvent()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            TestEvent receivedEvent1 = null;
            TestEvent receivedEvent2 = null;
            TestEvent receivedEvent3 = null;

            subscriber.Subscribe<TestEvent>(evt => receivedEvent1 = evt);
            subscriber.Subscribe<TestEvent>(evt => receivedEvent2 = evt);
            subscriber.Subscribe<TestEvent>(evt => receivedEvent3 = evt);
            var testEvent = new TestEvent { Value = 77 };

            // Act
            publisher.Publish(testEvent);

            // Assert
            Assert.That(receivedEvent1, Is.Not.Null);
            Assert.That(receivedEvent2, Is.Not.Null);
            Assert.That(receivedEvent3, Is.Not.Null);
            Assert.That(receivedEvent1.Value, Is.EqualTo(77));
            Assert.That(receivedEvent2.Value, Is.EqualTo(77));
            Assert.That(receivedEvent3.Value, Is.EqualTo(77));
        }

        [Test]
        public void Publish_WithFilter_OnlyMatchingSubscribersReceiveEvent()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            TestEvent receivedByFiltered = null;
            TestEvent receivedByUnfiltered = null;

            subscriber.Subscribe<TestEvent>(evt => receivedByFiltered = evt, evt => evt.Value > 50);
            subscriber.Subscribe<TestEvent>(evt => receivedByUnfiltered = evt);

            var lowValueEvent = new TestEvent { Value = 30 };

            // Act
            publisher.Publish(lowValueEvent);

            // Assert
            Assert.That(receivedByFiltered, Is.Null);
            Assert.That(receivedByUnfiltered, Is.Not.Null);
            Assert.That(receivedByUnfiltered.Value, Is.EqualTo(30));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Publish_AfterDispose_SubscriberNoLongerReceivesEvent()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            var receiveCount = 0;

            var subscription = subscriber.Subscribe<TestEvent>(evt => receiveCount++);

            publisher.Publish(new TestEvent { Value = 1 });
            Assert.That(receiveCount, Is.EqualTo(1));

            // Act
            subscription.Dispose();
            publisher.Publish(new TestEvent { Value = 2 });

            // Assert
            Assert.That(receiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_WithNullEvent_DoesNotThrow()
        {
            // Arrange
            var publisher = Container.Resolve<Publisher>();

            // Act & Assert - Publisher 改為 Debug.LogError + return，不拋出例外
            Assert.DoesNotThrow(() => publisher.Publish<TestEvent>(null));
        }

        [Test]
        public void Subscribe_MultipleTimes_EachSubscriptionWorksIndependently()
        {
            // Arrange
            var subscriber = Container.Resolve<Subscriber>();
            var publisher = Container.Resolve<Publisher>();
            var receiveCount1 = 0;
            var receiveCount2 = 0;

            var subscription1 = subscriber.Subscribe<TestEvent>(evt => receiveCount1++);
            var subscription2 = subscriber.Subscribe<TestEvent>(evt => receiveCount2++);

            // Act
            publisher.Publish(new TestEvent { Value = 1 });
            subscription1.Dispose();
            publisher.Publish(new TestEvent { Value = 2 });

            // Assert
            Assert.That(receiveCount1, Is.EqualTo(1));
            Assert.That(receiveCount2, Is.EqualTo(2));
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            // Arrange
            var publisher = Container.Resolve<Publisher>();
            var testEvent = new TestEvent { Value = 123 };

            // Act & Assert
            Assert.DoesNotThrow(() => publisher.Publish(testEvent));
        }

        [Test]
        public void PublishAsync_WithNullEvent_DoesNotThrow()
        {
            // Arrange
            var publisher = Container.Resolve<Publisher>();

            // Act & Assert - Publisher 改為 Debug.LogError + return，不拋出例外
            Assert.DoesNotThrow(() => publisher.PublishAsync<TestEvent>(null));
        }

        #endregion

        private class TestEvent : IEvent
        {
            public int Value { get; set; }
        }
    }
}
