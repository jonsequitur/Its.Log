(function(mockTests) {
  if (typeof module === 'object') {
    module.exports = mockTests
  } else {
    window.fakeServer.respondWith('GET', '/tests/environments',
      [200, { 'Content-Type': 'application/json' }, JSON.stringify(mockTests)]);
  }
})(['done', 'production'])