import {environmentSecrets} from "./environment-secrets";

/**
 * Common configuration values shared across all environments
 */
export const commonEnvironment = {
  mapBoxKey: environmentSecrets.mapBoxKey,
  claimType: 'https://af0e.org/claims',
  auth0domain: 'dev-4l6joodw0kczibgl.us.auth0.com',
  auth0clientId: 'sNpxyLre7xkR55bd6kHXURvJSGLkzaRX',
  auth0audience: 'https://af0e.logbook.api',
};

