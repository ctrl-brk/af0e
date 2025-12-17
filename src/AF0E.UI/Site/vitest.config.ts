import {defineConfig} from 'vitest/config';
// import {playwright} from '@vitest/browser-playwright';

export default defineConfig({
  test: {
    globals: false,
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
    // Browser mode - commented out in favor of jsdom for faster unit tests
    // browser: {
    //   provider: playwright(),
    //   instances: [{browser: 'chromium'}]
    // },
    coverage: {
      provider: 'v8',
      exclude: [
        'src/**/*.spec.ts',
        'src/**/*.d.ts',
        'src/test-setup.ts',
        'src/main.ts',
        'src/environments/**',
        'src/app/**/*.model.ts',
        'src/app/**/*.enum.ts'
      ],
      thresholds: {
        lines: 60,
        functions: 60,
        branches: 60,
        statements: 60
      }
    }
  }
});
