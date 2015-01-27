'use strict';

define(
    [
        'marionette',
        'Commands/CommandController'
    ], function (Marionette, CommandController) {
        return Marionette.ItemView.extend({
            template: 'System/Update/UpdateItemViewTemplate',

            events: {
                'click .x-install-update': '_installUpdate'
            },

            initialize: function () {
                this.updating = false;
            },

            _installUpdate: function () {
                if (this.updating) {
                    return;
                }

                this.updating = true;
                var self = this;

                var promise = CommandController.Execute('applicationUpdate');

                promise.done(function () {
                    window.setTimeout(function () {
                        self.updating = false;
                    }, 5000);
                });


            }
        });
    });
