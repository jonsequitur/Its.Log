ko = require("./lib/ko.es5")(require "knockout")
Q = require("q-xhr")((unless typeof window is 'undefined' then window.XMLHttpRequest else null), require("q"))
require "knockout.validation"

withQueryParam = (url, name, val) ->
  url + (url.indexOf '?' is -1 ? '?' : '&') + encodeURIComponent(name) + '=' + encodeURIComponent(val)

tests = ko.observableArray([])

module.exports = ko.observableModel
  init: ->
    [empty, @testRoot, @environment, @application] = @pathname.split '/'

    params = require('node-jquery-deparam')((@querystring || '').substr(1))
    for tag, included of params
      if included then @includeTags.push tag else @excludeTags.push tag

    Q.xhr.get(@pathname).then (req) =>
      req.data.Tests?.forEach (test) ->
        test.title = test.Url.split('/')
          .filter((p) -> !!p and p.indexOf('{') is -1)
          .reverse()[0]
          .replace(/[\-_]/g, ' ')
          .trim()

        ko.utils.defineObservableProperty test, 'request'
        ko.utils.defineObservableProperty test, 'expanded', null
        ko.utils.defineObservableProperty test, 'result'
        Object.defineProperty test, 'status',
          get: ko.computed ->
            if test.request
              if test.request.isPending()
                return 'running'
              else if test.request.isRejected()
                return 'failed'
              'succeeded'

        test.Tags = test.Tags || []

      tests req.data.Tests

  testResult: (test) ->
    state = test.request?.inspect()
    state?.value or state?.reason

  runTest: (test) ->
    if !test.request or !test.request.isPending()
      handle = (req) ->
        test.result = 
          status: req.status
          data: req.data
          headers: req.headers()
        unless test.expanded?
          test.expanded = req.status >= 300
        test._request.notifySubscribers()

      test.request = Q.xhr(
        method: test.HttpMethod or 'GET'
        url: test.Url
      )
      test.request.then handle, handle
    test.request

  runAll: (app) ->
    if app?.tests?
      app.tests.forEach @runTest.bind(@)
    else
      @filteredTests.forEach @runTest.bind(@)

  environment: undefined
  application: undefined
  includeTags: []
  excludeTags: []

  clearIncludeTag: (tag) ->
    includeIdx = @includeTags.indexOf tag
    @includeTags.splice includeIdx unless includeIdx is -1
  clearExcludeTag: (tag) ->
    excludeIdx = @excludeTags.indexOf tag
    @excludeTags.splice excludeIdx unless excludeIdx is -1

  filterOn: (tagData) ->
    @clearExcludeTag tagData.name
    @includeTags.push tagData.name
  filterOut: (tagData) ->
    @clearIncludeTag tagData.name
    @excludeTags.push tagData.name
  clearFilter: (tagData) ->
    @clearIncludeTag tagData.name
    @clearExcludeTag tagData.name

  friendlyStatus: (status) ->
    if status is 200
      'Passed'
    else if status is 500
      'Failed'
    else
      "Inconclusive (#{status})"


Object.defineProperty module.exports, 'filteredTests',
  get: ko.computed ->
    tests().filter (test) =>
      @excludeTags.every((x) -> test.Tags.indexOf(x) < 0) and
      (!@includeTags.length or @includeTags.every((i) -> test.Tags.indexOf(i) >= 0))
  , module.exports
  enumerable: true

Object.defineProperty module.exports, 'environments',
  get: ko.computed ->
    envs = []
    @filteredTests.forEach (t) ->
      env = envs.filter((e) -> e.name is t.Environment)[0]
      unless env
        env = ko.observableModel
          name: t.Environment
          applications: []
        envs.push env

      app = env.applications.filter((a) -> a.name is t.Application)[0]
      unless app
        app = ko.observableModel
          name: t.Application
          tests: []
        env.applications.push app

      app.tests.push t
      return

    envs
  , module.exports
  enumerable: true

Object.defineProperty module.exports, 'tags',
  get: ko.computed ->
    tags = []
    tests().forEach (test) =>
      test.Tags.forEach (tag) ->
        tagData = tags.filter((t) -> t.name is tag)[0]
        unless tagData
          tagData = ko.observableModel
            name: tag
            count: 0
          tags.push tagData
        tagData.count += 1
    tags.sort (a, b) -> if a.name < b.name then -1 else 1
  , module.exports
  enumerable: true
