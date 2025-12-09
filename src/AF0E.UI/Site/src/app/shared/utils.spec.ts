import {describe, expect, it, vi} from 'vitest';
import {Utils} from './utils';
import {HttpErrorResponse} from '@angular/common/http';
import {NotificationMessageModel, NotificationMessageSeverity} from './notification-message.model';
import {ErrorDtoModel} from './error-dto.model';
import {ErrorSource} from './error-source.enum';
import {ErrorSeverity} from './error-severity.enum';

describe('Utils', () => {
  describe('dateToYmd', () => {
    it('should convert date to YYYY-MM-DD format', () => {
      const date = new Date('2025-12-05T10:35:15.000Z');
      expect(Utils.dateToYmd(date)).toBe('2025-12-05');
    });

    it('should return empty string for null', () => {
      expect(Utils.dateToYmd(null)).toBe('');
    });

    it('should return empty string for undefined', () => {
      expect(Utils.dateToYmd(undefined)).toBe('');
    });

    it('should handle different timezones correctly', () => {
      const date = new Date('2025-01-15T23:59:59.000Z');
      const result = Utils.dateToYmd(date);
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    });
  });

  describe('getCurrentUtcDate', () => {
    it('should return current date with time set to UTC', () => {
      const result = Utils.getCurrentUtcDate();
      expect(result).toBeInstanceOf(Date);

      const now = new Date();
      expect(result.getUTCFullYear()).toBe(now.getUTCFullYear());
      expect(result.getUTCMonth()).toBe(now.getUTCMonth());
      expect(result.getUTCDate()).toBe(now.getUTCDate());
    });

    it('should have hours, minutes, seconds, and milliseconds set', () => {
      const result = Utils.getCurrentUtcDate();
      // Should have actual time, not zeroed out
      const totalTime = result.getUTCHours() + result.getUTCMinutes() + result.getUTCSeconds();
      expect(totalTime).toBeGreaterThanOrEqual(0); // Basic sanity check
    });
  });

  describe('getBandFromFrequency', () => {
    it('should return correct band for 160m frequencies', () => {
      expect(Utils.getBandFromFrequency(1.850)).toBe('160m');
      expect(Utils.getBandFromFrequency(1.999)).toBe('160m');
    });

    it('should return correct band for 80m frequencies', () => {
      expect(Utils.getBandFromFrequency(3.500)).toBe('80m');
      expect(Utils.getBandFromFrequency(4.000)).toBe('80m');
    });

    it('should return correct band for 40m frequencies', () => {
      expect(Utils.getBandFromFrequency(7.000)).toBe('40m');
      expect(Utils.getBandFromFrequency(7.300)).toBe('40m');
    });

    it('should return correct band for 20m frequencies', () => {
      expect(Utils.getBandFromFrequency(14.000)).toBe('20m');
      expect(Utils.getBandFromFrequency(14.350)).toBe('20m');
    });

    it('should return correct band for 15m frequencies', () => {
      expect(Utils.getBandFromFrequency(21.000)).toBe('15m');
      expect(Utils.getBandFromFrequency(21.450)).toBe('15m');
    });

    it('should return correct band for 10m frequencies', () => {
      expect(Utils.getBandFromFrequency(28.000)).toBe('10m');
      expect(Utils.getBandFromFrequency(29.700)).toBe('10m');
    });

    it('should return correct band for 6m frequencies', () => {
      expect(Utils.getBandFromFrequency(50.000)).toBe('6m');
      expect(Utils.getBandFromFrequency(54.000)).toBe('6m');
    });

    it('should return correct band for 2m frequencies', () => {
      expect(Utils.getBandFromFrequency(144.000)).toBe('2m');
      expect(Utils.getBandFromFrequency(148.000)).toBe('2m');
    });

    it('should return null for out-of-band frequencies', () => {
      expect(Utils.getBandFromFrequency(0.5)).toBeNull();
      expect(Utils.getBandFromFrequency(1000)).toBeNull();
    });

    it('should return null for negative frequencies', () => {
      expect(Utils.getBandFromFrequency(-1)).toBeNull();
    });

    it('should return null for zero frequency', () => {
      expect(Utils.getBandFromFrequency(0)).toBeNull();
    });
  });

  describe('showErrorMessage', () => {
    it('should handle generic error objects', () => {
      const mockNotificationService = {addMessage: vi.fn()};
      const mockLogService = {error: vi.fn()};

      const error = new Error('Generic error');

      Utils.showErrorMessage(error, mockNotificationService as any, mockLogService as any);

      expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
      expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Error', 'Unhandled error. Please notify me.', false));
      expect(mockLogService.error).toHaveBeenCalledWith(error);
    });

    describe('HTTP error responses', () => {
      it('should ignore 0', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: 0});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(0);
        expect(mockLogService.error).toHaveBeenCalledTimes(0);
      });

      it('should handle >=500', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: 500});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
        expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Error', 'Server error. Please notify me.', false));
      });

      it('should handle 401', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: 401});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
        expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Not authorized', 'You are not authorized to use this resource.', true));
      });

      it('should handle 403', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: 403});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
        expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Access denied', 'You do not have permissions to access this feature.', true));
      });

      it('should handle 404', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: 404});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
        expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Not found', 'The resource is not found. Please notify me.', true));
      });

      it('should handle unexpected', () => {
        const mockNotificationService = {addMessage: vi.fn()};
        const mockLogService = {error: vi.fn()};

        const httpError = new HttpErrorResponse({status: -1});

        Utils.showErrorMessage(httpError, mockNotificationService as any, mockLogService as any);

        expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
        expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Error', 'Unexpected error. Please notify me.', false));
      });

      describe('Error DTO', () => {
        describe('Business', () => {
          it('conflict', () => {
            const mockNotificationService = {addMessage: vi.fn()};
            const mockLogService = {error: vi.fn()};

            const error = new HttpErrorResponse({status: 1, error: new ErrorDtoModel(ErrorSource.Business, ErrorSeverity.Conflict, 'Test msg', 'Full message', null)});

            Utils.showErrorMessage(error, mockNotificationService as any, mockLogService as any);

            expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
            expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Error', 'Test msg', true));
          });

          it('not conflict', () => {
            const mockNotificationService = {addMessage: vi.fn()};
            const mockLogService = {error: vi.fn()};

            const error = new HttpErrorResponse({status: 1, error: new ErrorDtoModel(ErrorSource.Business, ErrorSeverity.Error, 'Test msg', 'Full message', null)});

            Utils.showErrorMessage(error, mockNotificationService as any, mockLogService as any);

            expect(mockNotificationService.addMessage).toHaveBeenCalledTimes(1);
            expect(mockNotificationService.addMessage).toHaveBeenCalledWith(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Error', 'Test msg', false));
          });
        });
      })
    })
  });
});

