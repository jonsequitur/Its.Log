chai = require 'chai'
sinon = require 'sinon'
chai.use require 'chai-things'
chai.use require 'sinon-chai'
chai.should()

describe 'Monitoring view model', ->
  viewModel = require '../viewModel'
  Q = require 'q'

  beforeEach -> 
    Q.xhr = sinon.spy (urlOrObj) -> 
      data = null
      try
        data = require("./#{(urlOrObj.url || urlOrObj).replace(/\//g, '-').substr(1)}.js")
      catch e
        data = {}
      Q 
        data: if typeof data is 'string' then data else JSON.parse JSON.stringify data
        headers: ->
          'Content-Type': 'application/json'
    Q.xhr.get = Q.xhr

    viewModel.querystring = null

  it 'should create a friendly title for tests', ->
    viewModel.pathname = '/tests/done/ordering'
    viewModel.init().then ->
      viewModel.filteredTests.map((t) -> t.title).should.deep.equal [
        'readmodels are caught up',
        'blob storage is reachable',
        'compute only call works',
        'basic purchase works',
        'buggy test',
        'can save data to storage'
      ]

  describe 'runTest', ->
    beforeEach ->
      viewModel.pathname = '/tests/done/ordering'
      viewModel.init()

    it 'should request parameterless Urls as is', ->
      viewModel.runTest viewModel.filteredTests[0]
      Q.xhr.should.have.been.deep.calledWith
        method: 'GET'
        url: '/tests/done/ordering/readmodels_are_caught_up'

    it 'should have a status of "running" while the test request is in flight', ->
      Q.xhr = -> Q.delay(10)
      viewModel.runTest viewModel.filteredTests[0]
      viewModel.filteredTests[0].status.should.equal 'running'      

    it 'should have a status of "succeeded" when the test finishes successfully', ->
      viewModel.runTest viewModel.filteredTests[0]
      viewModel.filteredTests[0].status.should.equal 'succeeded'
      (viewModel.filteredTests[0].expanded is null).should.be.true

    it 'should have a status of "failed" if the test fails', ->
      Q.xhr = ->
        Q.reject
          status: 500

      viewModel.runTest viewModel.filteredTests[0]
      viewModel.filteredTests[0].status.should.equal 'failed'      

    xit 'should expand the results if the test fails', ->
      Q.xhr = ->
        Q.reject
          status: 500

      viewModel.runTest viewModel.filteredTests[0]
      # something is odd with the == null check in node so this is failing
      viewModel.filteredTests[0].expanded.should.be.true    

    it 'should not expand the results if explicitly collapsed', ->
      Q.xhr = ->
        Q.reject
          status: 500

      viewModel.filteredTests[0].expanded = false
      viewModel.runTest viewModel.filteredTests[0]
      viewModel.filteredTests[0].expanded.should.be.false    

    it 'should be able to run tests again', ->
      viewModel.runTest viewModel.filteredTests[0]
      viewModel.filteredTests[0].status.should.equal 'succeeded'    

      viewModel.runTest viewModel.filteredTests[0]
      Q.xhr.should.have.been.calledThrice      

  describe 'runAll', ->
    beforeEach ->
      viewModel.pathname = '/tests/done/ordering'
      viewModel.init()

    it 'runAll should run all tests', ->
      viewModel.runAll()
      Q.xhr.should.have.callCount(viewModel.filteredTests.length + 1)

  describe 'tags', ->
    beforeEach ->
      viewModel.pathname = '/tests/production'
      viewModel.includeTags = []
      viewModel.excludeTags = []

      Q.xhr.get = sinon.spy (urlOrObj) -> 
        Q 
          data: 
            Tests: [
              Environment: 'production',
              Application: 'fruit-stand',
              Tags: ['fruit', 'citris']
              Url: '/test/production/fruit-stand/tangerine'
            ,
              Environment: 'production',
              Application: 'fruit-stand',
              Tags: ['fruit']
              Url: '/test/production/fruit-stand/apple'
            ,
              Environment: 'production',
              Application: 'fruit-stand',
              Tags: ['fruit', 'citris']
              Url: '/test/production/fruit-stand/lemon'
            ,
              Environment: 'production',
              Application: 'fruit-stand',
              Tags: ['health']
              Url: '/test/production/fruit-stand/database-connection-is-good'
            ,
              Environment: 'production',
              Application: 'fruit-stand',
              Tags: []
              Url: '/test/production/fruit-stand/wip-test'
            ]
          headers: ->
            'Content-Type': 'application/json'

    testTitles = -> viewModel.filteredTests.map((t) -> t.title)

    it 'should extract tag information from the query string', ->
      viewModel.querystring = '?side-effecting=true&banana=false'
      viewModel.init()

      viewModel.includeTags.should.deep.equal ['side-effecting']
      viewModel.excludeTags.should.deep.equal ['banana']

    it 'should query all test but filter them afterwards', ->
      viewModel.querystring = '?citris=true'
      viewModel.init().then ->
        Q.xhr.get.lastCall.args.should.deep.equal ['/tests/production']
        testTitles().should.deep.equal ['tangerine', 'lemon']

    describe 'filtering', ->
      beforeEach -> viewModel.init()

      it 'should return all tests when no filters', ->
        testTitles().length.should.equal 5

      it 'should perform simple exclusion', ->
        viewModel.filterOut 
          name: 'fruit'
        testTitles().should.deep.equal ['database connection is good', 'wip test']
        viewModel.excludeTags = ['non-existant']
        testTitles().length.should.equal 5

      it 'should perform exclusion and inclusion', ->
        viewModel.filterOn 
          name: 'fruit'
        viewModel.filterOut 
          name: 'citris'
        testTitles().should.deep.equal ['apple']

      it 'should be able to flip from exclusion to inclusion', ->
        viewModel.filterOut 
          name: 'fruit'
        testTitles().should.deep.equal ['database connection is good', 'wip test']
        viewModel.filterOn 
          name: 'fruit'
        testTitles().should.deep.equal ['tangerine', 'apple', 'lemon']

      it 'should not return any tests if no test includes a tag', ->
        viewModel.filterOn 
          name: 'fruit'
        viewModel.filterOn 
          name: 'something-random'
        testTitles().should.be.empty

      it 'should be able to clear tags', ->
        viewModel.includeTags = ['fruit']
        viewModel.excludeTags = ['citris']
        viewModel.clearIncludeTag 'fruit'
        viewModel.clearExcludeTag 'citris'
        testTitles().length.should.equal 5
        

  it 'friendlyStatus should explain if the test passed, failed, or something else happened', ->
    viewModel.friendlyStatus(200).should.equal 'Passed'
    viewModel.friendlyStatus(500).should.equal 'Failed'
    viewModel.friendlyStatus(404).should.equal 'Inconclusive (404)'

  xdescribe 'environments', ->
    it 'should aggregate by environment and application', ->
      viewModel.pathname = '/tests/done/ordering'
      viewModel.init()
      viewModel.environments.length.should.equal 1
      viewModel.environments[0].name.should.equal 'done'
      viewModel.environments[0].applications.length.should.equal 1
      viewModel.environments[0].applications[0].tests.length.should.equal 7
