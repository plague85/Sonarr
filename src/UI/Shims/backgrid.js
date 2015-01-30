require('backbone');

var backgrid = require('../JsLibraries/backbone.backgrid');


//require('../Shared/Grid/HeaderCell');

backgrid.Column.prototype.defaults = {
    name      : undefined,
    label     : undefined,
    sortable  : true,
    editable  : false,
    renderable: true,
    formatter : undefined,
    cell      : undefined,
    //headerCell: 'NzbDrone',
    sortType  : 'toggle'
};




module.exports = backgrid;