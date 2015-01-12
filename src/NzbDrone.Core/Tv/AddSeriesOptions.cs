using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Tv
{
    public class AddSeriesOptions : ModelBase
    {
        public int SeriesId { get; set; }
        public bool SearchForMissingEpisodes { get; set; }
        public bool IgnoreEpisodesWithFiles { get; set; }
        public bool IgnoreEpisodesWithoutFiles { get; set; }
        public List<int> IgnoreSeasons { get; set; }

        public AddSeriesOptions()
        {
            IgnoreSeasons = new List<int>();
        }
    }
}
