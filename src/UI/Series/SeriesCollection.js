'use strict';
define(
    [
        'underscore',
        'backbone',
        'backbone.pageable',
        'Series/SeriesModel',
        'Shared/ApiData',
        'Mixins/AsFilteredCollection',
        'Mixins/AsSortedCollection',
        'Mixins/AsPersistedStateCollection',
        'moment'
    ], function (_, Backbone, PageableCollection, SeriesModel, ApiData, AsFilteredCollection, AsSortedCollection, AsPersistedStateCollection, moment) {
        var Collection = PageableCollection.extend({
            url  : window.NzbDrone.ApiRoot + '/series',
            model: SeriesModel,
            tableName: 'series',

            state: {
                sortKey            : 'sortTitle',
                order              : -1,
                pageSize           : 100000,
                secondarySortKey   : 'sortTitle',
                secondarySortOrder : -1
            },

            mode: 'client',

            save: function () {
                var self = this;

                var proxy = _.extend( new Backbone.Model(),
                    {
                        id: '',

                        url: self.url + '/editor',

                        toJSON: function()
                        {
                            return self.filter(function (model) {
                                return model.edited;
                            });
                        }
                    });

                this.listenTo(proxy, 'sync', function (proxyModel, models) {
                    this.add(models, { merge: true });
                    this.trigger('save', this);
                });

                return proxy.save();
            },

            // Filter Modes
            filterModes: {
                'all'        : [null, null],
                'continuing' : ['status', 'continuing'],
                'ended'      : ['status', 'ended'],
                'monitored'  : ['monitored', true]
            },
            
            sortMappings: {
                'title'      : { sortKey: 'sortTitle' },
                'nextAiring' : {
                    sortValue: function (model, attr, order) {
                        var nextAiring = model.get(attr);

                        if (nextAiring) {
                            return moment(nextAiring).unix();
                        }

                        if (order === 1) {
                            return 0;
                        }

                        return Number.MAX_VALUE;
                    }
                },

                percentOfEpisodes: {
                    sortValue: function (model, attr) {
                        var percentOfEpisodes = model.get(attr);
                        var episodeCount = model.get('episodeCount');

                        return percentOfEpisodes + episodeCount / 1000000;
                    }

                }
            }
        });

        Collection = AsFilteredCollection.call(Collection);
        Collection = AsSortedCollection.call(Collection);
        Collection = AsPersistedStateCollection.call(Collection);


        var data = ApiData.get('series');

        return new Collection(data, { full: true });
    });
