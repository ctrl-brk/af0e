import {Component, OnDestroy, OnInit, signal} from '@angular/core';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss'
})
export class FooterComponent implements OnInit, OnDestroy {
  protected utcTime = signal('');
  protected nearMidnight = signal(false);
  private _clockInterval?: ReturnType<typeof setInterval>;

  ngOnInit() {
    this.tick();
    this._clockInterval = setInterval(() => this.tick(), 1000);
  }

  ngOnDestroy() {
    clearInterval(this._clockInterval);
  }

  private tick() {
    const now = new Date();
    const h = now.getUTCHours();
    const m = now.getUTCMinutes();
    const s = now.getUTCSeconds();
    this.utcTime.set(`${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')} UTC`);
    this.nearMidnight.set(h === 23 && m >= 55);
  }

  protected getYear(): string {
    const beginningYear = 2024;
    const currentYear = (new Date()).getUTCFullYear();

    if (currentYear === beginningYear)
      return currentYear.toString();

    return `${beginningYear}-${currentYear}`;
  }
}
