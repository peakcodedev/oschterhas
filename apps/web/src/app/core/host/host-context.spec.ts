import { resolveHostContext } from './host-context';

describe('resolveHostContext', () => {
  const resolve = (host: string) => resolveHostContext(host, 'oschterhas.li', 'admin');

  it('detects the administration host', () => {
    expect(resolve('admin.oschterhas.li')).toEqual({ kind: 'admin' });
  });

  it('detects the base domain and www as landing page', () => {
    expect(resolve('oschterhas.li')).toEqual({ kind: 'landing' });
    expect(resolve('www.oschterhas.li')).toEqual({ kind: 'landing' });
  });

  it('extracts the slug of an instance subdomain', () => {
    expect(resolve('duernten.oschterhas.li')).toEqual({ kind: 'instance', slug: 'duernten' });
  });

  it('ignores case and a trailing dot', () => {
    expect(resolve('Duernten.Oschterhas.LI.')).toEqual({ kind: 'instance', slug: 'duernten' });
  });

  it('treats unknown hosts as custom domains of an instance', () => {
    expect(resolve('ostern.ceviduernten.ch')).toEqual({ kind: 'instance', slug: null });
  });

  it('does not take deeper subdomains as slug', () => {
    expect(resolve('a.b.oschterhas.li')).toEqual({ kind: 'instance', slug: null });
  });

  it('does not match look-alike domains', () => {
    expect(resolve('admin.oschterhas.li.evil.ch')).toEqual({ kind: 'instance', slug: null });
    expect(resolve('fakeoschterhas.li')).toEqual({ kind: 'instance', slug: null });
  });

  it('works with the local development domain', () => {
    expect(
      resolveHostContext('tann.oschterhas.localhost', 'oschterhas.localhost', 'admin'),
    ).toEqual({
      kind: 'instance',
      slug: 'tann',
    });
  });
});
