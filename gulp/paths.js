module.exports = {
    src: {
        root: './src/UI/',
        templates: './src/UI/**/*.hbs',
        index: './src/UI/index.html',
        login: './src/UI/login.html',
        partials: './src/UI/**/*Partial.hbs',
        scripts: './src/UI/**/*.js',
        less: ['./src/UI/**/*.less'],
        content: './src/UI/Content/',
        images: './src/UI/Content/Images/**/*',
        exclude :{
            libs:'!./src/UI/JsLibraries/**'
        }
    },
    dest: {
        root: './_output/UI/',
        content: './_output/UI/Content/'
    }
};
