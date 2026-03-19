import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {QrzService} from './qrz.service';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {provideHttpClient} from '@angular/common/http';
import {QrzDetailsModel} from '../models/qrz-details.model';

describe('QrzService', () => {
  let service: QrzService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        QrzService,
        provideHttpClient(),
        provideHttpClientTesting()
     ]
    });

    service = TestBed.inject(QrzService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  describe('lookup', () => {
    it('should fetch a single result by call sign', () => {
      const mockDetails: QrzDetailsModel = {
        call: 'TEST',
        dxcc: 1,
        fname: 'First',
        name: 'Last',
        nickname: 'Tester',
        name_fmt: 'First "Tester" Last',
        addr1: '123 Main St',
        addr2: '',
        state: 'CA',
        zip: '90000',
        country: 'USA',
        ccode: 1,
        grid: 'AA00aa',
        county: 'County',
        cqzone: 1,
        ituzone: 1
      } as QrzDetailsModel;

      service.lookup('TEST').subscribe(details => {
        expect(details.call).toBe('TEST');
        expect(details.grid).toBe('AA00aa');
      });

      const req = httpMock.expectOne('/api/v1/qrz/TEST');
      expect(req.request.method).toBe('GET');
      req.flush(mockDetails);
    });
  });
});
