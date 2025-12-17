import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RouterOutlet } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PrimeNG } from 'primeng/config';
import { AppComponent } from './app.component';
import { NotificationService } from '../shared/notification.service';

@Component({
  selector: 'app-header',
  standalone: true,
  template: '',
})
class HeaderComponentStub {}

@Component({
  selector: 'app-footer',
  standalone: true,
  template: '',
})
class FooterComponentStub {}

describe('App', () => {
  beforeEach(async () => {
    TestBed.overrideComponent(AppComponent, {
      set: {
        imports: [
          HeaderComponentStub,
          ToastModule,
          RouterOutlet,
          FooterComponentStub,
        ],
      },
    });

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
      ],
      providers: [
        MessageService,
        PrimeNG,
        NotificationService,
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
