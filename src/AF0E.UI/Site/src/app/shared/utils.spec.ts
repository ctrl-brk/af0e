import {describe, expect, it, vi} from 'vitest';
import {Utils} from './utils';
import {HttpErrorResponse} from '@angular/common/http';
import {NotificationMessageModel, NotificationMessageSeverity} from './notification-message.model';

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
      const now = new Date();
      const result = Utils.getCurrentUtcDate();
      expect(result).toBeInstanceOf(Date);

      expect(result.getFullYear()).toBe(now.getUTCFullYear());
      expect(result.getMonth()).toBe(now.getUTCMonth());
      expect(result.getDate()).toBe(now.getUTCDate());
      expect(result.getHours()).toBe(now.getUTCHours());
      expect(result.getMinutes()).toBe(now.getUTCMinutes());
    });
  });

  describe('getBandFromFrequency', () => {
    it('should return correct band for 160m frequencies', () => {
      expect(Utils.getBandFromFrequency(1850000)).toBe('160m');
      expect(Utils.getBandFromFrequency(1999000)).toBe('160m');
    });

    it('should return correct band for 80m frequencies', () => {
      expect(Utils.getBandFromFrequency(3500000)).toBe('80m');
      expect(Utils.getBandFromFrequency(4000000)).toBe('80m');
    });

    it('should return correct band for 40m frequencies', () => {
      expect(Utils.getBandFromFrequency(7000000)).toBe('40m');
      expect(Utils.getBandFromFrequency(7300000)).toBe('40m');
    });

    it('should return correct band for 20m frequencies', () => {
      expect(Utils.getBandFromFrequency(14000000)).toBe('20m');
      expect(Utils.getBandFromFrequency(14350000)).toBe('20m');
    });

    it('should return correct band for 15m frequencies', () => {
      expect(Utils.getBandFromFrequency(21000000)).toBe('15m');
      expect(Utils.getBandFromFrequency(21450000)).toBe('15m');
    });

    it('should return correct band for 10m frequencies', () => {
      expect(Utils.getBandFromFrequency(28000000)).toBe('10m');
      expect(Utils.getBandFromFrequency(29700000)).toBe('10m');
    });

    it('should return correct band for 6m frequencies', () => {
      expect(Utils.getBandFromFrequency(50000000)).toBe('6m');
      expect(Utils.getBandFromFrequency(54000000)).toBe('6m');
    });

    it('should return correct band for 2m frequencies', () => {
      expect(Utils.getBandFromFrequency(144000000)).toBe('2m');
      expect(Utils.getBandFromFrequency(148000000)).toBe('2m');
    });

    it('should return null for out-of-band frequencies', () => {
      expect(Utils.getBandFromFrequency(500)).toBeNull();
      expect(Utils.getBandFromFrequency(1000000)).toBeNull();
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
    })
  });

  describe('getTimeOfDay', () => {
    it('should return morning for early morning hours (5-11 AM)', () => {
      // Mock a date at 8 AM in a specific timezone
      const mockDate = new Date('2026-03-06T15:00:00Z'); // 8 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      const result = Utils.getTimeOfDay('CO');
      expect(result).toBe('GM ');

      vi.useRealTimers();
    });

    it('should return afternoon for midday hours (12-4 PM)', () => {
      // Mock a date at 2 PM in a specific timezone
      const mockDate = new Date('2026-03-06T21:00:00Z'); // 2 PM MST (UTC-7)
      vi.setSystemTime(mockDate);

      const result = Utils.getTimeOfDay('CO');
      expect(result).toBe('GA ');

      vi.useRealTimers();
    });

    it('should return evening for evening hours (5 PM - 4 AM)', () => {
      // Mock a date at 8 PM in a specific timezone
      const mockDate = new Date('2026-03-07T03:00:00Z'); // 8 PM MST (UTC-7)
      vi.setSystemTime(mockDate);

      const result = Utils.getTimeOfDay('CO');
      expect(result).toBe('GE ');

      vi.useRealTimers();
    });

    it('should handle Eastern timezone states', () => {
      const mockDate = new Date('2026-03-06T14:00:00Z'); // 9 AM EST (UTC-5)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('NY')).toBe('GM ');
      expect(Utils.getTimeOfDay('FL')).toBe('GM ');
      expect(Utils.getTimeOfDay('MA')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Central timezone states', () => {
      const mockDate = new Date('2026-03-06T15:00:00Z'); // 9 AM CST (UTC-6)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('IL')).toBe('GM ');
      expect(Utils.getTimeOfDay('TX')).toBe('GM ');
      expect(Utils.getTimeOfDay('MN')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Mountain timezone states', () => {
      const mockDate = new Date('2026-03-06T16:00:00Z'); // 9 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CO')).toBe('GM ');
      expect(Utils.getTimeOfDay('UT')).toBe('GM ');
      expect(Utils.getTimeOfDay('MT')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Pacific timezone states', () => {
      const mockDate = new Date('2026-03-06T17:00:00Z'); // 9 AM PST (UTC-8)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CA')).toBe('GM ');
      expect(Utils.getTimeOfDay('WA')).toBe('GM ');
      expect(Utils.getTimeOfDay('OR')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Alaska timezone', () => {
      const mockDate = new Date('2026-03-06T18:00:00Z'); // 9 AM AKST (UTC-9)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('AK')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Hawaii timezone', () => {
      const mockDate = new Date('2026-03-06T19:00:00Z'); // 9 AM HST (UTC-10)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('HI')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle Arizona (no DST)', () => {
      const mockDate = new Date('2026-03-06T16:00:00Z'); // 9 AM MST (UTC-7, no DST)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('AZ')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle lowercase state abbreviations', () => {
      const mockDate = new Date('2026-03-06T15:00:00Z'); // 8 AM MST
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('co')).toBe('GM ');
      expect(Utils.getTimeOfDay('ny')).toBe('GM ');
      expect(Utils.getTimeOfDay('ca')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should return FB for empty state', () => {
      expect(Utils.getTimeOfDay('')).toBe('FB ');
    });

    it('should return FB for invalid state', () => {
      expect(Utils.getTimeOfDay('XX')).toBe('FB ');
      expect(Utils.getTimeOfDay('ZZ')).toBe('FB ');
    });

    it('should handle boundary condition at noon (12 PM)', () => {
      const mockDate = new Date('2026-03-06T19:00:00Z'); // 12 PM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CO')).toBe('GA ');

      vi.useRealTimers();
    });

    it('should handle boundary condition at 5 PM', () => {
      const mockDate = new Date('2026-03-07T00:00:00Z'); // 5 PM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CO')).toBe('GE ');

      vi.useRealTimers();
    });

    it('should handle boundary condition at 5 AM', () => {
      const mockDate = new Date('2026-03-06T12:00:00Z'); // 5 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CO')).toBe('GM ');

      vi.useRealTimers();
    });

    it('should handle midnight to early morning (12 AM - 4:59 AM)', () => {
      const mockDate = new Date('2026-03-06T09:00:00Z'); // 2 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('CO')).toBe('GE ');

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Eastern Time', () => {
      const mockDate = new Date('2026-03-06T14:00:00Z'); // 9 AM EST (UTC-5)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('ON')).toBe('GM '); // Ontario
      expect(Utils.getTimeOfDay('QC')).toBe('GM '); // Quebec

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Atlantic Time', () => {
      const mockDate = new Date('2026-03-06T13:00:00Z'); // 9 AM AST (UTC-4)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('NB')).toBe('GM '); // New Brunswick
      expect(Utils.getTimeOfDay('NS')).toBe('GM '); // Nova Scotia
      expect(Utils.getTimeOfDay('PE')).toBe('GM '); // Prince Edward Island

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Newfoundland Time', () => {
      const mockDate = new Date('2026-03-06T12:30:00Z'); // 9 AM NST (UTC-3:30)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('NL')).toBe('GM '); // Newfoundland and Labrador

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Central Time', () => {
      const mockDate = new Date('2026-03-06T15:00:00Z'); // 9 AM CST (UTC-6)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('MB')).toBe('GM '); // Manitoba
      expect(Utils.getTimeOfDay('SK')).toBe('GM '); // Saskatchewan

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Mountain Time', () => {
      const mockDate = new Date('2026-03-06T16:00:00Z'); // 9 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('AB')).toBe('GM '); // Alberta

      vi.useRealTimers();
    });

    it('should handle Canadian provinces - Pacific Time', () => {
      const mockDate = new Date('2026-03-06T17:00:00Z'); // 9 AM PST (UTC-8)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('BC')).toBe('GM '); // British Columbia
      expect(Utils.getTimeOfDay('YT')).toBe('GM '); // Yukon

      vi.useRealTimers();
    });

    it('should handle Canadian territories', () => {
      const mockDate = new Date('2026-03-06T16:00:00Z'); // 9 AM MST (UTC-7)
      vi.setSystemTime(mockDate);

      expect(Utils.getTimeOfDay('NT')).toBe('GM '); // Northwest Territories
      expect(Utils.getTimeOfDay('NU')).toBe('GM '); // Nunavut (using Eastern)

      vi.useRealTimers();
    });
  });

  describe('extractNameOrNickname', () => {
    it('should extract first name when no nickname present', () => {
      expect(Utils.extractNameOrNickname('John Doe')).toBe('John');
      expect(Utils.extractNameOrNickname('Mary Smith')).toBe('Mary');
      expect(Utils.extractNameOrNickname('Bob Johnson III')).toBe('Bob');
    });

    it('should extract nickname when present in double quotes', () => {
      expect(Utils.extractNameOrNickname('John "JD" Doe')).toBe('JD');
      expect(Utils.extractNameOrNickname('Mary "The Queen" Smith')).toBe('The Queen');
      expect(Utils.extractNameOrNickname('Robert "Bob" Wilson')).toBe('Bob');
    });

    it('should handle single word names', () => {
      expect(Utils.extractNameOrNickname('John')).toBe('John');
      expect(Utils.extractNameOrNickname('Mary')).toBe('Mary');
    });

    it('should handle names with multiple spaces', () => {
      expect(Utils.extractNameOrNickname('John  Doe')).toBe('John');
      expect(Utils.extractNameOrNickname('Mary   Smith')).toBe('Mary');
    });

    it('should handle leading and trailing whitespace', () => {
      expect(Utils.extractNameOrNickname('  John Doe  ')).toBe('John');
      expect(Utils.extractNameOrNickname('  John "JD" Doe  ')).toBe('JD');
    });

    it('should return empty string for null or undefined', () => {
      expect(Utils.extractNameOrNickname(null)).toBe('');
      expect(Utils.extractNameOrNickname(undefined)).toBe('');
    });

    it('should return empty string for empty string', () => {
      expect(Utils.extractNameOrNickname('')).toBe('');
      expect(Utils.extractNameOrNickname('   ')).toBe('');
    });

    it('should handle nicknames with spaces inside quotes', () => {
      expect(Utils.extractNameOrNickname('William "Big Bill" Johnson')).toBe('Big Bill');
      expect(Utils.extractNameOrNickname('Mary "M J" Smith')).toBe('M J');
    });

    it('should extract only the first nickname if multiple quotes present', () => {
      expect(Utils.extractNameOrNickname('John "JD" "Junior" Doe')).toBe('JD');
    });

    it('should handle empty quotes', () => {
      expect(Utils.extractNameOrNickname('John "" Doe')).toBe('John');
      expect(Utils.extractNameOrNickname('John "  " Doe')).toBe('John');
    });

    it('should handle nickname at the beginning', () => {
      expect(Utils.extractNameOrNickname('"Ace" John Doe')).toBe('Ace');
    });

    it('should handle nickname at the end', () => {
      expect(Utils.extractNameOrNickname('John Doe "JD"')).toBe('JD');
    });

    it('should handle names with special characters', () => {
      expect(Utils.extractNameOrNickname("John O'Brien")).toBe('John');
      expect(Utils.extractNameOrNickname('Jean-Paul Sartre')).toBe('Jean-Paul');
    });

    it('should handle unclosed quotes', () => {
      expect(Utils.extractNameOrNickname('John "JD Doe')).toBe('John');
      expect(Utils.extractNameOrNickname('John JD" Doe')).toBe('John');
    });

    it('should use second word if first word is only one letter', () => {
      expect(Utils.extractNameOrNickname('J Smith')).toBe('Smith');
      expect(Utils.extractNameOrNickname('M Johnson')).toBe('Johnson');
      expect(Utils.extractNameOrNickname('A John Doe')).toBe('John');
    });

    it('should return single letter if it is the only word', () => {
      expect(Utils.extractNameOrNickname('J')).toBe('J');
      expect(Utils.extractNameOrNickname('M')).toBe('M');
    });

    it('should prefer nickname over second word when first word is single letter', () => {
      expect(Utils.extractNameOrNickname('J "Jake" Smith')).toBe('Jake');
      expect(Utils.extractNameOrNickname('M "Mike" Johnson')).toBe('Mike');
    });
  });

  describe('calculateMorseTime', () => {
    it('should return 0 for empty string', () => {
      expect(Utils.calculateMorseTime('', 20)).toBe(0);
      expect(Utils.calculateMorseTime('   ', 20)).toBe(0);
    });

    it('should return 0 for invalid WPM', () => {
      expect(Utils.calculateMorseTime('TEST', 0)).toBe(0);
      expect(Utils.calculateMorseTime('TEST', -5)).toBe(0);
    });

    it('should calculate time for single letter', () => {
      // 'E' = '.' = 1 unit at 20 WPM (60ms per unit) = 60ms
      const timeE = Utils.calculateMorseTime('E', 20);
      expect(timeE).toBe(60);

      // 'T' = '-' = 3 units at 20 WPM = 180ms
      const timeT = Utils.calculateMorseTime('T', 20);
      expect(timeT).toBe(180);
    });

    it('should calculate time for standard word PARIS', () => {
      // "PARIS" is the standard 50-unit word in Morse code
      // At 20 WPM: 50 units * 60ms = 3000ms = 3 seconds
      const time = Utils.calculateMorseTime('PARIS', 20);
      // Allow small rounding tolerance
      expect(time).toBeGreaterThan(2900);
      expect(time).toBeLessThan(3100);
    });

    it('should calculate different times for different WPM', () => {
      const text = 'CQ';
      const time20 = Utils.calculateMorseTime(text, 20);
      const time40 = Utils.calculateMorseTime(text, 40);

      // At 40 WPM should be approximately half the time of 20 WPM
      expect(time40).toBeLessThan(time20);
      expect(time40).toBeGreaterThan(time20 / 2 - 50);
      expect(time40).toBeLessThan(time20 / 2 + 50);
    });

    it('should handle numbers', () => {
      // '5' = '.....' = 5 dots + 4 spaces = 9 units
      const time = Utils.calculateMorseTime('5', 20);
      expect(time).toBe(540); // 9 * 60ms
    });

    it('should handle special characters', () => {
      // '?' = '..--..'
      const time = Utils.calculateMorseTime('?', 20);
      expect(time).toBeGreaterThan(0);
    });

    it('should handle spaces between words', () => {
      // Space adds 7 units total between words
      const singleWord = Utils.calculateMorseTime('CQ', 20);
      const twoWords = Utils.calculateMorseTime('CQ CQ', 20);

      // Two words should be more than double (due to 7-unit space)
      expect(twoWords).toBeGreaterThan(singleWord * 2);
    });

    it('should handle typical CW exchanges', () => {
      const exchanges = [
        'CQ CQ CQ DE AF0E',
        'TU 599 CO',
        'R TU 599 599 CO CO',
        '73 GL'
      ];

      exchanges.forEach(exchange => {
        const time = Utils.calculateMorseTime(exchange, 25);
        expect(time).toBeGreaterThan(0);
        // Reasonable time check (should be under 30 seconds for typical exchanges)
        expect(time).toBeLessThan(30000);
      });
    });

    it('should be case insensitive', () => {
      const timeLower = Utils.calculateMorseTime('test', 20);
      const timeUpper = Utils.calculateMorseTime('TEST', 20);
      const timeMixed = Utils.calculateMorseTime('TeSt', 20);

      expect(timeLower).toBe(timeUpper);
      expect(timeLower).toBe(timeMixed);
    });

    it('should ignore unsupported characters', () => {
      // Should calculate for 'A' and 'B', ignore '#' and '$'
      const timeAB = Utils.calculateMorseTime('AB', 20);
      const timeWithSpecial = Utils.calculateMorseTime('A#$B', 20);

      expect(timeAB).toBe(timeWithSpecial);
    });

    it('should handle realistic callsigns', () => {
      const callsigns = ['AF0E', 'W1AW', 'VE3XYZ', 'K2ABC'];

      callsigns.forEach(call => {
        const time = Utils.calculateMorseTime(call, 20);
        expect(time).toBeGreaterThan(0);
        expect(time).toBeLessThan(10000); // Should be under 10 seconds
      });
    });

    it('should calculate faster times for higher WPM', () => {
      const text = 'AF0E AF0E K';
      const times = [10, 15, 20, 25, 30].map(wpm =>
        Utils.calculateMorseTime(text, wpm)
      );

      // Each subsequent speed should be faster (less time)
      for (let i = 1; i < times.length; i++) {
        expect(times[i]).toBeLessThan(times[i - 1]);
      }
    });

    it('should handle contest exchanges', () => {
      const exchange = 'R TU 5NN CO';
      const time20 = Utils.calculateMorseTime(exchange, 20);
      const time35 = Utils.calculateMorseTime(exchange, 35);

      expect(time20).toBeGreaterThan(0);
      expect(time35).toBeGreaterThan(0);
      expect(time35).toBeLessThan(time20);
    });
  });
});

