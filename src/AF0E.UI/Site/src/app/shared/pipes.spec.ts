import {beforeEach, describe, expect, it} from 'vitest';
import {GridPipe, ModeSeverityPipe, QsoModePipe} from './pipes';

describe('Pipes', () => {
  describe('QsoModePipe', () => {
    let pipe: QsoModePipe;

    beforeEach(() => {
      pipe = new QsoModePipe();
    });

    it('should transform sideband to SSB', () => {
      expect(pipe.transform('SSB')).toBe('SSB');
      expect(pipe.transform('USB')).toBe('SSB');
      expect(pipe.transform('LSB')).toBe('SSB');
    });

    it('should transform MFSK modes to FT4', () => {
      expect(pipe.transform('MFSK')).toBe('FT4');
    });

    it('should handle null or undefined', () => {
      expect(pipe.transform(null as any)).toBeNull();
      expect(pipe.transform(undefined as any)).toBeUndefined();
    });

    it('should handle unknown modes', () => {
      expect(pipe.transform('UNKNOWN')).toBe('UNKNOWN');
    });

    it('should be case-insensitive', () => {
      expect(pipe.transform('ft8')).toBe('FT8');
      expect(pipe.transform('Ft8')).toBe('FT8');
      expect(pipe.transform('FT8')).toBe('FT8');
    });
  });

  describe('ModeSeverityPipe', () => {
    let pipe: ModeSeverityPipe;

    beforeEach(() => {
      pipe = new ModeSeverityPipe();
    });

    it('should return success severity for CW', () => {
      expect(pipe.transform('CW')).toBe('success');
    });

    it('should return info severity for phone modes', () => {
      expect(pipe.transform('SSB')).toBe('info');
      expect(pipe.transform('USB')).toBe('info');
      expect(pipe.transform('LSB')).toBe('info');
      expect(pipe.transform('FM')).toBe('info');
      expect(pipe.transform('AM')).toBe('info');
    });

    it('should return warn severity for digital modes', () => {
      expect(pipe.transform('FT8')).toBe('warn');
      expect(pipe.transform('FT4')).toBe('warn');
      expect(pipe.transform('MFSK')).toBe('warn');
      expect(pipe.transform('PSK31')).toBe('warn');
      expect(pipe.transform('JT65')).toBe('warn');
      expect(pipe.transform('RTTY')).toBe('warn');
    });

    it('should return default severity for unknown modes', () => {
      expect(pipe.transform('UNKNOWN')).toBe('secondary');
    });

    it('should handle null or undefined', () => {
      expect(pipe.transform(null as any)).toBe('secondary');
      expect(pipe.transform(undefined as any)).toBe('secondary');
    });
  });

  describe('GridPipe', () => {
    let pipe: GridPipe;

    beforeEach(() => {
      pipe = new GridPipe();
    });

    it('should format valid 4-character grid', () => {
      expect(pipe.transform('dm79')).toBe('DM79');
    });

    it('should format valid 6-character grid', () => {
      expect(pipe.transform('dm79LV')).toBe('DM79lv');
    });

    it('should not truncate extra characters', () => {
      expect(pipe.transform('DM79lvxx')).toBe('DM79lvxx');
    });

    it('should handle null or undefined', () => {
      expect(pipe.transform(null as any)).toBe('');
      expect(pipe.transform(undefined as any)).toBe('');
    });

    it('should handle empty string', () => {
      expect(pipe.transform('')).toBe('');
    });
  });
});

