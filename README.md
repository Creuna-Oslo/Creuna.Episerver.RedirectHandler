# Creuna Redirect Handler
A 404 handler for EPiServer, based on the widely used BVNetworks' 404 handler.

It has been altered slightly for more control over urls and query strings, and has tests 
which prove its functionality.

## Installation
`Install-Package Creuna.EPiServer.RedirectHandler`

The package can be found in the [EPiServer Nuget Feed](http://nuget.episerver.com/).

## Release notes

- 4.0.0 - episerver updated to v10; urls with and without trailing slash are treated the same way
- Now handled special characters in from urls
- Proven compatibility with EPIServer 9.9.