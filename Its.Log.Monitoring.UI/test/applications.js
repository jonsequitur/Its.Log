(function(mockTests) {
  if (typeof module === 'object') {
    module.exports = mockTests
  } else {
    window.fakeServer.respondWith('GET', '/tests/applications',
      [200, { 'Content-Type': 'application/json' }, JSON.stringify(mockTests)]);
  }
})(['ordering', 'publishing'])