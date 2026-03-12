import {HttpErrorResponse} from '@angular/common/http';
import {NotificationService} from './notification.service';
import {LogService} from './log.service';
import {NotificationMessageModel, NotificationMessageSeverity} from './notification-message.model';

export class Utils {
  public static showErrorMessage(e: any, ntf: NotificationService, log?: LogService) {
    let msg = 'Server error. Please notify me.';
    const severity = NotificationMessageSeverity.Error;
    let title = 'Error';
    let sticky = false;

    if (!(e instanceof HttpErrorResponse)) {
      msg = 'Unhandled error. Please notify me.';
      if (log) log.error(e);
    } else if (e.status === 0) {
      title = 'Connection error';
      msg = 'Please make sure the back-end service is accessible.';
    }
    else if (e.status >= 500) {
      ntf.addMessage(new NotificationMessageModel(severity, title, msg, sticky));
      return;
    } else if (e.status === 401) {
      title = "Not authorized";
      msg = "You are not authorized to use this resource.";
      sticky = true;
    } else if (e.status === 403) {
      title = "Access denied";
      msg = "You do not have permissions to access this feature.";
      sticky = true;
    } else if (e.status === 404) {
      title = "Not found";
      msg = "The resource is not found. Please notify me.";
      sticky = true;
    } else {
      title = "Error";
      msg = "Unexpected error. Please notify me.";
    }

    ntf.addMessage(new NotificationMessageModel(severity, title, msg, sticky));

  }

  public static dateToYmd(date?: Date | string | null): string {
    if (!date) return '';

    if (typeof(date) === 'string')
      date = new Date(date);

    return date.getUTCFullYear() + "-" + ("0" + (date.getUTCMonth() + 1)).slice(-2) + "-" + ("0" + date.getUTCDate()).slice(-2);
  }

  public static dateToSql(date: Date): string {
    return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
  }

  public static getBandFromFrequency(freqHz: number | null): string | null {
    if (!freqHz) return null;

    if (freqHz >= 1800000 && freqHz <= 2000000) return '160m';
    if (freqHz >= 3500000 && freqHz <= 4000000) return '80m';
    if (freqHz >= 5330500 && freqHz <= 5403500) return '60m';
    if (freqHz >= 7000000 && freqHz <= 7300000) return '40m';
    if (freqHz >= 10100000 && freqHz <= 10150000) return '30m';
    if (freqHz >= 14000000 && freqHz <= 14350000) return '20m';
    if (freqHz >= 18068000 && freqHz <= 18168000) return '17m';
    if (freqHz >= 21000000 && freqHz <= 21450000) return '15m';
    if (freqHz >= 24890000 && freqHz <= 24990000) return '12m';
    if (freqHz >= 28000000 && freqHz <= 29700000) return '10m';
    if (freqHz >= 50000000 && freqHz <= 54000000) return '6m';
    if (freqHz >= 144000000 && freqHz <= 148000000) return '2m';
    if (freqHz >= 222000000 && freqHz <= 225000000) return '1.25m';
    if (freqHz >= 420000000 && freqHz <= 450000000) return '70cm';

    return null;
  }

  public static getCurrentUtcDate(): Date {
    const now = new Date();
    // Create a "local" date that displays the current UTC time
    // This allows the DatePicker to show the UTC time without timezone shifts
    return new Date(
      now.getUTCFullYear(),
      now.getUTCMonth(),
      now.getUTCDate(),
      now.getUTCHours(),
      now.getUTCMinutes(),
      now.getUTCSeconds()
    );
  }

