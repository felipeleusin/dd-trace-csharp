﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datadog.Trace.AspNetCore.Tests
{
    public class MockWriter : IAgentWriter
    {
        public MockWriter()
        {
            Traces = new List<List<Span>>();
        }

        public List<List<Span>> Traces { get; set; }

        public Task FlushAndCloseAsync()
        {
            return Task.FromResult(true);
        }

        public void WriteServiceInfo(ServiceInfo serviceInfo)
        {
        }

        public void WriteTrace(List<Span> trace)
        {
            Traces.Add(trace);
        }
    }
}