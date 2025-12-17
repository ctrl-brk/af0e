import {getTestBed} from '@angular/core/testing';
import {BrowserTestingModule, platformBrowserTesting,} from '@angular/platform-browser/testing';

// Initialize Angular testing environment once; the Vitest builder already calls this,
// so swallow the duplicate-initialization error to stay compatible with both flows.
const testBed = getTestBed();
try {
  testBed.initTestEnvironment(
    BrowserTestingModule,
    platformBrowserTesting(),
  );
} catch (err) {
  if (!(err instanceof Error) || !err.message.includes('base providers')) {
    throw err;
  }
}

// Mock global objects if needed
(globalThis as any).CSS = {
  supports: (_query: string) => false,
  escape: (value: string) => value,
};

// Mock IntersectionObserver
(globalThis as any).IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return [];
  }
  unobserve() {}
};

// Mock ResizeObserver
(globalThis as any).ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
};
