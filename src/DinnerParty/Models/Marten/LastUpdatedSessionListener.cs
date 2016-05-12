using System;
using DinnerParty.Helpers;
using Marten;
using Microsoft.Ajax.Utilities;

namespace DinnerParty.Models.Marten
{
    // This class ensures that the LastModified property/field is always updated before changes
    // are persisted by marten to PostgreSQL
    public class LastUpdatedSessionListener : DocumentSessionListenerBase
    {
        public override void BeforeSaveChanges(IDocumentSession session)
        {
            var pending = session.PendingChanges;

            pending.InsertsFor<Dinner>().Each(d => d.LastModified = DateTime.Now);
            pending.UpdatesFor<Dinner>().Each(d => d.LastModified = DateTime.Now);
        }
    }
}