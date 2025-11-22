import {AbstractControl, ValidationErrors, ValidatorFn} from '@angular/forms';

export function callSignValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;

    if (!value) {
      return null; // Let required validator handle empty values
    }

    // Must contain only letters, digits, and forward slashes
    if (!/^[A-Za-z0-9\/]+$/.test(value)) {
      return { callSignInvalid: { message: 'Call sign can only contain letters, digits, and forward slashes' } };
    }

    // Split by forward slashes to handle prefix/suffix
    const parts = value.split('/');

    // Can't have more than 2 slashes (3 parts max: prefix/call/suffix)
    if (parts.length > 3) {
      return { callSignTooManySlashes: { message: 'Call sign can have at most 2 forward slashes' } };
    }

    // Can't have empty parts (e.g., "W1AW/", "/W1AW", "W1//AW")
    if (parts.some((part: string) => !part)) {
      return { callSignEmptyPart: { message: 'Call sign parts cannot be empty' } };
    }

    // Determine which part is the main call sign
    let mainCallSign: string;

    if (parts.length === 1) {
      // No slashes, entire value is the call sign
      mainCallSign = parts[0];
    } else if (parts.length === 2) {
      // One slash: call sign is the longest part
      mainCallSign = parts[0].length >= parts[1].length ? parts[0] : parts[1];
    } else {
      // Two slashes: call sign is the middle part
      mainCallSign = parts[1];
    }

    // Validate the main call sign
    // Must be at least 3 characters long
    if (mainCallSign.length < 3) {
      return { callSignTooShort: { message: 'The main call sign must be at least 3 characters long' } };
    }

    // Must contain at least one digit
    if (!/\d/.test(mainCallSign)) {
      return { callSignNoDigit: { message: 'The main call sign must contain at least one digit' } };
    }

    return null;
  };
}
