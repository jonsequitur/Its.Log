(function(mockTests) {
  if (typeof module === 'object') {
    module.exports = mockTests
  } else {
    window.fakeServer = sinon.fakeServer.create()
    window.fakeServer.autoRespond = true
    window.fakeServer.respondWith('GET', '',
      [200, { 'Content-Type': 'application/json' }, JSON.stringify(mockTests)]);
  }
})({
  "tests": [
    {
      "HttpMethod": "GET",
      "Constraints": {
        "environment": {
          "Pattern": "^(done)|(working)$"
        }
      },
      "Documentation": null,
      "RelativePath": "/tests/{environment}/Healthy",
      "ParameterDescriptions": [
        {
          "Name": "environment",
          "IsOptional": false
        }
      ]
    },
    {
      "HttpMethod": "GET",
      "Constraints": {},
      "Documentation": null,
      "RelativePath": "/tests/Fail",
      "ParameterDescriptions": []
    },
    {
      "HttpMethod": "GET",
      "Constraints": {
        "value": {
          "InnerConstraint": {
            "MaxLength": 1000
          }
        }
      },
      "Documentation": null,
      "RelativePath": "/tests/Pass/{value}",
      "ParameterDescriptions": [
        {
          "Name": "value",
          "IsOptional": true
        }
      ]
    }
  ],
  "_links": [
    "http://blammo.com/tests/Fail",
    "http://blammo.com/tests/Pass"
  ]
})