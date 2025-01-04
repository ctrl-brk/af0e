import {Component} from '@angular/core';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss'
})
export class FooterComponent {
  protected getYear() : string {
    const beginningYear = 2024;
    const currentYear = (new Date()).getUTCFullYear();

    if (currentYear ===  beginningYear)
      return currentYear.toString();

    return `${beginningYear}-${currentYear}`;
  }
}
