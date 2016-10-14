var ko = require('knockout'),
    viewModel = require('./viewModel.coffee'),
    rfile = require('rfile'),
    view = rfile('./view.html')

require('./ko.extensions.coffee')(ko)

var scripts = document.querySelectorAll('script'),
    thisScriptLocation = scripts[scripts.length - 1].attributes.src.value

document.write('<link rel="stylesheet" href="' + thisScriptLocation.replace('.js', '.css') +  '">')
document.write('<meta name="viewport" conte' + 'nt="width=device-width">')
document.addEventListener('DOMContentLoaded', function() {
  document.getElementsByTagName('body')[0].innerHTML = view
  viewModel.pathname = window.location.protocol === 'file:' ? window.pathname : window.location.pathname
  viewModel.querystring = window.location.search
  viewModel.init()
  ko.applyBindings(viewModel)
})