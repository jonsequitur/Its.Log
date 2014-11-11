/// <reference path="../libs/qunit/qunit.js" />
/// <reference path="define.js" />
/// <reference path="../its.log.js/bus.js" />
/// <reference path="../its.log.js/stack.js" />
/// <reference path="../its.log.js/extend.js" />
/// <reference path="../its.log.js/its.log.js" />
/// <reference path="trace.js" />

module('stackTests.js', {
    setup: traceSetup,
    teardown: traceTeardown
});

var stack = defines["stack"]();
var log = defines["log"](defines["bus"](), stack, defines["extend"]());

test('stack uses supplied stack from error', function () {

    var error = new Error();
    error.stack = "test stack";
    var s = stack({ e: error });

    equal(s, "test stack", "stack comes from supplied error");
})

test('stack contains name of containing function', function () {
    var s;

    function abcdef() {
        s = stack();
    }

    abcdef();

    ok(s.indexOf("abcdef") != -1, "function abcdef found");
})

test('stack contains calling function when error does not include stack', function () {
    var s;

    function abcdef() {
        s = stack({ bypassError: true }).join();
    }

    abcdef();

    ok(s.indexOf("abcdef") != -1, "function abcdef found");
})