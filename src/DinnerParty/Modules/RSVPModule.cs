using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nancy.Security;
using DinnerParty.Models;
using Marten;
using Nancy.RouteHelpers;

namespace DinnerParty.Modules
{
    public class RSVPAuthorizedModule : BaseModule
    {
        public RSVPAuthorizedModule(IDocumentSession documentSession)
            : base("/RSVP")
        {
            this.RequiresAuthentication();

            Post["/Cancel/{id}"] = parameters =>
            {
                Dinner dinner = documentSession.Load<Dinner>((int)parameters.id);

                RSVP rsvp = dinner.RSVPs
                    .SingleOrDefault(r => this.Context.CurrentUser.UserName == (r.AttendeeNameId ?? r.AttendeeName));

                if (rsvp != null)
                {
                    dinner.RSVPs.Remove(rsvp);
                    documentSession.SaveChanges();

                }

                return "Sorry you can't make it!";
            };

            Post["/Register/{id}"] = parameters =>
            {
                Dinner dinner = documentSession.Load<Dinner>((int)parameters.id);

                if (!dinner.IsUserRegistered(this.Context.CurrentUser.UserName))
                {

                    RSVP rsvp = new RSVP();
                    rsvp.AttendeeNameId = this.Context.CurrentUser.UserName;
                    rsvp.AttendeeName = ((UserIdentity)this.Context.CurrentUser).FriendlyName;

                    dinner.RSVPs.Add(rsvp);

                    documentSession.SaveChanges(); 
                }

                return "Thanks - we'll see you there!";
            };
        }
    }
}