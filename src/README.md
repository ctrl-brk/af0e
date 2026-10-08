## Packages Upgrade

List

`dotnet list "AF0E.slnx" package --outdated`

Upgrade
```
dotnet tool install --global dotnet-outdated-tool
dotnet outdated "AF0E.slnx" --upgrade
```

Verify
```
dotnet build "AF0E.slnx"
dotnet test "AF0E.slnx"
```
