/**
 *
 * @param {string} myStr
 */
export function camelCaseToDash(myStr) {
  return myStr.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
}

/**
 *
 * @param {string} myStr
 */
export function dashToCamelCase(myStr) {
  return myStr.replace(/-([a-z])/g, (g) => g[1].toUpperCase());
}

/**
 *
 * @param {string} myStr
 */
export function toUpperCase(myStr) {
  return `${myStr.charAt(0).toUpperCase()}${myStr.substr(1)}`;
}

/**
 *
 * @param {string} myStr
 */
export function pascalCase(myStr) {
  return toUpperCase(dashToCamelCase(myStr));
}

/**
 *
 * @param {string} rawPackageName
 */
export function normalizePackageName(rawPackageName) {
  const scopeEnd = rawPackageName.indexOf('/') + 1;

  return pascalCase(rawPackageName.substring(scopeEnd));
}

/**
 *
 * @param {string} fileName
 */
export function getBundleFileName(fileName) {
  return fileName.replace(/\.js$/, '.min.js');
}

export function getDependencies(dependencies = {}, peerDependencies = {}) {
  return [...Object.keys(dependencies), ...Object.keys(peerDependencies)];
}
