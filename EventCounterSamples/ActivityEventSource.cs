using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace EventCounterSamples
{
    // REVIEW: Perhaps this type should be public? Obviously not the methods that emit events, but for access to the
    // Name, Keywords, etc.?
    [EventSource(Name =  "EventCounterSamples-Activity")]
    internal partial class ActivityEventSource : EventSource
    {
        public static readonly ActivityEventSource Log = new ActivityEventSource();
        private readonly EventCounter _requestsStartedCounter;
        private readonly EventCounter _requestsCompletedCounter;
        private readonly EventCounter _requestDurationCounter;

        private ActivityEventSource()
        {
            _requestsStartedCounter = new EventCounter("RequestsStarted", this);
            _requestsCompletedCounter = new EventCounter("RequestsCompleted", this);
            _requestDurationCounter = new EventCounter("RequestDuration", this);
        }

        [NonEvent]
        internal EventActivity<int> Request(string path)
        {
            RequestStarted(path);
            return EventActivity.Create<int>(this, (self, statusCode, ts) => ((ActivityEventSource)self).RequestCompleted(path, statusCode, (float)ts.TotalMilliseconds));
        }

        [Event(eventId: 1, Message = "Request started at path '{0}'", Level = EventLevel.Informational, Keywords = Keywords.RequestEvents)]
        private void RequestStarted(string path)
        {
            if (IsEnabled())
            {
                if (IsEnabled(EventLevel.LogAlways, Keywords.Counters))
                {
                    _requestsStartedCounter.WriteMetric(1.0f);
                }

                if (IsEnabled(EventLevel.Informational, Keywords.RequestEvents))
                {
                    WriteEvent(1, path);
                }
            }
        }

        [Event(eventId: 2, Message = "Request completed at path '{0}', with status code: {1} (duration: {2}ms)", Level = EventLevel.Informational, Keywords = Keywords.RequestEvents)]
        internal void RequestCompleted(string path, int statusCode, float elapsedMilliseconds)
        {
            if (IsEnabled())
            {
                if (IsEnabled(EventLevel.LogAlways, Keywords.Counters))
                {
                    _requestsCompletedCounter.WriteMetric(1.0f);
                    _requestDurationCounter.WriteMetric(elapsedMilliseconds);
                }

                if (IsEnabled(EventLevel.Informational, Keywords.RequestEvents))
                {
                    WriteEvent(2, path, statusCode, elapsedMilliseconds);
                }
            }
        }

        public static class Keywords
        {
            // REVIEW: Try to reserve the same keyword value across all our providers for enabling counters?
            public const EventKeywords Counters = (EventKeywords)0x01;

            // Keyword for each logical "category" of events within a provider. Usually, only the one.
            public const EventKeywords RequestEvents = (EventKeywords)0x02;
        }
    }
}
