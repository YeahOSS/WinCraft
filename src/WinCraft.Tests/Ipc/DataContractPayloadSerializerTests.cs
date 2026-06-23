using System.Runtime.Serialization;
using NUnit.Framework;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Tests.Ipc
{
    [TestFixture]
    internal sealed class DataContractPayloadSerializerTests
    {
        [DataContract]
        private sealed class TestPayload
        {
            [DataMember(Order = 1)]
            public string Name { get; set; }

            [DataMember(Order = 2)]
            public int Count { get; set; }
        }

        [Test]
        public void RoundTrip_SimpleObject_PreservesValues()
        {
            var original = new TestPayload { Name = "hello", Count = 42 };

            var serialized = DataContractPayloadSerializer.Serialize(original);
            var deserialized = DataContractPayloadSerializer.Deserialize<TestPayload>(serialized);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Name, Is.EqualTo("hello"));
            Assert.That(deserialized.Count, Is.EqualTo(42));
        }

        [Test]
        public void RoundTrip_ElevatedCommandRequest_PreservesAllFields()
        {
            var original = new ElevatedCommandRequest
            {
                OperationName = "ping",
                Payload = "{\"key\":\"value\"}",
                RequestId = "req-abc",
                PrivilegeLevel = PrivilegeLevel.Administrator
            };

            var serialized = DataContractPayloadSerializer.Serialize(original);
            var deserialized = DataContractPayloadSerializer.Deserialize<ElevatedCommandRequest>(serialized);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.OperationName, Is.EqualTo("ping"));
            Assert.That(deserialized.Payload, Is.EqualTo("{\"key\":\"value\"}"));
            Assert.That(deserialized.RequestId, Is.EqualTo("req-abc"));
            Assert.That(deserialized.PrivilegeLevel, Is.EqualTo(PrivilegeLevel.Administrator));
        }

        [Test]
        public void RoundTrip_CommandResult_PreservesErrorFields()
        {
            var original = CommandResult.Failure("err", "message", "req-1");

            var serialized = DataContractPayloadSerializer.Serialize(original);
            var deserialized = DataContractPayloadSerializer.Deserialize<CommandResult>(serialized);

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Succeeded, Is.False);
            Assert.That(deserialized.ErrorCode, Is.EqualTo("err"));
            Assert.That(deserialized.ErrorMessage, Is.EqualTo("message"));
            Assert.That(deserialized.RequestId, Is.EqualTo("req-1"));
        }

        [Test]
        public void Deserialize_EmptyString_Throws()
        {
            Assert.That(
                () => DataContractPayloadSerializer.Deserialize<TestPayload>(string.Empty),
                Throws.Exception);
        }

        [Test]
        public void Deserialize_Null_Throws()
        {
            Assert.That(
                () => DataContractPayloadSerializer.Deserialize<TestPayload>(null),
                Throws.Exception);
        }

        [Test]
        public void Serialize_NullObject_DoesNotThrow()
        {
            Assert.That(
                () => DataContractPayloadSerializer.Serialize<TestPayload>(null),
                Throws.Nothing);
        }
    }
}
