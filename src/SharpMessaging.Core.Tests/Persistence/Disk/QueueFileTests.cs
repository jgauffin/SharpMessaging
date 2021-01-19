using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SharpMessaging.Core.Persistence.Disk;
using Xunit;

namespace SharpMessaging.Core.Tests.Persistence.Disk
{
    public class QueueFileTests : IDisposable
    {
        public QueueFileTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
            Directory.CreateDirectory(_directory);
            _sut = new QueueFile(_directory, "Test");
        }

        public void Dispose()
        {
            _sut.Close();
            Directory.Delete(_directory, true);
        }

        private readonly string _directory;
        private readonly QueueFile _sut;

        [Fact]
        public async Task Should_be_able_to_store_and_read_record()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            var msg = await _sut.Dequeue();

            msg.Message.Should().Be("Hello world");
        }

        [Fact]
        public async Task Should_count_records_correctly_when_reopening()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            await _sut.Enqueue("Hello world2");
            _sut.Close();
            await _sut.Open();

            _sut.RecordCount.Should().Be(2);
        }

        [Fact]
        public async Task Should_remove_message_when_completing()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            var msg = await _sut.Dequeue();
            await msg.Complete();
            msg = await _sut.Dequeue();

            msg.Should().BeNull();
        }

        [Fact]
        public async Task Should_resume_when_file_is_reopened()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            await _sut.Enqueue("Hello world2");
            var msg1 = await _sut.Dequeue();
            await msg1.Complete();
            _sut.Close();
            await _sut.Open();
            var msg = await _sut.Dequeue();
            await msg.Complete();

            msg.Message.Should().Be("Hello world2");
        }

        [Fact]
        public async Task Should_resume_when_file_is_reopened_even_on_aborted()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            await _sut.Enqueue("Hello world2");
            var msg1 = await _sut.Dequeue();
            await msg1.Abort();
            _sut.Close();
            await _sut.Open();
            var msg = await _sut.Dequeue();
            await msg.Complete();

            msg.Message.Should().Be("Hello world");
        }

        [Fact]
        public async Task Should_return_message_when_aborting()
        {
            await _sut.Open();

            await _sut.Enqueue("Hello world");
            var msg = await _sut.Dequeue();
            await msg.Abort();
            msg = await _sut.Dequeue();

            msg.Message.Should().Be("Hello world");
        }
    }
}