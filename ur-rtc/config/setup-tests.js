// add here any code that you wanna execute before tests like
// - polyfills
// - some custom code
// for more docs check see https://jestjs.io/docs/en/configuration.html#setupfiles-array
require('dotenv').config();
process.env = {
  ...process.env,
  RELEASE_STAGE: 'local',
};
