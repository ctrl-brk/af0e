import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {HamEventsService} from './ham-events.service';

describe('HamEventsService', () => {
  let service: HamEventsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        HamEventsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(HamEventsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  it('should fetch events since the requested date and normalize dates', () => {
    const since = new Date('2026-09-01T12:34:56Z');

    service.getEvents(since).subscribe(events => {
      expect(events).toEqual([
        {
          id: 1,
          eventType: 'E',
          name: 'Special Event',
          startDate: new Date('2026-09-02T00:00:00Z'),
          endDate: null
        }
      ]);
    });

    const request = httpMock.expectOne('/api/v1/hamevents/list?since=2026-09-01T12:34:56.000Z');
    expect(request.request.method).toBe('GET');
    request.flush([
      {
        id: 1,
        eventType: 'E',
        name: 'Special Event',
        startDate: '2026-09-02T00:00:00Z',
        endDate: null
      }
    ]);
  });

  it('should fetch all events without a since query', () => {
    service.getEvents(null).subscribe(events => {
      expect(events).toEqual([]);
    });

    const request = httpMock.expectOne('/api/v1/hamevents/list');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('should fetch active events and normalize nullable dates', () => {
    const response = [
      {
        id: 1,
        eventType: 'E',
        name: 'Special Event',
        startDate: '2026-09-01T00:00:00Z',
        endDate: null
      },
      {
        id: 2,
        eventType: 'D',
        name: 'DXpedition',
        startDate: null,
        endDate: '2026-09-30T00:00:00Z'
      }
    ];

    service.getActiveEvents().subscribe(events => {
      expect(events).toHaveLength(2);
      expect(events[0].startDate).toEqual(new Date('2026-09-01T00:00:00Z'));
      expect(events[0].endDate).toBeNull();
      expect(events[1].startDate).toBeNull();
      expect(events[1].endDate).toEqual(new Date('2026-09-30T00:00:00Z'));
    });

    const request = httpMock.expectOne('/api/v1/hamevents/active');
    expect(request.request.method).toBe('GET');
    request.flush(response);
  });

  it('should preserve an empty event list', () => {
    service.getActiveEvents().subscribe(events => {
      expect(events).toEqual([]);
    });

    const request = httpMock.expectOne('/api/v1/hamevents/active');
    request.flush([]);
  });
});
