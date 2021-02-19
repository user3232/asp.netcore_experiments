
# Http state object (cookies)

> 1. **A cookie can only be overwritten (or deleted) by a subsequent cookie
>    exactly matching the name, path and domain of the original cookie.** 
> 2. If multiple cookies of the same name match a given request URI, one is
>    chosen by the browser. The more specific the path, the higher the
>    precedence. However precedence based on other attributes, including the
>    domain, is unspecified, and may vary between browsers.
> 3. The HTTP state object is called a cookie "for no compelling reason"
>    according to the preliminary specification from Netscape.
>
> [[Source: 3 Things About Cookies You May Not Know]](https://www.sitepoint.com/3-things-about-cookies-you-may-not-know/)
> [[Additional source: StackOverflow]](https://stackoverflow.com/questions/4056306/how-to-handle-multiple-cookies-with-the-same-name)



# Debugging and source code of Asp .Net Core

One can enable debugging with source code of Asp .Net Core libraries. To do this
there is need to add some configurations at `lunch.json` file.

Example `lunch.json` for launching Asp .net core webserver:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/netcoreapp3.1/SAuth2.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "justMyCode": false,
      "symbolOptions": {
        "searchMicrosoftSymbolServer": true,
        "searchNuGetOrgSymbolServer": true
      },
      "suppressJITOptimizations": true,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "COMPlus_ZapDisable": "1",
        "COMPlus_ReadyToRun": "0"
      }
    }
  ]
}
```

Important things are:

```json
"justMyCode": false,
"symbolOptions": {
  "searchMicrosoftSymbolServer": true
},
"suppressJITOptimizations": true,
"env": {
  "COMPlus_ZapDisable": "1",
  "COMPlus_ReadyToRun": "0"
}
```

More informations at:
* https://github.com/OmniSharp/omnisharp-vscode/wiki/Debugging-into-the-.NET-Framework-itself
* https://github.com/dotnet/designs/blob/main/accepted/2020/diagnostics/source-link.md
* https://devblogs.microsoft.com/dotnet/improving-debug-time-productivity-with-source-link/
* https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink
* https://carlos.mendible.com/2018/08/25/adding-sourcelink-to-your-net-core-library/

Asp .net core source code is at:
* https://github.com/dotnet/aspnetcore



# Turning off annoing references in code editor


For folder project, `.vscode/settings.json` must contain
`"csharp.referencesCodeLens.enabled": false`, for example:

```json
{
  "csharp.referencesCodeLens.enabled": false
}
```

If workspace is used, `workspace_name.code-workspace` must contain in settings
section `"csharp.referencesCodeLens.enabled": false`, for example:

```json
{
  "folders": 
  [
    {"path": "."}
  ],
  "settings": {
    "csharp.referencesCodeLens.enabled": false
  }
}
```


# Certificates for SNI

It is possible to have certificate for name e.g.:
* `10.111.121.240:3000`

But beware,:
* address must match name typed in web browser.
* For a private IP address you will only be able to use a self signed
  certificate or a local private CA, no public CA will sign this.

To make live simpler, one may:
* put the IP in `/etc/hosts` with whatever name 
* and get a certificate for that name instead.


## Info links

* https://medium.com/@antelle/how-to-generate-a-self-signed-ssl-certificate-for-an-ip-address-f0dd8dddf754
* https://www.openssl.org/docs/manmaster/man5/x509v3_config.html#Subject-Alternative-Name
* https://en.wikipedia.org/wiki/Subject_Alternative_Name
* https://access.redhat.com/documentation/en-us/red_hat_enterprise_linux/6/html/deployment_guide/sssd-ldap-domain-ip
* https://1.1.1.1 and its certs