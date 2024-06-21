# Configuration

Most aspects of configuration for Project Tanager related applications will be
controlled by through the standard [ASP.NET application configuration
sources](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

* Commonly, configuration settings will be provided through an
  `appsettings.json` file.
* Environment-specific files are supported via ASP.NET, and values can
  alternately be overridden through environment variables.

For example, logging using Serilog can be configured thusly in an
`appsettings.json` file:

```json
{
  "Serilog": {
      "Using": [ "Serilog.Sinks.File", "Serilog.Sinks.Console" ],
      "MinimumLevel": {
          "Default": "Information"
      },
      "WriteTo": [
          {
              "Name": "Console",
              "Args": {
                  "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3} {Message:lj}{Exception}{NewLine}"
              }
          }
      ]
  }
}
```

Rather than modify the file, one can use an environmental variable override at
runtime. For example, to set the logging level to `DEBUG`, and change the output
template, start the application with the following environment variables set:

```shell
Serilog_MinimumLevel_Default="Debug"
Serilog_WriteTo_0_Args_outputTemplate="{Message;lj}
```
