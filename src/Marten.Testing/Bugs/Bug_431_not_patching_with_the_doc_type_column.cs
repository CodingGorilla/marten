using System.Threading.Tasks;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Weasel.Postgresql;
using Xunit;

namespace Marten.Testing.Bugs
{
    public class Bug_431_not_patching_with_the_doc_type_column : BugIntegrationContext
    {
        [Fact]
        public async Task should_add_a_missing_doc_type_column_in_patch()
        {
            StoreOptions(_ =>
            {
                _.AutoCreateSchemaObjects = AutoCreate.All;
                _.Connection(ConnectionSource.ConnectionString);
                _.Schema.For<User>();
            });


            theStore.Tenancy.Default.EnsureStorageExists(typeof(User));


            var store2 = SeparateStore(_ =>
            {
                _.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
                _.Schema.For<User>().AddSubClass<SuperUser>();
            });

            await store2.Schema.ApplyAllConfiguredChangesToDatabase();
            await store2.Schema.AssertDatabaseMatchesConfiguration();



        }
    }
}
