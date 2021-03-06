'use strict';
define(
    [
        'vent',
        'marionette',
        'Profile/ProfileCollection',
        'Mixins/AsModelBoundView',
        'Mixins/AsValidatedView',
        'Mixins/AsEditModalView',
        'Mixins/TagInput',
        'Mixins/FileBrowser'
    ], function (vent, Marionette, Profiles, AsModelBoundView, AsValidatedView, AsEditModalView) {

        var view = Marionette.ItemView.extend({
            template: 'Series/Edit/EditSeriesViewTemplate',

            ui: {
                profile : '.x-profile',
                path    : '.x-path',
                tags    : '.x-tags'
            },

            events: {
                'click .x-remove': '_removeSeries'
            },

            initialize: function () {
                this.model.set('profiles', Profiles);
            },

            onRender: function () {
                this.ui.path.fileBrowser();

                this.ui.tags.tagInput({
                    model    : this.model,
                    property : 'tags'
                });
            },

            _onBeforeSave: function () {
                var profileId = this.ui.profile.val();
                this.model.set({ profileId: profileId});
            },

            _onAfterSave: function () {
                this.trigger('saved');
                vent.trigger(vent.Commands.CloseModalCommand);
            },

            _removeSeries: function () {
                vent.trigger(vent.Commands.DeleteSeriesCommand, {series:this.model});
            }
        });

        AsModelBoundView.call(view);
        AsValidatedView.call(view);
        AsEditModalView.call(view);

        return view;
    });
