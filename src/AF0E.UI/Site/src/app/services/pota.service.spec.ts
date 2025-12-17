import {TestBed} from '@angular/core/testing';
import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {PotaService} from './pota.service';
import {PotaActivationModel} from '../models/pota-activation.model';
import {PotaParkModel} from '../models/pota-park.model';

describe('PotaService', () => {
  let service: PotaService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PotaService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(PotaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } finally {
      TestBed.resetTestingModule();
    }
  });

  describe('getActivations', () => {
    it('should fetch all activations', () => {
      const mockActivations: PotaActivationModel[] = [
        {
          id: 1,
          parkNum: 'US-0001',
          parkName: 'Test Park',
          startDate: new Date(),
          endDate: new Date(),
          count: 50,
          cwCount: 10,
          digiCount: 20,
          phoneCount: 20,
          p2pCount: 5
        } as PotaActivationModel
      ];

      service.getActivations().subscribe(activations => {
        expect(activations.length).toBe(1);
        expect(activations[0].parkNum).toBe('US-0001');
      });

      const req = httpMock.expectOne('/api/v1/pota/activations');
      expect(req.request.method).toBe('GET');
      req.flush(mockActivations);
    });
  });

  describe('getActivation', () => {
    it('should fetch a single activation by ID', () => {
      const mockActivation: PotaActivationModel = {
        id: 123,
        parkNum: 'US-0001',
        parkName: 'Test Park',
        startDate: new Date(),
        endDate: new Date(),
        count: 50
      } as PotaActivationModel;

      service.getActivation(123).subscribe(activation => {
        expect(activation.id).toBe(123);
        expect(activation.parkNum).toBe('US-0001');
      });

      const req = httpMock.expectOne('/api/v1/pota/activations/123');
      expect(req.request.method).toBe('GET');
      req.flush(mockActivation);
    });
  });

  describe('getPark', () => {
    it('should fetch park information', () => {
      const mockPark: PotaParkModel = {
        parkNum: 'US-0001',
        parkDesc: 'Test Park Description',
        location: 'Colorado',
        grid: 'DM79',
        active: true,
        totalActivationCount: 10,
        totalQsoCount: 500
      } as PotaParkModel;

      service.getPark('US-0001').subscribe(park => {
        expect(park.parkNum).toBe('US-0001');
        expect(park.location).toBe('Colorado');
      });

      const req = httpMock.expectOne('/api/v1/pota/park/US-0001');
      expect(req.request.method).toBe('GET');
      req.flush(mockPark);
    });
  });

  describe('searchPark', () => {
    it('should search parks by name', () => {
      const mockParks: PotaParkModel[] = [
        { parkNum: 'US-0001', parkDesc: 'Rocky Mountain Park' } as PotaParkModel,
        { parkNum: 'US-0002', parkDesc: 'Rocky River Park' } as PotaParkModel
      ];

      service.searchPark('Rocky').subscribe(parks => {
        expect(parks.length).toBe(2);
        expect(parks[0].parkNum).toBe('US-0001');
      });

      const req = httpMock.expectOne('/api/v1/pota/parks/search/Rocky');

      expect(req.request.method).toBe('GET');
      req.flush(mockParks);
    });

    it('should return empty array for no matches', () => {
      service.searchPark('XYZ123').subscribe(parks => {
        expect(parks.length).toBe(0);
      });

      const req = httpMock.expectOne('/api/v1/pota/parks/search/XYZ123');

      req.flush([]);
    });
  });

  describe('getUnconfirmedLog', () => {
    it('should fetch unconfirmed QSOs', () => {
      const mockQsos = [
        { id: 1, call: 'W1AW', metadata: 'US-0001' },
        { id: 2, call: 'K3LR', metadata: 'US-0002' }
      ];

      service.getUnconfirmedLog().subscribe(qsos => {
        expect(qsos.length).toBe(2);
      });

      const req = httpMock.expectOne('/api/v1/pota/log/unconfirmed');
      expect(req.request.method).toBe('GET');
      req.flush(mockQsos);
    });
  });

  describe('GeoJSON endpoints', () => {
    it('should fetch activated parks GeoJSON', () => {
      const mockGeoJson = {
        type: 'FeatureCollection',
        features: []
      };

      service.getActivatedParksGeoJson().subscribe(data => {
        expect(data.type).toBe('FeatureCollection');
      });

      const req = httpMock.expectOne('/api/v1/pota/geojson/parks/activated');
      expect(req.request.method).toBe('GET');
      req.flush(mockGeoJson);
    });

    it('should fetch activations GeoJSON', () => {
      const mockGeoJson = {
        type: 'FeatureCollection',
        features: []
      };

      service.getActivationsGeoJson().subscribe(data => {
        expect(data.type).toBe('FeatureCollection');
      });

      const req = httpMock.expectOne('/api/v1/pota/geojson/activations/all');
      expect(req.request.method).toBe('GET');
      req.flush(mockGeoJson);
    });

    it('should fetch parks by boundary with correct parameters', () => {
      const mockGeoJson = {
        type: 'FeatureCollection',
        features: []
      };

      service.getGeoJsonByBoundary(-109, 37, -102, 41).subscribe(data => {
        expect(data.type).toBe('FeatureCollection');
      });

      const req = httpMock.expectOne(request =>
        request.url.includes('/api/v1/pota/geojson/parks/not-activated/boundary')
      );

      expect(req.request.method).toBe('GET');
      req.flush(mockGeoJson);
    });
  });
});