  /**
   * Converts a Date object to ISO 8601 UTC string format for API submission
   * This treats the "local" date components as UTC to avoid double conversion
   */
  public static dateToUtcString(date: Date | null | undefined): string | null {
    if (!date) return null;

    // Read the date components as if they are already UTC values
    // and construct an ISO string manually
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}Z`;
  }

  /**
   * Converts a UTC date string from the API to a Date object for form display
   * This creates a "local" Date that displays the UTC values without timezone shift
   */
  public static utcStringToDate(dateString: string | Date | null | undefined): Date | null {
    if (!dateString) return null;

    // If already a Date object, return as-is
    if (dateString instanceof Date) return dateString;

    // Parse the UTC date string and extract components
    const date = new Date(dateString);

    // Create a "local" date with the UTC components
    // This prevents the DatePicker from shifting the display
    return new Date(
      date.getUTCFullYear(),
      date.getUTCMonth(),
      date.getUTCDate(),
      date.getUTCHours(),
      date.getUTCMinutes(),
      date.getUTCSeconds()
    );
  }

  /**
   * Maps US state and Canadian province abbreviations to their IANA timezone identifiers
   * Covers all 50 US states, DC, US territories, 10 Canadian provinces, and 3 Canadian territories
   */
  private static readonly STATE_TIMEZONES: { [key: string]: string } = {
    // === UNITED STATES ===

    // Eastern Time (19 states + DC)
    'CT': 'America/New_York', 'DE': 'America/New_York', 'FL': 'America/New_York',
    'GA': 'America/New_York', 'ME': 'America/New_York', 'MD': 'America/New_York',
    'MA': 'America/New_York', 'MI': 'America/New_York', 'NH': 'America/New_York',
    'NJ': 'America/New_York', 'NY': 'America/New_York', 'NC': 'America/New_York',
    'OH': 'America/New_York', 'PA': 'America/New_York', 'RI': 'America/New_York',
    'SC': 'America/New_York', 'VT': 'America/New_York', 'VA': 'America/New_York',
    'WV': 'America/New_York', 'DC': 'America/New_York',

    // Central Time (17 states)
    'AL': 'America/Chicago', 'AR': 'America/Chicago', 'IL': 'America/Chicago',
    'IN': 'America/Chicago', 'IA': 'America/Chicago', 'KS': 'America/Chicago',
    'KY': 'America/Chicago', 'LA': 'America/Chicago', 'MN': 'America/Chicago',
    'MS': 'America/Chicago', 'MO': 'America/Chicago', 'ND': 'America/Chicago',
    'NE': 'America/Chicago', 'OK': 'America/Chicago', 'SD': 'America/Chicago',
    'TN': 'America/Chicago', 'TX': 'America/Chicago', 'WI': 'America/Chicago',

    // Mountain Time (7 states)
    'AZ': 'America/Phoenix',      // Arizona does not observe DST
    'CO': 'America/Denver', 'ID': 'America/Denver', 'MT': 'America/Denver',
    'NM': 'America/Denver', 'UT': 'America/Denver', 'WY': 'America/Denver',

    // Pacific Time (4 states)
    'CA': 'America/Los_Angeles', 'NV': 'America/Los_Angeles',
    'OR': 'America/Los_Angeles', 'WA': 'America/Los_Angeles',

    // Alaska Time (1 state)
    'AK': 'America/Anchorage',

    // Hawaii-Aleutian Time (1 state)
    'HI': 'Pacific/Honolulu',

    // US Territories
    'PR': 'America/Puerto_Rico',      // Puerto Rico (Atlantic Time, no DST)
    'VI': 'America/Virgin',            // US Virgin Islands (Atlantic Time, no DST)
    'GU': 'Pacific/Guam',              // Guam (Chamorro Time)
    'MP': 'Pacific/Saipan',            // Northern Mariana Islands (Chamorro Time)
    'AS': 'Pacific/Pago_Pago',         // American Samoa (Samoa Time)

    // === CANADA ===

    // Newfoundland Time (1 province)
    'NL': 'America/St_Johns',          // Newfoundland and Labrador (UTC-3:30)

    // Atlantic Time (3 provinces)
    'NB': 'America/Halifax',           // New Brunswick
    'NS': 'America/Halifax',           // Nova Scotia
    'PE': 'America/Halifax',           // Prince Edward Island

    // Eastern Time (2 provinces)
    'ON': 'America/Toronto',           // Ontario
    'QC': 'America/Toronto',           // Quebec

    // Central Time (2 provinces)
    'MB': 'America/Winnipeg',          // Manitoba
    'SK': 'America/Regina',            // Saskatchewan (no DST)

    // Mountain Time (1 province)
    'AB': 'America/Edmonton',          // Alberta

    // Pacific Time (1 province)
    'BC': 'America/Vancouver',         // British Columbia

    // Yukon Time (1 territory)
    'YT': 'America/Whitehorse',        // Yukon (was MST, now PST year-round since 2020)

    // Mountain Time (1 territory)
    'NT': 'America/Yellowknife',       // Northwest Territories

    // Eastern Time (1 territory)
    'NU': 'America/Iqaluit',           // Nunavut (uses multiple zones, Iqaluit is most populous)
  };

  /**
   * Determines the time of day (morning, afternoon, or evening) based on the local time in a given US state or Canadian province
   * @param state Two-letter US state or Canadian province abbreviation (e.g., 'CO', 'NY', 'ON', 'BC')
   * @returns 'GM ' (5am-12pm), 'GA ' (12pm-5pm), 'GE ' (5pm-5am), or 'FB ' (fallback if state/province not found)
   */
  public static getTimeOfDay(state: string): 'GM ' | 'GA ' | 'GE ' | 'FB ' {
    if (!state) return 'FB ';

    const stateUpper = state.toUpperCase();
    const timezone = this.STATE_TIMEZONES[stateUpper];

    if (!timezone) return 'FB ';

    try {
      // Get current time in the state's timezone
      const now = new Date();
      const formatter = new Intl.DateTimeFormat('en-US', {
        timeZone: timezone,
        hour: 'numeric',
        hour12: false
      });

      const hourStr = formatter.format(now);
      const hour = parseInt(hourStr, 10);

      // Determine time of day based on hour
      if (hour >= 5 && hour < 12) {
        return 'GM ';
      } else if (hour >= 12 && hour < 17) {
        return 'GA ';
      } else {
        return 'GE ';
      }
    } catch (e) {
      // Fallback if timezone API fails
      return 'FB ';
    }
  }

  /**
   * Extracts a nickname (in quotes) or first name from a name_fmt string
   * @param nameFmt Name format string (e.g., "John Doe", "John "JD" Doe", "Mary Smith")
   * @returns The nickname if present (without quotes), otherwise the first name, or empty string if invalid
   *
   * @example
   * Utils.extractNameOrNickname('John Doe') // Returns 'John'
   * Utils.extractNameOrNickname('John "JD" Doe') // Returns 'JD'
   * Utils.extractNameOrNickname('Mary "The Queen" Smith') // Returns 'The Queen'
   * Utils.extractNameOrNickname('Bob') // Returns 'Bob'
   */
  public static extractNameOrNickname(nameFmt: string | null | undefined): string {
    if (!nameFmt) {
      return '';
    }

    const trimmed = nameFmt.trim();
    if (!trimmed) {
      return '';
    }

    // Check for nickname in double quotes
    const nicknameMatch = trimmed.match(/"([^"]+)"/);
    if (nicknameMatch && nicknameMatch[1]) {
      const nickname = nicknameMatch[1].trim();
      // Only return the nickname if it's not empty after trimming
      if (nickname) {
        return nickname;
      }
    }

    // No valid nickname found, extract first name (first word)
    const words = trimmed.split(/\s+/);
    const firstWord = words[0] || '';

    // If the first word is only one letter (likely a middle initial), try the second word
    if (firstWord.length === 1 && words.length > 1) {
      return words[1];
    }

    return firstWord;
  }

  /**
   * Calculates the approximate time required to send a Morse code message
   * @param text The text message to send in Morse code
   * @param wpm Words per minute (standard is based on "PARIS" which is 50 units)
   * @param addedTime Additional time (in milliseconds) to add to the calculation (default 250ms)
   * @returns Time in milliseconds required to transmit the message
   *
   * @example
   * Utils.calculateMorseTime('CQ CQ CQ', 20) // Returns time in ms for sending at 20 WPM
   * Utils.calculateMorseTime('TU 599 CO', 25) // Returns time in ms for sending at 25 WPM
   *
   * @remarks
   * Morse code timing is based on the standard word "PARIS," which has 50 units:
   * - Dot: 1 unit
   * - Dash: 3 units
   * - Space between elements: 1 unit
   * - Space between letters: 3 units
   * - Space between words: 7 units
   * At 20 WPM, each unit is 60ms (1200ms / 20 words = 60ms per unit)
   */
  public static calculateMorseTime(text: string, wpm: number, addedTime: number = 250): number {
    if (!text.trim() || wpm <= 0) {
      return 0;
    }

    // Morse code lookup table (International Morse Code)
    const morseCode: { [key: string]: string } = {
      'A': '.-', 'B': '-...', 'C': '-.-.', 'D': '-..', 'E': '.', 'F': '..-.',
      'G': '--.', 'H': '....', 'I': '..', 'J': '.---', 'K': '-.-', 'L': '.-..',
      'M': '--', 'N': '-.', 'O': '---', 'P': '.--.', 'Q': '--.-', 'R': '.-.',
      'S': '...', 'T': '-', 'U': '..-', 'V': '...-', 'W': '.--', 'X': '-..-',
      'Y': '-.--', 'Z': '--..',
      '0': '-----', '1': '.----', '2': '..---', '3': '...--', '4': '....-',
      '5': '.....', '6': '-....', '7': '--...', '8': '---..', '9': '----.',
      '.': '.-.-.-', ',': '--..--', '?': '..--..', '/': '-..-.', '-': '-....-',
      '(': '-.--.', ')': '-.--.-', '=': '-...-', '+': '.-.-.', '@': '.--.-.',
      ' ': ' ' // Space between words
    };

    // Calculate time per unit (dot length) in milliseconds
    // Standard: "PARIS" = 50 units, so at X WPM: (60000ms / X) / 50 units
    const dotDuration = 1200 / wpm; // milliseconds per dot

    let totalUnits = 0;
    const upperText = text.toUpperCase();
    let previousWasLetter = false;

    for (let i = 0; i < upperText.length; i++) {
      const char = upperText[i];

      if (char === ' ') {
        // Space between words = 7 units (but we already counted 1 after a previous letter)
        totalUnits += 6; // Add 6 more to make 7 total
        previousWasLetter = false;
      } else if (morseCode[char]) {
        const morse = morseCode[char];

        // Add space between letters (3 units) if this isn't the first character
        if (previousWasLetter) {
          totalUnits += 3;
        }

        // Count dots and dashes
        for (const symbol of morse) {
          if (symbol === '.') {
            totalUnits += 1; // Dot = 1 unit
          } else if (symbol === '-') {
            totalUnits += 3; // Dash = 3 units
          }
          // Add 1 unit space between elements (dots/dashes) within a letter
          totalUnits += 1;
        }

        // Remove the last element space (we added one too many)
        totalUnits -= 1;

        previousWasLetter = true;
      }
      // Ignore characters not in the morse code table
    }
    let time = Math.round(totalUnits * dotDuration);
    if (time > 0)
      time += addedTime;

    return time;
  }
}
