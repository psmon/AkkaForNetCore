﻿using System;
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

        static public string akkaConfig = @"
akka.loglevel = DEBUG

my-custom-mailbox {
    mailbox-type : ""AkkaNetCore.Models.Message.IssueTrackerMailbox, AkkaNetCore""
}

akka.persistence.max-concurrent-recoveries = 50 #복구 최고 개수
akka.actor.default-mailbox.stash-capacity = 10000
akka.persistence.internal-stash-overflow-strategy = ""akka.persistence.ThrowExceptionConfigurator""

actor.deployment {
    /mymailbox {
        mailbox = my-custom-mailbox
    }
}
";
        public TestKitXunit(ITestOutputHelper output) : base(akkaConfig)
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
