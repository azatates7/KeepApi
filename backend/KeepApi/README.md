<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>

    <!-- NotesController'daki /// <summary> yorumlarının Swagger UI'da görünmesi için -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <!-- Note.cs gibi dokümante edilmemiş public üyeler için CS1591 uyarısını bastırıyoruz -->
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

</Project>