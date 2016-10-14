/// <reference path="../libs/qunit/qunit.js" />
/// <reference path="define.js" />
/// <reference path="../its.log.js/bus.js" />
/// <reference path="../its.log.js/extend.js" />
/// <reference path="../its.log.js/its.log.js" />
/// <reference path="trace.js" />

module('its.log.tests.js', {
    setup: traceSetup,
    teardown: traceTeardown
});

var extend = defines["extend"]();

function mockStack() {
    return 'Fake stack';
};

function testLog() {
    return defines['log'](defines["bus"](), mockStack, extend);
};

function pause(ms) {
    var date = new Date();
    var curDate = null;
    do { curDate = new Date(); }
    while (curDate - date < ms);
}

test('subscriber recieves noted message', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        entry = e;
    });
    log.note('ponies');
    ok(entry, 'subscription called');
})

test('log note with exception subject attaches to error member', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        entry = e;
    });
    log.note(new ReferenceError());
    ok(entry.error instanceof ReferenceError, 'subscription called with ' + entry.error);
})

test('log note with string subject attaches to message member', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        entry = e;
    });
    log.note('ponies');
    equal(entry.message, 'ponies', 'subscription called with ' + entry.message);
})

test('log note with object subject extends entry', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        entry = e;
    });
    log.note({ abc: 123, def: 456 });

    equal(entry.abc, 123, "entry extended with abc=" + entry.abc);
    equal(entry.def, 456, "entry extended with def=" + entry.def);
})

test('log enter writes entry', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        entry = e;
    });
    log.enter();
    equal(entry.message, 'entering activity', 'log.enter in subscription said: ' + entry.message);
})

test('log exit writes exit', function () {
    var entry;
    var log = testLog();
    log.subscribe(function (e) {
        if (e.exiting) {
            entry = e;
        }
    });
    log.enter().exit();

    equal(entry.message, 'exiting activity', 'log.exit in subscription said:' + entry.message);
})

test('log exit has elapsed time', function () {
    var log = testLog();
    var entry;
    log.subscribe(function (e) {
        if (e.exiting) {
            entry = e;
        }
    });

    log.enter(function () {
        pause(50);
    });

    ok(entry.elapsed() > 49, 'Enough time elapsed: ' + entry.elapsed() + 'ms');
})

test('log enter writes exception to entry on throw', function () {
    var log = testLog();
    var entry;
    log.subscribe(function (e) {
        if (e.error) {
            entry = e;
        }
    });

    try {
        log.enter(function () {
            throw new Error("Test Failure");
        });
    } catch (e) {
    }

    equal("Test Failure", entry.error.message, "exit should have exception as subject");
})

test('log note has correct calling method name and arguments', function () {
    var log = testLog();
    var entry;

    log.subscribe(function (e) {
        entry = e;
    });

    function testCall(value) {
        log.note("test");
    }

    testCall("testValue");

    equal(entry.method, "testCall", "caller was " + entry.method);
    equal(entry.arguments[0], "testValue", "first argument was " + entry.arguments[0]);
})

test('log enter has correct caller name and arguments', function () {
    var log = testLog();
    var entry;

    log.subscribe(function (e) {
        entry = e;
    });

    function testCall(value1) {
        log.enter();
    }

    testCall("testValue");

    equal(entry.method, "testCall", "caller was " + entry.method);
    equal(entry.arguments[0], "testValue", "first argument was " + entry.arguments[0]);
})

test('log entry has correct start time', function () {
    var log = testLog();
    var entry,
        testStart = new Date();

    log.subscribe(function(e) {
        entry = e;
    });

    log.note();

    ok(testStart < entry.time < new Date(), "Entry start time occured during test");
})

test('log entry has extensions from activity', function () {
    var log = testLog();
    var entry;
    log.subscribe(function (e) {
        entry = e;
    });

    log.with({ abc: 123 }).note("test");

    equal(entry.abc, 123, "extension abc set at " + entry.abc);
})

test('log entry has extensions from enter activity', function () {
    var log = testLog();
    var entry;
    log.subscribe(function (e) {
        if (!e.entering) {
            entry = e;
        }
    });

    var activity = log.with({ abc: 123 }).enter();

    activity.note("test");

    equal(entry.abc, 123, "extension abc set at " + entry.abc);
})

test('log entry does not throw on cyclic arguments', function () {
    var log = testLog();
    var entry;

    log.subscribe(function (e) {
        entry = e;
    });

    function testCall(value) {
        log.note("test");
    }

    var cyclic = {};
    cyclic.cycle = cyclic;
    testCall(cyclic);
    equal(entry.arguments, "Cyclic arguments");
})

test('log entry copy returns complete clone', function () {
    var log = testLog(),
        clone;

    log.subscribe(function (e) {
        clone = e.copy();
    });
    log.with({ test: "test extension" }).note("test hello");

    equal(clone.message, "test hello", "message is " + clone.message);
    equal(clone.test, "test extension", "first extension is " + clone.test);
})

test('can not continue with activity after exit', function () {
    var log = testLog();

    var activity = log.with({ abc: 123 }).enter();
    activity.exit();

    throws(function () {
        activity.note("test");
    }, null, "calling note on an exited activity should throw");
})

test('entered activity has no enter method', function() {
    var log = testLog();
    var activity = log.enter();

    equal(activity.enter, null);
})

test('unentered activity has no exit method', function () {
    var log = testLog();
    var activity = log.with();

    equal(activity.exit, null);
})

test('log entry has supplied stack', function() {
    var log = defines["log"](defines["bus"](), function () { return "Test stack"; }, extend);
    var entry;
    log.subscribe(function (e) {
        entry = e;
    });

    log.note("test");

    equal(entry.stack(), "Test stack");
})

test('log enter applies activity extensions to each entry', function() {
    var log = testLog();

    var activity = log.with({abc: 123}).enter();
    
    var entry = activity.note("test note");

    equal(entry.abc, 123);
})

test('log enter passes current activity into section', function() {
    var log = testLog();
    var entry;

    log.with({abc: 123}).enter(function(a) {
        entry = a.note("in method");
    });

    equal(entry.abc, 123);
})
