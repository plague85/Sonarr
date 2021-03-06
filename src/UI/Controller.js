'use strict';
define(
    [
        'Shared/NzbDroneController',
        'AppLayout',
        'marionette',
        'Activity/ActivityLayout',
        'Settings/SettingsLayout',
        'AddSeries/AddSeriesLayout',
        'Wanted/WantedLayout',
        'Calendar/CalendarLayout',
        'Release/ReleaseLayout',
        'System/SystemLayout',
        'SeasonPass/SeasonPassLayout',
        'Series/Editor/SeriesEditorLayout'
    ], function (NzbDroneController,
                 AppLayout,
                 Marionette,
                 ActivityLayout,
                 SettingsLayout,
                 AddSeriesLayout,
                 WantedLayout,
                 CalendarLayout,
                 ReleaseLayout,
                 SystemLayout,
                 SeasonPassLayout,
                 SeriesEditorLayout) {
        return NzbDroneController.extend({

            addSeries: function (action) {
                this.setTitle('Add Series');
                this.showMainRegion(new AddSeriesLayout({action: action}));
            },

            calendar: function () {
                this.setTitle('Calendar');
                this.showMainRegion(new CalendarLayout());
            },

            settings: function (action) {
                this.setTitle('Settings');
                this.showMainRegion(new SettingsLayout({ action: action }));
            },

            wanted: function (action) {
                this.setTitle('Wanted');

                this.showMainRegion(new WantedLayout({ action: action }));
            },

            activity: function (action) {
                this.setTitle('Activity');

                this.showMainRegion(new ActivityLayout({ action: action }));
            },

            rss: function () {
                this.setTitle('RSS');
                this.showMainRegion(new ReleaseLayout());
            },

            system: function (action) {
                this.setTitle('System');
                this.showMainRegion(new SystemLayout({ action: action }));
            },

            seasonPass: function () {
                this.setTitle('Season Pass');
                this.showMainRegion(new SeasonPassLayout());
            },

            seriesEditor: function () {
                this.setTitle('Series Editor');
                this.showMainRegion(new SeriesEditorLayout());
            }
        });
    });
