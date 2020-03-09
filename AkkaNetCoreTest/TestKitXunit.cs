using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.TestKit.Xunit2;
using Xunit.Abstractions;

namespace AkkaNetCoreTest
{
    public abstract class TestKitXunit : TestKit
    {
        private readonly ITestOutputHelper _output;
        private readonly TextWriter _originalOut;
        private readonly TextWriter _textWriter;

        public TestKitXunit(ITestOutputHelper output)
        {
            _output = output;
            _originalOut = Console.Out;
            _textWriter = new StringWriter();
            Console.SetOut(_textWriter);         
        }

        protected override void Dispose(bool disposing)
        {            
            _output.WriteLine(_textWriter.ToString());
            Console.SetOut(_originalOut);
            base.Dispose(disposing);
        }
    }
}
