
# Sessions and Cookies overview links


* https://medium.com/developers-tomorrow/understanding-how-cookie-and-session-in-javascript-3af858fa8112
* https://crypto.stackexchange.com/questions/47024/securing-http-session-ids-by-appending-hmac-hash
* https://django-registration.readthedocs.io/en/3.1.1/activation-workflow.html
* https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html


# C# Cookie Options

* **expires**: Setting a cookie for “foo=bar” to last 5 minutes, 
  using expires (https://mrcoles.com/blog/cookies-max-age-vs-expires/):

  ```js
  var d = new Date();
  d.setTime(d.getTime() + 5*60*1000); // in milliseconds
  document.cookie = 'foo=bar;path=/;expires='+d.toGMTString()+';';
  ```

* **max-age**: Setting a cookie for “foo=bar” to last 5 minutes, 
  using max-age (https://mrcoles.com/blog/cookies-max-age-vs-expires/):

  ```js
  document.cookie = 'foo=bar;path=/;max-age='+5*60+';';
  ```

* **HttpOnly**: attaches cookie header to response indicating 
  if cookie can by accessed by JS in browser

* **SameSite**: emits cookie header indicating allowed origins
  for browser to send cookie

* **Secure**: server and browser will send only with https



# The Set-Cookie and Cookie headers
https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies

The Set-Cookie HTTP response header sends cookies from 
the server to the user agent. A simple cookie is set like this:

```http
Set-Cookie: <cookie-name>=<cookie-value>
```

This shows the server sending headers to tell 
the client to store a pair of cookies:

```http
HTTP/2.0 200 OK
Content-Type: text/html
Set-Cookie: yummy_cookie=choco
Set-Cookie: tasty_cookie=strawberry

[page content]
```

Then, with every subsequent request to the server, 
the browser sends back all previously stored cookies 
to the server using the Cookie header.

```http
GET /sample_page.html HTTP/2.0
Host: www.example.org
Cookie: yummy_cookie=choco; tasty_cookie=strawberry    
```   

# Define the lifetime of a cookie

The lifetime of a cookie can be defined in two ways:

1. Session cookies are deleted when the current session ends. 
    The browser defines when the "current session" ends, 
    and some browsers use session restoring when restarting, 
    which can cause session cookies to last indefinitely long.

2. Permanent cookies are deleted at a date specified by the 
    Expires attribute, or after a period of time specified 
    by the Max-Age attribute.

For example:

```http
Set-Cookie: id=a3fWa; Expires=Thu, 31 Oct 2021 07:28:00 GMT;
```

> Note: When an Expires date is set, the time and date set 
> is relative to the client the cookie is being set on, 
> not the server.

If your site authenticates users, it should regenerate and 
resend session cookies, even ones that already exist, 
whenever the user authenticates. 

# Restrict access to cookies

There are a couple of ways to ensure that cookies are sent securely 
and are not accessed by unintended parties or scripts: 
* the Secure attribute 
* and the HttpOnly attribute.

Here is an example:

```http
Set-Cookie: id=a3fWa; Expires=Thu, 21 Oct 2021 07:28:00 GMT; Secure; HttpOnly
```

# Define where cookies are sent

The Domain and Path attributes define the scope of the cookie: 
* what URLs the cookies should be sent to.

## Domain attribute

The Domain attribute specifies which hosts are allowed to receive the cookie. 
* If unspecified, it defaults to the same host that set the cookie, excluding subdomains. 
* If Domain is specified, then subdomains are always included. 

> **Note**: Therefore, specifying Domain is less restrictive than omitting it. 
> However, it can be helpful when subdomains need to share information 
> about a user.

For example:, 
* if `Domain=mozilla.org` is set, 
* then cookies are available on subdomains like `developer.mozilla.org`.

## Path attribute

The Path attribute indicates a URL path that must exist in 
the requested URL in order to send the Cookie header. 
The %x2F ("/") character is considered a directory separator, 
and subdirectories match as well.

For example, if `Path=/docs` is set, these paths match:

* /docs
* /docs/Web/
* /docs/Web/HTTP

# SameSite attribute

The SameSite attribute lets servers specify whether/when cookies 
are sent with cross-origin requests (where Site is defined by 
the registrable domain), which provides some protection against 
cross-site request forgery attacks (CSRF).

It takes three possible values: Strict, Lax, and None. 

* With Strict, the cookie is sent only to the same site 
  as the one that originated it; 

* Lax is similar, except that cookies are sent when the 
  user navigates to the cookie's origin site, for example, 
  by following a link from an external site; 

* None specifies that cookies are sent on both originating 
  and cross-site requests, but only in secure contexts 
  (i.e. if SameSite=None then the Secure attribute must also be set). 

* If no SameSite attribute is set then the cookie is treated as Lax.

Here is an example:

```http
Set-Cookie: mykey=myvalue; SameSite=Strict
```

# Kestrel server bind addresses

Kestrel urls:

For Kestrel final listen endpoints configuration is taken from
KestrelServerOptions (DI service, options pattern) with
Listen or ListenUnixSocket methods (allowing configuration of
prefixes and ports)

> By default, ASP.NET Core binds to:
> 
> * http://localhost:5000
> * https://localhost:5001 (when a local development certificate is present)
> 
> Specify URLs using the:
> 
> * ASPNETCORE_URLS environment variable.
> * --urls command-line argument.
> * urls host configuration key.
> * UseUrls extension method.
>
> source:
> https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints

GenericHost urls:

> Key: urls
> Type: string
> Default: http://localhost:5000 and https://localhost:5001
> Environment variable: <PREFIX_>URLS
>                       (ASPNETCORE_URLS)
> 
> To set this value, use the environment variable or call UseUrls:
> 
>   ```cs
>   webBuilder.UseUrls("http://*:5000;http://localhost:5001;https://hostname:5002");
>   ```
> 
> Kestrel has its own endpoint configuration API. 
> For more information, see: 
> https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-5.0
> 
> Source: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0#urls

Links:
* Host listen urls configuration options:
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?#urls
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?#server-urls
* kestrel:
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?configureiconfiguration
* configuration which have urls key:
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration
* options "pattern":
* https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options
* defining server urls: (adds values to configuration urls key)
* https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.hostingabstractionswebhostbuilderextensions.useurls
