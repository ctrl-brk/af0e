const fs = require('fs');
const path = require('path');

const testCallsign = process.argv[2];
const callsListPath = process.argv[3] || './dx-list.txt';

if (!testCallsign) {
  console.log('Usage: node test-calls.js <callsign-to-test> [path-to-calls.txt]');
  console.log('Example: node test-calls.js AF0E dx-list.txt');
  process.exit(1);
}

// Load and parse the calls list (same logic as your app)
let callsRegexList = [];

if (fs.existsSync(callsListPath)) {
  try {
    const fileContent = fs.readFileSync(callsListPath).toString();
    const patterns = fileContent
      .split("|")
      .map(pattern => pattern.trim())
      .filter(pattern => pattern.length > 0);
    
    // Convert each pattern to a RegExp object
    callsRegexList = patterns.map(pattern => {
      try {
        const escapedPattern = pattern.replace(/\//g, '\\/');
        return { pattern: pattern, regex: new RegExp('^' + escapedPattern + '$') };
      } catch (e) {
        console.log(`Invalid regex pattern: ${pattern}`);
        return null;
      }
    }).filter(item => item !== null);
    
    console.log(`Loaded ${callsRegexList.length} patterns from ${callsListPath}\n`);
  } catch (error) {
    console.log(`Error loading calls.txt: ${error}`);
    process.exit(1);
  }
} else {
  console.log(`File not found: ${callsListPath}`);
  process.exit(1);
}

// Test the callsign
console.log(`Testing callsign: ${testCallsign}\n`);

const matches = callsRegexList.filter(item => item.regex.test(testCallsign));

if (matches.length > 0) {
  console.log(`✓ MATCH FOUND! (${matches.length} pattern(s) matched)`);
  console.log('\nMatching patterns:');
  matches.forEach(match => {
    console.log(`  - ${match.pattern}`);
  });
} else {
  console.log('✗ NO MATCH');
}

/*
console.log('\n--- All patterns in file ---');
callsRegexList.forEach(item => {
  const matched = item.regex.test(testCallsign) ? '✓' : ' ';
  console.log(`${matched} ${item.pattern}`);
});
*/
