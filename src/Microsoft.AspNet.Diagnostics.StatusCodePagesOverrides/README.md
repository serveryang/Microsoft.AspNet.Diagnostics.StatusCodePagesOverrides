Microsoft.AspNet.Diagnostics.StatusCodePagesOverrides
=============
  Override StatusCodePages, Add `app.UseStatusCodePagesWithRedirects("~/404.html", 404)` method 
  which redirect to specified page by pass status code.
  support asp.net 5 rc1-final.

## How to use
* Install package from nuget.
    ```
    Install-Package Microsoft.AspNet.Diagnostics.StatusCodePagesOverrides -Pre
    ```
* Modify `Startup.cs` file.
    * `using Microsoft.AspNet.Diagnostics.StatusCodePagesOverrides`
    * Add `app.UseStatusCodePagesWithRedirects("~/404.html", 404);` in `Configure` method.