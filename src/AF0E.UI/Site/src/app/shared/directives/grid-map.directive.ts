import {AfterViewInit, Directive, ElementRef, Input} from '@angular/core';

@Directive({
  selector: 'a[gridMap]',
  standalone: true
})
export class GridMapDirective implements AfterViewInit {
  @Input() grid: string = '';

  constructor(private el: ElementRef<HTMLAnchorElement>) {}

  ngAfterViewInit() {
    const innerText = this.el.nativeElement.textContent || '';
    if (innerText) {
      //mobile
      //this.el.nativeElement.href = `https://www.karhukoti.com/maidenhead-grid-square-locator/?grid=${innerText}`;
      //desktop
      this.el.nativeElement.href = `https://www.karhukoti.com/maidenhead-grid-square-locator-desktop-map/?grid=${innerText}`;
    }
  }
}
