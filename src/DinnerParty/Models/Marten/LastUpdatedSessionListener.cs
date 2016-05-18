using System;
using DinnerParty.Helpers;
using Marten;

namespace DinnerParty.Models.Marten
{
    /// <summary>
    /// This class demonstrates using a <seealso cref="DocumentSessionListenerBase"/> to ensure that 
    /// the <see cref="Dinner.LastModified"/> property is always updated before an insert/update opration
    /// </summary>
    /// <remarks>
    /// See: https://github.com/JasperFx/marten/blob/master/documentation/documentation/documents/diagnostics.md#listening-for-document-store-events
    /// </remarks>
    public class LastUpdatedSessionListener : DocumentSessionListenerBase
    {
        public override void BeforeSaveChanges(IDocumentSession session)
        {
            // Get a set of pending changes for this session
            var pending = session.PendingChanges;

            // For each dinner that is to be inserted, set the Dinner.LastModified property to now
            pending.InsertsFor<Dinner>().Each(d => d.LastModified = DateTime.Now);

            // For each dinner that is to be updated, set the Dinner.LastModified property to now
            pending.UpdatesFor<Dinner>().Each(d => d.LastModified = DateTime.Now);
        }
    }
}