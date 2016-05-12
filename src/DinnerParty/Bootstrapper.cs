using Nancy;
using Nancy.Authentication.Forms;
using Nancy.TinyIoc;
using Nancy.Validation.DataAnnotations;
using DinnerParty.Models.CustomAnnotations;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using System;
using System.Collections.Generic;
using System.Configuration;
using DinnerParty.Models;
using DinnerParty.Models.Marten;
using Marten;

namespace DinnerParty
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

#if !DEBUG
            Cassette.Nancy.CassetteNancyStartup.OptimizeOutput = true;
#endif
            // Not needed in newer versions of Nancy.Validation?
            //DataAnnotationsValidator.RegisterAdapter(typeof(MatchAttribute), (v, d) => new CustomDataAdapter((MatchAttribute)v));

            var docStore = container.Resolve<DocumentStore>("DocStore");

            CleanUpDB(docStore);

            pipelines.OnError += (context, exception) =>
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(exception);
                return null;
            };
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            var martenConnectionString = ConfigurationManager.ConnectionStrings["dinnerParty"].ConnectionString;
            var store = DocumentStore.For(_ =>
            {
                _.Connection(martenConnectionString);
                _.Schema.Include<UserModelRegistry>();
                _.Schema.Include<DinnersRegistry>();
            });

            container.Register(store, "DocStore");
        }

        protected override void RequestStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            // At request startup we modify the request pipelines to
            // include forms authentication - passing in our now request
            // scoped user name mapper.
            //
            // The pipelines passed in here are specific to this request,
            // so we can add/remove/update items in them as we please.
            var formsAuthConfiguration =
                new FormsAuthenticationConfiguration()
                {
                    RedirectUrl = "~/account/logon",
                    UserMapper = container.Resolve<IUserMapper>(),
                };

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            container.Register<IUserMapper, UserMapper>();

            var docStore = container.Resolve<DocumentStore>("DocStore");
            var documentSession = docStore.OpenSession();

            container.Register<IDocumentSession>(documentSession);
        }

        protected override void ConfigureConventions(Nancy.Conventions.NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.Add(Nancy.Conventions.StaticContentConventionBuilder.AddDirectory("/", "public"));
        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration => new DiagnosticsConfiguration { Password = @"nancy" };

        protected override IEnumerable<Type> ViewEngines => new[] { typeof(Nancy.ViewEngines.Razor.RazorViewEngine) };

        private void CleanUpDB(DocumentStore documentStore)
        {
            var docSession = documentStore.OpenSession();
            var configInfo = docSession.Load<Config>("DinnerParty/Config");

            if (configInfo == null)
            {
                configInfo = new Config();
                configInfo.Id = "DinnerParty/Config";
                configInfo.LastTruncateDate = DateTime.Now.AddHours(-48); //No need to delete data if config doesnt exist but setup ready for next time

                docSession.Store(configInfo);
                docSession.SaveChanges();

                return;
            }


            if ((DateTime.Now - configInfo.LastTruncateDate).TotalHours < 24)
                return;

            long docCount = 0, dbSize = 0;

            using(var cmd = docSession.Connection.CreateCommand())
            {
                var dinnerTableName = documentStore.Schema.MappingFor(typeof(Dinner)).Table.QualifiedName;
                cmd.CommandText = $"SELECT COUNT(*) FROM {dinnerTableName}";
                docCount = (long)cmd.ExecuteScalar();
            }

            using(var cmd = docSession.Connection.CreateCommand())
            {
                var dinnerPartyDbName = docSession.Connection.Database;
                cmd.CommandText = $"SELECT pg_database_size('{dinnerPartyDbName}')";
                dbSize = (long)cmd.ExecuteScalar();

                configInfo.LastTruncateDate = DateTime.Now;
                docSession.SaveChanges();
            }

            //If database size >15mb or 1000 documents delete documents older than a week
            if (docCount > 1000 || dbSize > 15000000) //its actually 14.3mb but goood enough
            {
                //TODO: Finish this
                //docSession.DeleteWhere<Dinner>(dp=>dp.);

                //documentStore.DatabaseCommands.DeleteByIndex("Raven/DocumentsByEntityName",
                //    new IndexQuery
                //    {
                //        Query = docSession.Advanced.LuceneQuery<object>()
                //            .WhereEquals("Tag", "Dinners")
                //            .AndAlso()
                //            .WhereLessThan("LastModified", DateTime.Now.AddDays(-7)).ToString()
                //    },
                //    false);
            }
        }
    }
}