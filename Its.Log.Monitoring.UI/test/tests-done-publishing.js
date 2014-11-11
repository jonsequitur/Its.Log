(function(mockTests) {
  if (typeof module === 'object') {
    module.exports = mockTests
  } else {
    window.fakeServer.respondWith('GET', '/tests/done/publishing',
      [200, { 'Content-Type': 'application/json' }, JSON.stringify(mockTests)]);
  }
})({
  Tests: [{
    Url: "/tests/done/publishing/readmodels_are_caught_up"
  }, {
    Url: "/tests/done/publishing/seller_dashboard_is_reachable"
  }, {
    Url: "/tests/done/publishing/offers-list-returns-offers"
  }]
})