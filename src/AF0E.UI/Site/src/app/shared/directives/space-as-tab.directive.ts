import {Directive, HostListener, input} from '@angular/core';

/**
 * Directive that makes the Space key behave like a Tab key.
 * Useful for quick data entry in forms.
 *
 * Usage: Add [spaceAsTab] to any input element
 * Example: <input pInputText formControlName="field" spaceAsTab />
 *
 * With callback: <input pInputText formControlName="field" spaceAsTab [spaceAsTabCallback]="myCallback" />
 */
@Directive({
  selector: '[spaceAsTab]',
  standalone: true
})
export class SpaceAsTabDirective {
  /**
   * Optional callback function to execute before moving to the next field.
   * Useful for triggering lookups, validations, or other side effects.
   */
  spaceAsTabCallback = input<(() => void) | undefined>();

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === ' ' || event.key === 'Spacebar') {
      event.preventDefault();

      // Execute callback if provided
      const callback = this.spaceAsTabCallback();
      if (callback) {
        callback();
      }

      // Get the current element
      const currentElement = event.target as HTMLElement;

      // Find all focusable elements in the document
      const focusableElements = this.getFocusableElements();
      const currentIndex = focusableElements.indexOf(currentElement);

      if (currentIndex > -1 && currentIndex < focusableElements.length - 1) {
        // Move to the next focusable element
        const nextElement = focusableElements[currentIndex + 1] as HTMLElement;
        nextElement.focus();

        // If it's an input element, select its content
        if (nextElement instanceof HTMLInputElement || nextElement instanceof HTMLTextAreaElement) {
          nextElement.select();
        }
      }
    }
  }

  private getFocusableElements(): HTMLElement[] {
    const selector = [
      'input:not([disabled]):not([type="hidden"])',
      'select:not([disabled])',
      'textarea:not([disabled])',
      'button:not([disabled])',
      'a[href]',
      '[tabindex]:not([tabindex="-1"])'
    ].join(', ');

    const elements = Array.from(document.querySelectorAll(selector)) as HTMLElement[];

    // Filter out PrimeNG UI elements that shouldn't be in tab order
    const excludedClasses = [
      'p-fieldset-toggle-button',
      'p-datatable-sortable-column',
      'p-menubar-button',
      'p-hidden-focusable',
      'p-select-label',           // PrimeNG select dropdown labels
      'p-menubar-root-list'       // Menu navigation lists
    ];

    return elements.filter(el =>
      !excludedClasses.some(className => el.classList.contains(className))
    );
  }
}
