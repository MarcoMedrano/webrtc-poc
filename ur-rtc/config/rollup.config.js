import commonjs from '@rollup/plugin-commonjs';
import json from '@rollup/plugin-json';
import nodeResolve from '@rollup/plugin-node-resolve';
import replace from '@rollup/plugin-replace';
import { resolve, dirname } from 'path';
import cleanup from 'rollup-plugin-cleanup';
import sourceMaps from 'rollup-plugin-sourcemaps';
import { terser } from 'rollup-plugin-terser';
import typescript from 'rollup-plugin-typescript2';
import pkg from '../package.json';
import { normalizePackageName, getBundleFileName, getDependencies } from './helpers';

const env = process.env.NODE_ENV || 'development';

const LIB_NAME = normalizePackageName(pkg.name);
const ROOT = resolve(__dirname, '..');

/**
 * Object literals are open-ended for js checking, so we need to be explicit
 * @type {{main: string, module: string, es5: string, es2015: string}}
 */
const ENTRIES = {
  main: resolve(ROOT, pkg.main),
  module: resolve(ROOT, pkg.module),
  es5: resolve(ROOT, pkg.es5),
  es2015: resolve(ROOT, pkg.es2015),
};

/**
 * @type {string[]}
 */
const external = getDependencies(pkg.dependencies, pkg.peerDependencies);

const plugins = (tsOptions = {}) => ({
  plugins: [
    json(),
    typescript(tsOptions),
    commonjs(),
    nodeResolve(),
    sourceMaps(),
    cleanup({ comments: 'none', extensions: ['ts', 'js'] }),
    replace({
      exclude: 'node_modules/**',
      'process.env.NODE_ENV': JSON.stringify(env),
    }),
  ],
});

const CommonConfig = {
  input: 'src/index.ts',
  output: {},
  inlineDynamicImports: true,
  external,
};

const UMDconfigs = {
  ...CommonConfig,
  output: [
    {
      file: ENTRIES.main,
      format: 'umd',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [],
    },
    {
      file: getBundleFileName(ENTRIES.main),
      format: 'umd',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [terser()],
    },
  ],
  ...plugins({
    tsconfigOverride: {
      compilerOptions: {
        declaration: false,
        declarationMap: false,
        declarationDir: '',
      },
    },
  }),
};

const ESM5configs = {
  ...CommonConfig,
  output: [
    {
      dir: dirname(ENTRIES.es5),
      format: 'es',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [],
    },
  ],
  inlineDynamicImports: false,
  preserveModules: true,
  ...plugins({
    tsconfigOverride: {
      compilerOptions: {
        declaration: false,
        declarationMap: false,
        declarationDir: '',
      },
    },
  }),
};

const ESconfigs = {
  ...CommonConfig,
  output: [
    {
      file: ENTRIES.module,
      format: 'es',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [],
    },
    {
      file: getBundleFileName(ENTRIES.module),
      format: 'es',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [terser()],
    },
  ],
  ...plugins({
    useTsconfigDeclarationDir: true,
    tsconfigOverride: {
      compilerOptions: {
        target: 'es2018',
      },
    },
  }),
};

const ESM2015configs = {
  ...CommonConfig,
  output: [
    {
      dir: dirname(ENTRIES.es2015),
      format: 'es',
      name: LIB_NAME,
      sourcemap: true,
      plugins: [],
    },
  ],
  inlineDynamicImports: false,
  preserveModules: true,
  ...plugins({
    useTsconfigDeclarationDir: true,
    tsconfigOverride: {
      compilerOptions: {
        target: 'es2018',
      },
    },
  }),
};

export default [UMDconfigs, ESM5configs, ESconfigs, ESM2015configs];
