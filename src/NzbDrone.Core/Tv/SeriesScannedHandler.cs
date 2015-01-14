using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Tv
{
    public class SeriesScannedHandler : IHandle<SeriesScannedEvent>,
                                        IHandle<SeriesScanSkippedEvent>
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly ICommandExecutor _commandExecutor;
       
        private readonly Logger _logger;

        public SeriesScannedHandler(ISeriesService seriesService,
                                  IEpisodeService episodeService,
                                  ICommandExecutor commandExecutor,
                                  Logger logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _commandExecutor = commandExecutor;
            _logger = logger;
        }

        private void UnmonitorEpisodes(IEnumerable<Episode> episodes)
        {
            foreach (var episode in episodes)
            {
                episode.Monitored = false;
            }
        }
        
        private void SetEpisodeMonitoredStatus(Series series, List<Episode> episodes)
        {
            _logger.Debug("[{0}] Setting episode monitored status.", series.Title);

            if (series.AddOptions.IgnoreEpisodesWithFiles)
            {
                _logger.Debug("Ignoring Episodes with Files");
                UnmonitorEpisodes(episodes.Where(e => e.HasFile));
            }

            if (series.AddOptions.IgnoreEpisodesWithoutFiles)
            {
                _logger.Debug("Ignoring Episodes without Files");
                UnmonitorEpisodes(episodes.Where(e => !e.HasFile && e.AirDateUtc.HasValue && e.AirDateUtc.Value.Before(DateTime.UtcNow)));
            }

            foreach (var season in series.Seasons)
            {
                if (series.Seasons.Select(s => s.SeasonNumber).MaxOrDefault() != season.SeasonNumber &&
                    episodes.Where(e => e.SeasonNumber == season.SeasonNumber).All(e => !e.Monitored))
                    
                {
                    season.Monitored = false;
                }
            }

            _seriesService.UpdateSeries(series);
            _episodeService.UpdateEpisodes(episodes);
        }

        private void SearchForMissingEpisodes(Series series)
        {
            _commandExecutor.PublishCommand(new MissingEpisodeSearchCommand(series.Id));
        }

        private void HandleScanEvents(Series series)
        {
            if (series.AddOptions == null)
            {
                _logger.Debug("[{0}] Was not recently added, skipping post-add actions", series.Title);
                return;
            }

            var episodes = _episodeService.GetEpisodeBySeries(series.Id);
            SetEpisodeMonitoredStatus(series, episodes);

            if (series.AddOptions.SearchForMissingEpisodes)
            {
                SearchForMissingEpisodes(series);
            }

            series.AddOptions = null;
            _seriesService.UpdateSeries(series);
        }

        public void Handle(SeriesScannedEvent message)
        {
            HandleScanEvents(message.Series);
        }

        public void Handle(SeriesScanSkippedEvent message)
        {
            HandleScanEvents(message.Series);
        }
    }
}
