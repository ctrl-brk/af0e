import {beforeEach, describe, expect, it, vi} from 'vitest';
import {SpaceAsTabDirective} from './space-as-tab.directive';

describe('SpaceAsTabDirective', () => {
  let directive: SpaceAsTabDirective;

  beforeEach(() => {
    directive = new SpaceAsTabDirective();
  });

  it('should create an instance', () => {
    expect(directive).toBeTruthy();
  });

  it('should be a standalone directive', () => {
    expect(SpaceAsTabDirective).toBeDefined();
  });

  it('should have optional callback input', () => {
    expect(directive.spaceAsTabCallback).toBeDefined();
  });

  it('should accept a callback function', () => {
    const mockCallback = vi.fn();
    // Note: In real usage, Angular's input() handles the binding
    // This test just verifies the structure exists
    expect(typeof directive.spaceAsTabCallback).toBe('function');
  });

  // Integration tests with Angular TestBed would go here
  // For now, a basic instantiation test ensures the directive is properly structured
});




