## Packages Upgrade

Prerequisites
```
npm install -g @angular/cli@latest
```
List
```
npm outdated
```

Upgrade (<i>haven't found an automated way yet</i>)<br>
Udate `package.json` with the latest versions of the packages, making sure they work together.
```
npm install
```

Verify
```
npm run build:test
```
