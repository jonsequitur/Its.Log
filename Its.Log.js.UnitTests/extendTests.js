/// <reference path="../libs/qunit/qunit.js" />
/// <reference path="../its.log.js/extend.js" />

module('extendTests.js');

test('extend combines values into target', function () {
    var target = { a: 1 };

    var result = extend(target, { b: 2, c: 2 }, { c: 3 });

    equal(target, result, "target and returned value are equal");
    equal(target.a, 1, "a");
    equal(target.b, 2, "b");
    equal(target.c, 3, "c");
})

test('extend accepts empty target', function () {
    var target = null;

    extend(target, { b: 2, c: 2 }, { c: 3 });
    equal(null, target);
})
