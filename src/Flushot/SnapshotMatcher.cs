using System;
using System.IO;
using System.Text;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Json;
using FluentAssertions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Flushot
{
    internal class SnapshotMatcher
    {
        private readonly Snapshotter _snapshotter;

        internal SnapshotMatcher(Snapshotter snapshotter)
        {
            _snapshotter = snapshotter;
        }

        public AndConstraint<ObjectAssertions> Match<T>(
            ObjectAssertions assertions,
            Type deserializationType,
            JsonSerializer? serializer,
            Func<EquivalencyAssertionOptions<T>, EquivalencyAssertionOptions<T>>? config)
        {
            serializer ??= new JsonSerializer();

            var subject = assertions.Subject;
            var snapshot = _snapshotter.GetOrCreateSnapshot(subject, serializer);

            var actualJson = ToJTokenUsingSerializer(assertions, serializer);

            actualJson.Should()
                      .BeEquivalentTo(snapshot, $"snapshot {_snapshotter.SnapshotPath} doesn't match");

            var deserializedSnapshot = snapshot?.ToObject(deserializationType, serializer);
            deserializedSnapshot.Should()
                                .BeOfType(subject.GetType())
                                .And.BeEquivalentTo((T)subject, config ?? (x => x));

            return deserializedSnapshot.Should().BeAssignableTo(deserializationType);
        }

        private JToken? ToJTokenUsingSerializer(ObjectAssertions assertions, JsonSerializer serializer)
        {
            var buffer = new MemoryStream();
            var writer = new StreamWriter(buffer);
            serializer.Serialize(writer, assertions.Subject);
            writer.Flush();
            var bytes = buffer.ToArray();
            var input = new MemoryStream(bytes);

            var actualJson =
                serializer.Deserialize<JToken>(
                    new JsonTextReader(new StreamReader(input, Encoding.UTF8)));
            return actualJson;
        }
    }
}
