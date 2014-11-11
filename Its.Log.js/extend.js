/*
    extend.js - A default, poorly implemented extend method.
    I would recommend instead using something 3rd party, like $.extend() from jQuery
*/

(function (name, dependencies, definition) {
    if (typeof define === 'function' && typeof define.amd === 'object') {
        define(name, dependencies, definition);
    } else {
        this.extend = definition();
    }
})('extend', [], function() {
    return function(target) {
        var args = Array.prototype.slice.call(arguments);
        target = target || {};
        for (var a in args) {
            if (a === "0" || !args.hasOwnProperty(a))
                continue;
            var arg = args[a];

            for (var p in arg) {
                if (!arg.hasOwnProperty(p))
                    continue;

                target[p] = arg[p];
            }
        }

        return target;
    };
});
