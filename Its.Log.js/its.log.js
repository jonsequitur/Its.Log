/*jshint browser:true, undef:true, evil:true, strict:false*/
/*
    its.log.js - publish/subscribe model logging and instrumentation for javascript
    Requires defined modules for a bus, stack and the extend function, or global variables for each.
    Also optionally uses JSON.stringify()
    In the absense of an AMD framework, this will assign to the global member 'log'
*/

(function (name, dependencies, definition) {
    if (typeof define === 'function' && typeof define.amd === 'object') {
        define(name, dependencies, definition);
    } else {
        this.log = definition(bus, stack, extend);
    }
})('log', ['bus', 'stack', 'extend'], function (bus, stack, extend) {
    var appStart = new Date();
    var supportsJson = true;
    try {
        JSON.stringify({ test: 'value' });
    } catch (e) {
        supportsJson = false;
    }

    var log = {
        with: function (extension) {
            return new LogActivity().with(extension);
        },
        note: function (subject) {
            return new LogActivity().note(subject, log.note.caller);
        },
        enter: function (section) {
            return new LogActivity().enter(section, log.enter.caller);
        },
        subscribe: function (subscription) {
            return bus.subscribe(subscription);
        }
    };
    log.include = log.with;

    function LogActivity() {
        if (!(this instanceof LogActivity)) {
            return new LogActivity();
        }

        var self = this,
            extensions = {},
            entries = [],
            active = true;

        function publish(entry) {
            var publishEntry = entry.copy();
            extend(publishEntry, extensions);
            entries.push(publishEntry);
            bus.publish(publishEntry);
            return entry;
        };

        function exit(error) {
            active = false;
            var start = entries[0];
            var entry = new LogEntry(error || 'exiting activity', start.method);
            entry.entries = entries;
            entry.startTime = start.time;
            entry.exiting = true;
            entry.stopTime = new Date();
            publish(entry);
        }

        this.note = function (subject, caller) {
            if (!active) {
                throw new Error('Note called on a completed activity');
            }
            var note = new LogEntry(subject, caller || self.note.caller);
            extend(note, extensions);
            return publish(note);
        };

        this.with = function (extension) {
            if (extension) {
                extend(extensions, extension);
            }
            return self;
        };
        this.include = this.with;

        this.enter = function (section, caller) {
            var entry = new LogEntry('entering activity', caller || self.enter.caller);
            entry.entering = true;
            publish(entry);

            self.exit = exit;

            if (!section) {
                self.enter = undefined;
                return self;
            }

            try {
                section(this);
                exit();
            } catch (e) {
                exit(e);
                throw e;
            }
            return null;
        };
    }

    function LogEntry(subject, caller) {
        if (!(this instanceof LogEntry)) {
            return new LogEntry(subject, caller);
        }

        var self = this;

        var addNote = function (subject) {
            if (!subject) {
                return;
            }
            if (typeof subject === 'string') {
                self.message = subject;
            } else if (subject instanceof Error) {
                self.error = subject;
            } else {
                extend(self, subject);
            }
        };

        var addArguments = function (caller) {
            try {
                if (caller.arguments) {
                    var args = Array.prototype.slice.call(caller.arguments);
                    if (supportsJson) {
                        JSON.stringify(args); // assure arguments are not cyclic
                    }
                    self.arguments = args;
                }
            } catch (e) {
                self.arguments = 'Cyclic arguments';
            }
        };

        this.method = caller.name || 'anonymous';
        this.time = new Date();

        addNote(subject);
        addArguments(caller);
        this.stack = function () {
            return stack({ e: self.error });
        };

        this.copy = function () {
            var result = new LogEntry(null, self.method);
            result.clone = true;
            extend(result, self);
            return result;
        };
        this.appStart = function () {
            return appStart;
        };
        this.appElapsed = function () {
            return self.time - appStart;
        };
        this.elapsed = function () {
            return Math.max(0, self.time - self.startTime);
        };
    }

    return log;
});
