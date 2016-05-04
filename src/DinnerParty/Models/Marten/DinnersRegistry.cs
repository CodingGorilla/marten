using System;
using Marten;

namespace DinnerParty.Models.Marten
{
    public class DinnersRegistry : MartenRegistry
    {
        public DinnersRegistry()
        {
            // Generate a gin index on the Dinner JSONB data
            For<Dinner>().GinIndexJsonData();
        }
    }
}