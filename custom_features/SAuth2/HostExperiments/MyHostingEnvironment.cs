using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders; // IFileProvider



namespace SAuth2.HostExperiments
{
  // https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingEnvironment.cs
  public class MyHostingEnvironment : IWebHostEnvironment
  {
    public string EnvironmentName { get; set; } = "Developement";
    public string ApplicationName { get; set; } = default!;
    public string WebRootPath { get; set; } = default!;
    public IFileProvider WebRootFileProvider { get; set; } = default!;
    public string ContentRootPath { get; set; } = default!;
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
  }
}