﻿using System;
using Marten.Events;
using Shouldly;
using Xunit;

namespace Marten.Testing.Events
{
    public class EventGraphTests
    {
        private readonly EventGraph theGraph = new EventGraph(new StoreOptions());

        [Fact]
        public void async_projections_are_not_enabled_by_default()
        {
            theGraph.AsyncProjectionsEnabled.ShouldBeFalse();
        }

        [Fact]
        public void javascript_projections_are_not_enabled_by_default()
        {
            theGraph.JavascriptProjectionsEnabled.ShouldBeFalse();
        }

        [Fact]
        public void default_rolling_buffer_size()
        {
            theGraph.AsyncProjectionBufferTableSize.ShouldBe(1000);
        }


        [Fact]
        public void find_stream_mapping_initially()
        {
            theGraph.AggregateFor<Issue>()
                .AggregateType.ShouldBe(typeof(Issue));
        }

        [Fact]
        public void caches_the_stream_mapping()
        {
            theGraph.AggregateFor<Issue>()
                .ShouldBeSameAs(theGraph.AggregateFor<Issue>());
        }

        [Fact]
        public void register_event_types_and_retrieve()
        {
            theGraph.AddEventType(typeof (IssueAssigned));
            theGraph.AddEventType(typeof (IssueCreated));
            theGraph.AddEventType(typeof (MembersJoined));
            theGraph.AddEventType(typeof (MembersDeparted));

            theGraph.EventMappingFor<IssueAssigned>().ShouldBeTheSameAs(theGraph.EventMappingFor<IssueAssigned>());
        }

        [Fact]
        public void find_event_by_event_type_name()
        {
            theGraph.AddEventType(typeof(IssueAssigned));
            theGraph.AddEventType(typeof(IssueCreated));
            theGraph.AddEventType(typeof(MembersJoined));
            theGraph.AddEventType(typeof(MembersDeparted));

            theGraph.EventMappingFor("members_joined").DocumentType.ShouldBe(typeof(MembersJoined));

            theGraph.EventMappingFor("issue_created").DocumentType.ShouldBe(typeof(IssueCreated));
        }

        [Fact]
        public void derives_the_stream_type_name()
        {

            theGraph.AggregateFor<HouseRemodeling>().Alias.ShouldBe("house_remodeling");
            theGraph.AggregateFor<Quest>().Alias.ShouldBe("quest");
        }

        [Fact]
        public void has_any_in_starting_state()
        {
            theGraph.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void has_any_is_true_with_any_events()
        {
            theGraph.AddEventType(typeof(IssueAssigned));
            theGraph.IsActive.ShouldBeTrue();
        }


        public class HouseRemodeling
        {
            public Guid Id { get; set; }
        }
    }
}