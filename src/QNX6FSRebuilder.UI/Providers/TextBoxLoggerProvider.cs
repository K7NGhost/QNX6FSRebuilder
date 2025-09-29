using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace QNX6FSRebuilder.UI.Providers
{
    public class TextBoxLoggerProvider : ILoggerProvider
    {
        private readonly Action<string> _writeAction;

        public TextBoxLoggerProvider(Action<string> writeAction)
        {
            _writeAction = writeAction;
        }

        public ILogger CreateLogger(string categoryName) => new TextBoxLogger(categoryName, _writeAction);

        public void Dispose() { }


    }
}
