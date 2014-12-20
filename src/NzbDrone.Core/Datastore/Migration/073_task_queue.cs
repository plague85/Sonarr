using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(73)]
    public class task_queue : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("TaskQueue")
                  .WithColumn("TypeName").AsString().NotNullable()
                  .WithColumn("Priority").AsInt32().NotNullable()
                  .WithColumn("Parameters").AsString().NotNullable();
        }
    }
}
