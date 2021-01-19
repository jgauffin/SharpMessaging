using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMessaging.Core.Persistence.Disk
{
    /// <summary>
    ///     Facade for the queue files.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Makes sure that a new file is created when the current is full and that the oldest file is deleted once we've
    ///         dequeued the last entry from it.
    ///     </para>
    /// </remarks>
    public class FileQueue
    {
        private readonly LinkedList<QueueFile> _files = new LinkedList<QueueFile>();
        private readonly SemaphoreSlim _newMessageNotification = new SemaphoreSlim(0);
        private readonly string _queueDirectory;
        private readonly string _queueName;

        public FileQueue(string queueDirectory, string queueName)
        {
            _queueDirectory = queueDirectory ?? throw new ArgumentNullException(nameof(queueDirectory));
            if (!Directory.Exists(_queueDirectory)) Directory.CreateDirectory(_queueDirectory);

            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        }

        /// <summary>
        ///     Max number of bytes that a file can contain.
        /// </summary>
        /// <value>
        ///     Default is 10MB.
        /// </value>
        public int MaxFileSize { get; set; } = 10000000;

        /// <summary>
        ///     Close all files.
        /// </summary>
        public void Close()
        {
            foreach (var file in _files) file.Close();

            _files.Clear();
        }

        /// <summary>
        ///     Dequeue a new entry.
        /// </summary>
        /// <param name="maxWaitTime">
        ///     Amount of time to wait if the queue do not contain any entries. Use <c>TimeSpan.Zero</c> to
        ///     return directly if the queue is empty.
        /// </param>
        /// <returns>Entry if found; otherwise <c>null</c>.</returns>
        public async Task<DequeuedMessage> Dequeue(TimeSpan maxWaitTime)
        {
            if (!await _newMessageNotification.WaitAsync(maxWaitTime))
                return null;

            var queueFile = _files.First.Value;
            var msg = await queueFile.Dequeue();
            if (msg != null)
            {
                msg.EnlistAbort(() =>
                {
                    _newMessageNotification.Release();
                    return Task.CompletedTask;
                });
                return msg;
            }

            if (_files.Count == 1)
                return null;

            _files.RemoveFirst();
            queueFile.Close();
            queueFile.Delete();

            queueFile = _files.First.Value;
            return await queueFile.Dequeue();
        }

        /// <summary>
        ///     Enqueue a new entry.
        /// </summary>
        /// <param name="message">Entry to enqueue</param>
        /// <returns></returns>
        public async Task Enqueue(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var queueFile = _files.Last.Value;
            await queueFile.Enqueue(message);

            if (queueFile.FileSize > MaxFileSize)
            {
                var newFile = await CreateNewFile();
                _files.AddLast(newFile);
            }

            _newMessageNotification.Release();
        }

        /// <summary>
        ///     Open queue and all its files.
        /// </summary>
        /// <returns>Task when completed.</returns>
        public async Task Open()
        {
            var fileNames = Directory.GetFiles(_queueDirectory, _queueName + "_*.data")
                .OrderBy(x => x);
            foreach (var fileName in fileNames)
            {
                var file = new QueueFile(_queueDirectory, fileName);
                await file.Open();
                _files.AddLast(file);
            }

            if (_files.Count == 0)
            {
                var newFile = await CreateNewFile();
                _files.AddLast(newFile);
            }

            var count = _files.Sum(x => x.RecordCount);
            if (count > 0) _newMessageNotification.Release(count);
        }

        private async Task<QueueFile> CreateNewFile()
        {
            var counter = 0;
            var fileName = $"{_queueName}_{DateTime.UtcNow:MMddHHmmss}-{counter:00}.data";

            //guard against many files being creating at the same second.
            while (File.Exists(Path.Combine(_queueDirectory, fileName)))
            {
                counter++;
                fileName = $"{_queueName}_{DateTime.UtcNow:MMddHHmmss}-{counter:00}.data";
            }

            var file = new QueueFile(_queueDirectory, fileName);
            await file.Open();
            return file;
        }
    }
}