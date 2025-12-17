# AFØE.org

Angular client UI

## Setup

### Environment Secrets

Before running the application, you need to create your environment secrets file:

1. Copy `src/environments/environment-secrets.template.ts` to `src/environments/environment-secrets.ts`
2. Fill in your actual API keys and secrets
3. The `environment-secrets.ts` file is in `.gitignore` and will not be committed

```bash
# Windows PowerShell
Copy-Item src/environments/environment-secrets.template.ts src/environments/environment-secrets.ts
```

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
