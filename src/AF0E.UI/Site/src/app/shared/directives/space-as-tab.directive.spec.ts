import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';
import {SpaceAsTabDirective} from './space-as-tab.directive';
import {Component, DebugElement} from '@angular/core';
import {ComponentFixture, TestBed} from '@angular/core/testing';
import {By} from '@angular/platform-browser';

@Component({
  template: `
    <div>
      <input id="input1" type="text" spaceAsTab />
      <input id="input2" type="text" spaceAsTab />
      <input id="input3" type="text" spaceAsTab />
    </div>
  `,
  standalone: true,
  imports: [SpaceAsTabDirective]
})
class TestComponent {}

describe('SpaceAsTabDirective', () => {
  let fixture: ComponentFixture<TestComponent>;
  let component: TestComponent;
  let inputElements: DebugElement[];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    inputElements = fixture.debugElement.queryAll(By.css('input'));
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should create the directive', () => {
    const directiveInstances = fixture.debugElement.queryAll(By.directive(SpaceAsTabDirective));
    expect(directiveInstances.length).toBe(3);
  });

  it('should move focus to next element on space key press', () => {
    const input1 = inputElements[0].nativeElement as HTMLInputElement;
    const input2 = inputElements[1].nativeElement as HTMLInputElement;

    input1.focus();
    expect(document.activeElement).toBe(input1);

    // Simulate space key press
    const event = new KeyboardEvent('keydown', {
      key: ' ',
      bubbles: true,
      cancelable: true
    });
    input1.dispatchEvent(event);

    fixture.detectChanges();

    // Focus should move to input2
    expect(document.activeElement).toBe(input2);
  });

  it('should have callback input signal', () => {
    // Get the directive instance
    const directiveInstance = inputElements[0].injector.get(SpaceAsTabDirective);

    // Verify the callback signal exists and returns undefined when not bound
    expect(directiveInstance.spaceAsTabCallback).toBeDefined();
    expect(typeof directiveInstance.spaceAsTabCallback).toBe('function');
    expect(directiveInstance.spaceAsTabCallback()).toBeUndefined();
  });

  it('should prevent default space behavior', () => {
    const input1 = inputElements[0].nativeElement as HTMLInputElement;
    input1.focus();

    const event = new KeyboardEvent('keydown', {
      key: ' ',
      bubbles: true,
      cancelable: true
    });
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

    input1.dispatchEvent(event);

    expect(preventDefaultSpy).toHaveBeenCalled();
  });
});
