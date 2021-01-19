using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SharpMessaging.Core.Persistence.Disk;
using Xunit;

namespace SharpMessaging.Core.Tests.Persistence.Disk
{
    public class FileQueueTests : IDisposable
    {
        public FileQueueTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
            Directory.CreateDirectory(_directory);
            _sut = new FileQueue(_directory, "Mepp");
        }

        public void Dispose()
        {
            _sut.Close();
            Directory.Delete(_directory, true);
        }

        private readonly string _directory;
        private readonly FileQueue _sut;

        [Fact]
        public async Task Should_create_a_new_file_when_the_existing_file_is_full()
        {
            _sut.MaxFileSize = 1;

            await _sut.Open();
            await _sut.Enqueue("hello1");
            await _sut.Enqueue("hello2");

            Directory.GetFiles(_directory, "*.data").Length.Should().Be(3,
                "because a new file should be added each time we go over the limit");
        }


        [Fact]
        public async Task Should_create_one_queue_file_on_creation()
        {
            await _sut.Open();

            Directory.GetFiles(_directory, "*.data").Length.Should().Be(1);
        }

        [Fact]
        public async Task Should_only_release_once_when_entry_is_enqueued()
        {
            _sut.MaxFileSize = 1;

            ThreadPool.QueueUserWorkItem(x =>
            {
                Thread.Sleep(100);
                _sut.Enqueue("Hello world").Wait();
            });
            await _sut.Open();
            var t1 = _sut.Dequeue(TimeSpan.FromSeconds(1));
            var t2 = _sut.Dequeue(TimeSpan.FromSeconds(1));
            var t = await Task.WhenAll(t1, t2);

            t1.Result.Message.Should().Be("Hello world");
            t2.Result.Should().BeNull();
        }

        [Fact]
        public async Task Should_open_existing_files()
        {
            _sut.MaxFileSize = 1;

            await _sut.Open();
            await _sut.Enqueue("hello1");
            _sut.Close();
            await _sut.Open();
            var entry = await _sut.Dequeue(TimeSpan.Zero);

            entry.Message.Should().Be("hello1");
        }

        [Fact]
        public async Task Should_release_when_entry_is_enqueued()
        {
            _sut.MaxFileSize = 1;

            ThreadPool.QueueUserWorkItem(x =>
            {
                Thread.Sleep(100);
                _sut.Enqueue("Hello world").Wait();
            });
            await _sut.Open();
            var entry = await _sut.Dequeue(TimeSpan.FromSeconds(1));

            entry.Message.Should().Be("Hello world");
        }

        [Fact]
        public async Task Should_release_when_message_is_aborted()
        {
            _sut.MaxFileSize = 1;
            await _sut.Open();
            await _sut.Enqueue("Hello world1");
            await _sut.Enqueue("Hello world2");
            var entry = await _sut.Dequeue(TimeSpan.FromMilliseconds(100));

            await entry.Abort();
            entry = await _sut.Dequeue(TimeSpan.FromMilliseconds(100));

            entry.Message.Should().Be("Hello world1");
        }

        [Fact]
        public async Task Should_remove_file_once_the_last_entry_is_enqueued()
        {
            _sut.MaxFileSize = 1;
            await _sut.Open();
            await _sut.Enqueue("Hello world1");
            await _sut.Enqueue("Hello world2");
            var entry = await _sut.Dequeue(TimeSpan.FromMilliseconds(100));
            await entry.Complete();

            // required since we remove files on the next dequeue when the file returns null.
            await _sut.Dequeue(TimeSpan.FromMilliseconds(100));

            Directory.GetFiles(_directory, "*.data").Length.Should().Be(2, "because one file was removed");
        }

        [Fact]
        public async Task Should_wait_if_no_entries_exist()
        {
            _sut.MaxFileSize = 1;

            await _sut.Open();
            var sw = Stopwatch.StartNew();
            var entry = await _sut.Dequeue(TimeSpan.FromMilliseconds(100));
            sw.Stop();

            entry.Should().BeNull();
            sw.ElapsedMilliseconds.Should().BeGreaterThan(100);
        }
    }
}