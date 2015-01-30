'use strict';
define(
    [
        'backbone',
        'Shared/ApiData'
    ], function (Backbone,ApiData) {

        var StatusModel = Backbone.Model.extend({
            url: window.NzbDrone.ApiRoot + '/system/status'
        });

        var instance = new StatusModel(ApiData.get('system/status'));
        return instance;
    });
