/*
    bus.js - A default, barebones pub/sub bus.
*/

(function (name, dependencies, definition) {
    if (typeof define === 'function' && typeof define.amd === 'object') {
        define(name, dependencies, definition);
    } else {
        this.bus = definition();
    }
})('bus', [], function () {
    var subscriptions = [];
    return {
        subscribe: function (callback) {
            return this.when().subscribe(callback);
        },
        when: function (condition) {
            return {
                subscribe: function (callback) {
                    var sub = function (payload) {
                        if (!condition || condition(payload)) {
                            callback(payload);
                        }
                    };
                    subscriptions.push(sub);

                    return {
                        unsubscribe: function () {
                            subscriptions = subscriptions.filter(function (next) {
                                return next != sub;
                            });
                        }
                    };
                }
            };
        },
        publish: function (subject) {
            subscriptions.forEach(function (sub) {
                try {
                    sub(subject);
                } catch (e) {
                    // Honey badger
                }
            });
        }
    };
});
