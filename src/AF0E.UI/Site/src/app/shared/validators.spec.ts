import {beforeEach, describe, expect, it} from 'vitest';
import {callSignValidator} from './validators';
import {FormControl} from '@angular/forms';

describe('Validators', () => {
  describe('callSignValidator', () => {
    let validator: any;

    beforeEach(() => {
      validator = callSignValidator();
    });

    it('should accept valid basic callsigns', () => {
      const validCalls = [
        'W1AW',
        'K3LR',
        'N4ZZ',
        'AA1A',
        'KB1ABC'
      ];

      validCalls.forEach(call => {
        const control = new FormControl(call);
        const result = validator(control);
        expect(result).toBeNull(); // null means valid
      });
    });

    it('should accept valid prefixed callsigns', () => {
      const validCalls = [
        'VE3/W1AW',
        'DL/K3LR',
        'G/N4ZZ'
      ];

      validCalls.forEach(call => {
        const control = new FormControl(call);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should accept valid suffixed callsigns', () => {
      const validCalls = [
        'W1AW/P',
        'K3LR/M',
        'N4ZZ/QRP',
        'AA1A/7'
      ];

      validCalls.forEach(call => {
        const control = new FormControl(call);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should accept valid prefix and suffix callsigns', () => {
      const control = new FormControl('VE3/W1AW/P');
      const result = validator(control);
      expect(result).toBeNull();
    });

    it('should reject invalid callsigns', () => {
      // Only numbers - no letters
      const control1 = new FormControl('123');
      expect(validator(control1)).toEqual({
        callSignNoLetter: { message: 'The main call sign must contain at least one letter' }
      });

      // Only letters - no numbers
      const control2 = new FormControl('ABC');
      expect(validator(control2)).toEqual({
        callSignNoDigit: { message: 'The main call sign must contain at least one digit' }
      });

      // Too short
      const control3 = new FormControl('W');
      expect(validator(control3)).toEqual({
        callSignTooShort: { message: 'The main call sign must be at least 3 characters long' }
      });

      // Too short - only 2 characters
      const control4 = new FormControl('W1');
      expect(validator(control4)).toEqual({
        callSignTooShort: { message: 'The main call sign must be at least 3 characters long' }
      });


      // Invalid character (hyphen)
      const control5 = new FormControl('W1-AW');
      expect(validator(control5)).toEqual({
        callSignInvalid: { message: 'Call sign can only contain letters, digits, and forward slashes' }
      });

      // Invalid character (space)
      const control6 = new FormControl('W1 AW');
      expect(validator(control6)).toEqual({
        callSignInvalid: { message: 'Call sign can only contain letters, digits, and forward slashes' }
      });
    });

    it('should accept international callsigns', () => {
      const validCalls = [
        'G4ABC',       // UK
        'DL1ABC',      // Germany
        'JA1ABC',      // Japan
        'VK2ABC',      // Australia
        '9K2ES',       // Kuwait
        'ZL1ABC'       // New Zealand
      ];

      validCalls.forEach(call => {
        const control = new FormControl(call);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });

    it('should handle null or undefined values', () => {
      // Validator returns null for empty values, letting the 'required' validator handle it
      const control = new FormControl(null);
      const result = validator(control);
      expect(result).toBeNull();

      const control2 = new FormControl(undefined);
      const result2 = validator(control2);
      expect(result2).toBeNull();

      const control3 = new FormControl('');
      const result3 = validator(control3);
      expect(result3).toBeNull();
    });

    it('should handle special stations', () => {
      const validCalls = [
        'W1AW/KH6',    // /KH6 suffix (Hawaii)
        'K1A/MM',      // Maritime mobile
        'N7AA/AM'      // Aeronautical mobile
      ];

      validCalls.forEach(call => {
        const control = new FormControl(call);
        const result = validator(control);
        expect(result).toBeNull();
      });
    });
  });
});

