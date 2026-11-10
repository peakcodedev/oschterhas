// Generates src/styles/tokens.css from the design system (design-system/tokens.json).
// Run after every change to the design system: npm run tokens
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const source = resolve(here, '../../../design-system/tokens.json');
const target = resolve(here, '../src/styles/tokens.css');
const tokens = JSON.parse(readFileSync(source, 'utf8'));

const firstTheme = tokens.color.themes[0].id;
const colorValue = (value) => (typeof value === 'string' ? value : value[firstTheme]);
const lines = [];
const add = (name, value, usage) => lines.push(`  --${name}: ${value};${usage ? ` /* ${usage.replace(/\*\//g, '')} */` : ''}`);

for (const t of tokens.color.tokens) add(t.name, colorValue(t.value), t.usage);
lines.push('');
for (const [family, stack] of Object.entries(tokens.type.families)) add(`font-${family}`, stack);
for (const group of tokens.type.groups) {
  for (const s of group.styles) {
    add(`${s.name}-font`, `var(--font-${group.family})`);
    add(`${s.name}-size`, s.fontSize);
    add(`${s.name}-line`, s.lineHeight);
    add(`${s.name}-weight`, String(s.fontWeight));
    if (s.letterSpacing) add(`${s.name}-tracking`, s.letterSpacing);
  }
}
lines.push('');
for (const family of ['spacing', 'radius', 'shadow']) {
  for (const t of tokens[family]?.tokens ?? []) add(t.name, t.value, t.usage);
}

const css = `/* ${tokens.name} design tokens — generated from design-system/tokens.json by scripts/generate-tokens.mjs. Do not edit. */
:root {
${lines.join('\n')}
}
`;
writeFileSync(target, css);
console.log(`tokens.css: ${lines.filter((l) => l.startsWith('  --')).length} custom properties`);
