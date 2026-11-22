import { describe, expect, it } from '@jest/globals';

import { busNumber, busTitle, formatAge, headingLabel, stopShortName } from '../format';

describe('headingLabel', () => {
  it.each([
    [0, 'Norte'],
    [45, 'Noreste'],
    [90, 'Este'],
    [180, 'Sur'],
    [270, 'Oeste'],
    [315, 'Noroeste'],
  ])('%p° is %s', (degrees, label) => {
    expect(headingLabel(degrees)).toBe(label);
  });

  it('rounds to the nearest compass point', () => {
    expect(headingLabel(100)).toBe('Este');
    expect(headingLabel(350)).toBe('Norte');
  });

  it('accepts 360 and negative angles', () => {
    expect(headingLabel(360)).toBe('Norte');
    expect(headingLabel(-90)).toBe('Oeste');
  });
});

describe('busNumber and busTitle', () => {
  it('take the trailing number of the id', () => {
    expect(busNumber('bus-01')).toBe('01');
    expect(busTitle('bus-12')).toBe('Bus 12');
  });

  it('fall back to the whole id when it has no number', () => {
    expect(busNumber('azul')).toBe('azul');
  });
});

describe('formatAge', () => {
  it('uses seconds, minutes or hours', () => {
    expect(formatAge(3)).toBe('hace 3 s');
    expect(formatAge(59)).toBe('hace 59 s');
    expect(formatAge(60)).toBe('hace 1 min');
    expect(formatAge(3599)).toBe('hace 59 min');
    expect(formatAge(7200)).toBe('hace 2 h');
  });
});

describe('stopShortName', () => {
  it('drops the "Parada" prefix', () => {
    expect(stopShortName('Parada El Roble')).toBe('El Roble');
    expect(stopShortName('Terminal Norte')).toBe('Terminal Norte');
  });
});
