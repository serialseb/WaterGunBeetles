# 💦🔫🐞
_Load-testing made easy_

WaterGunBeetles allows you to write load-testing code using .net core,
and deploy little beetles in AWS lambdas to attack your systems, but only
with water guns, because those are more fun!

## Getting started

Start by installing the dotnet templates to create a new WaterGunBeetles
project.

```bash
dotnet new -i WaterGunBeetles.Templates.CSharp
``` 

You can now create a new project using the provided template.

```bash
dotnet new beetles --output MyLoadTest
cd MyLoadTest
dotnet restore
```

To run your load test, nothing could be easier!

```bash
dotnet beetles squirt --rate 100 --duration 10m --rebuild
```

That's it, a swarm of lovely lady bugs will start squirting at your system!
