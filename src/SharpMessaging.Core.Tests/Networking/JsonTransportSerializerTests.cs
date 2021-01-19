using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using SharpMessaging.Core.Networking;
using Xunit;

namespace SharpMessaging.Core.Tests.Networking
{
    public class JsonTransportSerializerTests
    {
        [Fact]
        public void Should_be_able_to_serialize_messages()
        {
            var headers = new Dictionary<string, string>();

            var sut = new JsonTransportSerializer();
            var buffer = sut.Serialize("Hello world", headers);
            var message = sut.Deserialize(buffer, 0, buffer.Length, headers);

            message.Should().Be("Hello world");
        }

        [Fact]
        public void Should_tell_if_the_contentType_header_is_missing()
        {
            var headers = new Dictionary<string, string>();
            var sut = new JsonTransportSerializer();
            var buffer = sut.Serialize("Hello world", headers);
            headers.Clear();

            Action actual = () => sut.Deserialize(buffer, 0, buffer.Length, headers);

            actual.Should().Throw<JsonSerializationException>().And.Message.Should().Contain("Type-Name");
        }
    }
}