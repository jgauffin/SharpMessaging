using System;

namespace SharpMessaging.LabApp
{
    internal class MyMessage
    {
        public MyMessage(string action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string Action { get; }
    }
}