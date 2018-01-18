﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToAgent
    {
        private Tracer _tracer;
        private RecordHttpHandler _httpRecorder;

        public SendTracesToAgent()
        {
            _httpRecorder = new RecordHttpHandler();
            _tracer = Tracer.Create(new Uri("http://localhost:8126"), null, null, _httpRecorder);
        }

        [Fact]
        public async void MinimalSpan()
        {
            var scope = _tracer.StartActive("Operation");
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(1);
            Assert.Equal(1, _httpRecorder.Requests.Count);
            Assert.Equal(1, _httpRecorder.Responses.Count);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());
        }

        [Fact]
        public async void CustomServiceName()
        {
            const string App = "MyApp";
            const string AppType = "db";
            const string ServiceName = "MyService";
            var serviceList = new List<ServiceInfo> { new ServiceInfo { App = App, AppType = AppType, ServiceName = ServiceName } };
            _httpRecorder = new RecordHttpHandler();
            _tracer = Tracer.Create(new Uri("http://localhost:8126"), serviceList, null, _httpRecorder);

            var scope = _tracer.StartActive("Operation", serviceName: ServiceName);
            scope.Span.ResourceName = "This is a resource";
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(2);
            Assert.Equal(2, _httpRecorder.Requests.Count);
            Assert.Equal(2, _httpRecorder.Responses.Count);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());

            var serviceInfo = _httpRecorder.Services.Select(x => x.ServiceInfos().Single()).Single();
            Assert.Equal(ServiceName, serviceInfo.ServiceName);
            Assert.Equal(App, serviceInfo.App);
            Assert.Equal(AppType, serviceInfo.AppType);
        }

        [Fact]
        public async void Utf8Everywhere()
        {
            var scope = _tracer.StartActive("Aᛗᚪᚾᚾᚪ", serviceName: "На берегу пустынных волн");
            scope.Span.ResourceName = "η γλώσσα μου έδωσαν ελληνική";
            scope.Span.SetTag("யாமறிந்த", "ნუთუ კვლა");
            scope.Dispose();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(1);
            Assert.Equal(1, _httpRecorder.Requests.Count);
            Assert.Equal(1, _httpRecorder.Responses.Count);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.Traces.Single();
            MsgPackHelpers.AssertSpanEqual(scope.Span, trace.Single());
        }

        [Fact]
        public void WithDefaultFactory()
        {
            // This test does not check anything it validates that this codepath runs without exceptions
            var tracer = Tracer.Create();
            tracer.StartActive("Operation")
                .Dispose();
        }

        [Fact]
        public void WithGlobalTracer()
        {
            // This test does not check anything it validates that this codepath runs without exceptions
            Tracer.Instance.StartActive("Operation")
                .Dispose();
        }
    }
}
