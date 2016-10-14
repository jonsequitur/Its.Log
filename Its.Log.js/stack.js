/*
    stack.js - A default, poorly implemented stack trace getter.
    I would recommend instead using something 3rd party, like stacktrace.js
*/

(function (name, dependencies, definition) {
    if (typeof define === 'function' && typeof define.amd === 'object') {
        define(name, dependencies, definition);
    } else {
        this.stack = definition();
    }
})('stack', [], function () {
    return function (options) {
        if (!options || !options.bypassError) { // pretty much just for testing
            if (options && options.e instanceof Error && options.e.stack) {
                return options.e.stack;
            }
            try {
                throw new Error();
            } catch(e) {
                // if the browser supports automatic stack on the error, use that.
                if (e.stack) {
                    return e.stack;
                }
            }
        }

        var s = [];
        var c = arguments.callee;
        while (c) {
            s.push(c);
            c = c.caller;
        }
        return s;
    };
});
