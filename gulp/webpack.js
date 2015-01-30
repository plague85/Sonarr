var gulp = require('gulp');

var run = require('gulp-run');


gulp.task('webpack', function (cb) {
  run('webpack -d').exec()
  .pipe(gulp.dest('output'))
});

gulp.task('webpackWatch', function (cb) {
  run('webpack -d -w').exec()
  .pipe(gulp.dest('output'))
});
