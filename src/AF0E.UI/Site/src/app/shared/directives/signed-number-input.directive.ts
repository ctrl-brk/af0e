import {Directive, ElementRef, HostListener} from '@angular/core';

@Directive({
  selector: '[signedNumberInput]',
  standalone: true
})
export class SignedNumberInputDirective {
  constructor(private readonly elementRef: ElementRef<HTMLInputElement>) {}

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (this.isShortcutKey(event) || this.isNavigationKey(event.key)) {
      return;
    }

    if (/^[0-9]$/.test(event.key)) {
      return;
    }

    if (event.key === '+' || event.key === '-') {
      const input = this.elementRef.nativeElement;
      const value = input.value;
      const selectionStart = input.selectionStart ?? 0;
      const selectionEnd = input.selectionEnd ?? 0;
      const hasLeadingSign = /^[+-]/.test(value);

      const signWouldBeAtStart = selectionStart === 0;
      const replacingCurrentSign = hasLeadingSign && selectionStart === 0 && selectionEnd > 0;

      if (signWouldBeAtStart && (!hasLeadingSign || replacingCurrentSign)) {
        return;
      }
    }

    event.preventDefault();
  }

  @HostListener('paste', ['$event'])
  onPaste(event: ClipboardEvent): void {
    const pastedText = event.clipboardData?.getData('text') ?? '';
    this.replaceSelectionWith(pastedText, event);
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent): void {
    const droppedText = event.dataTransfer?.getData('text') ?? '';
    this.replaceSelectionWith(droppedText, event);
  }

  @HostListener('input')
  onInput(): void {
    const input = this.elementRef.nativeElement;
    const sanitized = this.sanitize(input.value);

    if (sanitized !== input.value) {
      input.value = sanitized;
      input.dispatchEvent(new Event('input', {bubbles: true}));
    }
  }

  private replaceSelectionWith(text: string, event: Event): void {
    event.preventDefault();

    const input = this.elementRef.nativeElement;
    const selectionStart = input.selectionStart ?? input.value.length;
    const selectionEnd = input.selectionEnd ?? input.value.length;

    const combined = `${input.value.slice(0, selectionStart)}${text}${input.value.slice(selectionEnd)}`;
    input.value = this.sanitize(combined);
    input.setSelectionRange(input.value.length, input.value.length);
    input.dispatchEvent(new Event('input', {bubbles: true}));
  }

  private sanitize(value: string): string {
    if (!value) {
      return '';
    }

    const leadingSign = value[0] === '+' || value[0] === '-' ? value[0] : '';
    const digitsOnly = value.replace(/\D/g, '');
    return `${leadingSign}${digitsOnly}`;
  }

  private isShortcutKey(event: KeyboardEvent): boolean {
    return event.ctrlKey || event.metaKey || event.altKey;
  }

  private isNavigationKey(key: string): boolean {
    return [
      'Backspace',
      'Delete',
      'Tab',
      'Enter',
      'Escape',
      'ArrowLeft',
      'ArrowRight',
      'ArrowUp',
      'ArrowDown',
      'Home',
      'End'
    ].includes(key);
  }
}

