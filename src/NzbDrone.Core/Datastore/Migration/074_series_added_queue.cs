using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(74)]
    public class series_added_queue : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("SeriesAddedQueue")
                  .WithColumn("SeriesId").AsInt32().NotNullable()
                  .WithColumn("SearchForMissingEpisodes").AsBoolean().NotNullable()
                  .WithColumn("IgnoreEpisodesWithFiles").AsBoolean().NotNullable()
                  .WithColumn("IgnoreEpisodesWithoutFiles").AsBoolean().NotNullable();
        }
    }
}
