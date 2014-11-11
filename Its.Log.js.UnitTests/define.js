(function (exports) {
    exports.defines = {};
    exports.define = function(name, dependencies, ctor) {
        defines[name] = ctor;
    };
    exports.define.amd = {};
})(window);