/// <reference path="../libs/qunit/qunit.js" />
/// <reference path="define.js" />
/// <reference path="../its.log.js/bus.js" />

module('busTests.js');

test("bus Subscribe uses supplied callback after event", function () {
    var bus = defines["bus"]();
    var actualPayload;

    bus.subscribe(function (payload) {
        actualPayload = payload;
    });

    bus.publish('test Payload');

    equal(actualPayload, 'test Payload');
})

test("bus Subscribe uses callback when condition satisfied", function () {
    var bus = defines["bus"]();
    var actualPayload;

    bus.when(function () {
        return true;
    }).subscribe(function (payload) {
        actualPayload = payload;
    });

    bus.publish('test Payload');

    equal(actualPayload, 'test Payload');
})

test("bus Publish uses payload in condition", function () {
    var bus = defines["bus"]();
    var testedPayload;

    bus.when(function (payload) {
        testedPayload = payload;
    }).subscribe(function () {
    });

    bus.publish('test Payload');

    equal(testedPayload, 'test Payload');
})

test("bus Subscribe does not hit callback when condition false", function () {
    var bus = defines["bus"]();
    var actualPayload = 'original payload';

    bus.when(function () {
        return false;
    }).subscribe(function (payload) {
        actualPayload = payload;
    });

    bus.publish('test Payload');

    equal(actualPayload, 'original payload');
})

test('bus calls all subscriptions when a subscription throws', function() {
    var bus = defines["bus"]();
    var called = false;

    bus.subscribe(function(payload) {
        throw new Error("Intentional error");
    });
    bus.subscribe(function(payload) {
        called = true;
    });

    bus.publish("test message");

    ok(called, "second subscription was called");
})