using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace QNX6FSRebuilder.UI.Providers
{
    public class TextBoxLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Action<string> _writeAction;

        public TextBoxLogger(string categoryName, Action<string> writeAction)
        {
            _categoryName = categoryName;
            _writeAction = writeAction;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = $"{DateTime.Now:HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            if (exception != null)
                message += Environment.NewLine + exception;

            _writeAction(message);
        }

    }
}
