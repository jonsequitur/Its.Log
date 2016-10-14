highlight = require('highlight.js')
formatJson = require "format-json"

module.exports = (ko) ->
  ko.bindingHandlers.highlight =
    init: ->
      controlsDecedantBindings: true
    update: (element, valueAccessor) ->
      val = ko.utils.unwrapObservable valueAccessor()
      if typeof val is 'object'
        val = formatJson.plain val
        element.classList.add 'json'

      element.innerText = val
      highlight.highlightBlock(element)