using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Tv
{
    public interface ISeriesAddedQueueRepository : IBasicRepository<AddSeriesOptions>
    {
        AddSeriesOptions Find(int seriesId);
    }

    public class SeriesAddedQueueRepository : BasicRepository<AddSeriesOptions>, ISeriesAddedQueueRepository
    {
        public SeriesAddedQueueRepository(IDatabase database, IEventAggregator eventAggregator) : base(database, eventAggregator)
        {
        }


        public AddSeriesOptions Find(int seriesId)
        {
            return Query.Where(s => s.SeriesId == seriesId).SingleOrDefault();
        }
    }
}
