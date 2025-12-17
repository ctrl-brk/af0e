import {TestBed} from '@angular/core/testing';
import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {LogbookService} from './logbook.service';
import {QsoSummaryModel} from '../models/qso-summary.model';
import {SortDirection} from '../shared/sort-direction.enum';

describe('LogbookService', () => {
  let service: LogbookService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        LogbookService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(LogbookService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  describe('getQsoSummaries', () => {
    it('should fetch QSO summaries with correct parameters', () => {
      const mockResponse = {
        totalCount: 2,
        contacts: [
          { id: 1, call: 'W1AW', band: '20m', mode: 'SSB', date: new Date() },
          { id: 2, call: 'K3LR', band: '40m', mode: 'CW', date: new Date() }
        ] as QsoSummaryModel[]
      };

      const minDate = new Date('2025-01-01');
      const maxDate = new Date('2025-12-31');

      service.getQsoSummaries(null, 0, 50, SortDirection.Descending, 'date', [minDate, maxDate])
        .subscribe(response => {
          expect(response.totalCount).toBe(2);
          expect(response.contacts.length).toBe(2);
          expect(response.contacts[0].call).toBe('W1AW');
        });

      const req = httpMock.expectOne(req => req.url.includes('/api/v1/logbook'));

      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should include call parameter when provided', () => {
      const mockResponse = { totalCount: 0, contacts: [] };
      const minDate = new Date('2025-01-01');
      const maxDate = new Date('2025-12-31');

      service.getQsoSummaries('W1AW', 0, 50, SortDirection.Descending, 'date', [minDate, maxDate])
        .subscribe();

      const req = httpMock.expectOne(request =>
        request.url.includes('/api/v1/logbook/W1AW')
      );

      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle ascending sort order', () => {
      const mockResponse = { totalCount: 0, contacts: [] };
      const minDate = new Date('2025-01-01');
      const maxDate = new Date('2025-12-31');

      service.getQsoSummaries(null, 0, 50, SortDirection.Ascending, 'call', [minDate, maxDate])
        .subscribe();

      const req = httpMock.expectOne(request =>
        request.url.includes('/api/v1/logbook')
      );

      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getQso', () => {
    it('should fetch a single QSO by ID', () => {
      const mockQso: any = {
        id: 123,
        call: 'W1AW',
        band: '20m',
        mode: 'SSB',
        date: new Date('2025-12-05T10:00:00Z'),
        rstSent: '59',
        rstRcvd: '59'
      };

      service.getQso(123).subscribe(qso => {
        expect(qso.id).toBe(123);
        expect(qso.call).toBe('W1AW');
      });

      const req = httpMock.expectOne('/api/v1/logbook/qso/123');
      expect(req.request.method).toBe('GET');
      req.flush(mockQso);
    });
  });

  describe('lookupPartial', () => {
    it('should search for partial callsigns', () => {
      const mockResults = ['W1AW', 'W1ABC', 'W1XYZ'];

      service.lookupPartial('W1A').subscribe(results => {
        expect(results.length).toBe(3);
        expect(results[0]).toBe('W1AW');
      });

      const req = httpMock.expectOne('/api/v1/logbook/partial-lookup/W1A');

      expect(req.request.method).toBe('GET');
      req.flush(mockResults);
    });

    it('should handle empty search results', () => {
      service.lookupPartial('ZZZZ').subscribe(results => {
        expect(results.length).toBe(0);
      });

      const req = httpMock.expectOne('/api/v1/logbook/partial-lookup/ZZZZ');

      req.flush([]);
    });
  });

  describe('createQso', () => {
    it('should create a new QSO', () => {
      const newQso: any = {
        call: 'W1AW',
        band: '20m',
        mode: 'SSB',
        date: new Date()
      };

      service.createQso(newQso).subscribe(response => {
        expect(response).toBeTruthy();
      });

      const req = httpMock.expectOne('/api/v1/logbook/qso');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(newQso);
      req.flush({ id: 999, ...newQso });
    });
  });

  describe('updateQso', () => {
    it('should update an existing QSO', () => {
      const updatedQso: any = {
        id: 123,
        call: 'W1AW',
        band: '40m',
        mode: 'CW',
        date: new Date()
      };

      service.updateQso(updatedQso).subscribe(response => {
        expect(response).toBeTruthy();
      });

      const req = httpMock.expectOne('/api/v1/logbook/qso');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updatedQso);
      req.flush(updatedQso);
    });
  });

});

