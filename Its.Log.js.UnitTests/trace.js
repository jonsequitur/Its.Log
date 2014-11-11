(function (window, console, log, qUnit) {
    var buffer = '',
        subscription;

    window.trace = function (msg) {
        log.with({ trace: true }).note(msg);
    };

    window.traceSetup = function () {
        flush();
        subscription = log.subscribe(function (entry) {
            if (entry.trace) {
                internalLog(formatLog(entry));
            }
        });
    };

    window.traceTeardown = function () {
        subscription.unsubscribe();
        flush();
    };

    function formatLog(entry) {
        return "Trace: " + entry.startTime + " " + entry.method + " -> " + entry.note;
    }

    function flush() {
        internalLog(buffer);
        buffer = '';
    }
    function internalLog(msg) {
        if (console && console.log && msg) {
            console.log(msg);
        }

        if (qUnit && qUnit.config && qUnit.config.current && qUnit.config.current.testName && window.ReSharperAjax) {
            window.ReSharperAjax("/testOutput", { test: qUnit.config.current.testName, message: msg + '\n' });
        } else if (!window.ReSharperAjax) {
            buffer += msg + '\n';
        }
    }
})(window, console, define ? defines['log'](defines['bus'](), null, defines['extend']()) : window.log, QUnit);
