# localtunnel-client

.NET implementation of a tunnel client for [localtunnel.me](localtunnel.me).

## Getting Started

Let's get started with starting up a simple tunnel. In the following command
we open a proxy tunnel with a custom subdomain name (my-subdomain) and let
the requests proxy to example.com (HTTPS).

```
localtunnel --subdomain my-subdomain --host example.com --port 443 https
```

> ![](https://i.imgur.com/cX476cI.png)

## Features

#### Open Webbrowser

If you are developing an web application or something else, you can put the --browser option onto
your command, and the client will open your browser with the subdomain.

#### Dashboard

The client records all recent connections made and shows where they pointed at. You can also disable
the dashboard if needed.

#### HTTP header manipulation

The client fetches HTTP headers during the request and transforms the Host header to point onto the target domain
to emulate a real proxy. If you want to disable this to make a passthrough proxy, specify the `--passthrough` option.

## CLI

You can change the options as you need, here is a list of options the client offers:

```
Usage:
  Localtunnel [options] [command]

Options:
  -v, --verbose                                  Enables detailed verbose output.
  -b, --browser                                  If specified, opens the webpage in the browser.
  --no-dashboard                                 If specified, disables the dashboard.
  -c, --max-connections <max-connections>        The number of maximum allowed connections. [default: 10]
  -d, --subdomain <subdomain>                    The name of the subdomain to use, if not specified a random subdomain
                                                 name is used.
  -s, --server <server>                          The hostname of the server to use. [default: https://localtunnel.me/]
  -h, --host <host>                              The host to proxy requests to. [default: localhost]
  -p, --port <port>                              The port to proxy requests to. [default: 80]
  --receive-buffer-size <receive-buffer-size>    The minimum number of bytes to use for the receive buffer. [default:
                                                 65536]
  --passthrough                                  If specified, the request is proxied as received and no HTTP headers
                                                 are reinterpreted. [default: False]
  --version                                      Show version information
  -?, -h, --help                                 Show help and usage information

Commands:
  http     Starts a tunnel that exposes a HTTP server.
  https    Starts a tunnel that exposes a HTTPS server.
  ```
  
  ### Additional notes
  
  If you use a self-signed certificate for SSL, you can pass the `--allow-untrusted-certificates` option **AFTER** the `https` verb  to bypass the SSL verification.
