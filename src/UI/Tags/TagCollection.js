'use strict';
define(
    [
        'backbone',
        'Tags/TagModel',
        'Shared/ApiData'
    ], function (Backbone, TagModel, ApiData) {
        var Collection = Backbone.Collection.extend({
            url  : window.NzbDrone.ApiRoot + '/tag',
            model: TagModel
        });

        return new Collection(ApiData.get('tag'));
    });
