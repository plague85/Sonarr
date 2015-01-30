using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(76)]
    public class add_users_table : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("Users")
                  .WithColumn("Identifier").AsString().NotNullable()
                  .WithColumn("Username").AsString().NotNullable()
                  .WithColumn("Password").AsString().NotNullable();
        }
    }
}
