'use strict';
define(
    [
        'jquery',
        'Mixins/jquery.ajax'
    ], function ($) {
        return {

            get: function (resource) {
                var url = window.NzbDrone.ApiRoot + '/' + resource;
                var _data;

                $.ajax({
                    url: url,
                    async:false
                }).done(function (data) {
                    _data = data;
                }).error(function (xhr, status, error) {
                    throw error;
                });


                return _data;
            }
        };
    });